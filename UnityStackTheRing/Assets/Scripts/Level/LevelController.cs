namespace HyperCasualGame.Scripts.Level
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.CollectArea;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class LevelController : MonoBehaviour, IInitializable
    {
        #region Serialized Fields

        [Header("Level Config")]
        [SerializeField] private LevelData levelData;

        [Header("References")]
        [SerializeField] private ConveyorController conveyorController;
        [SerializeField] private BucketColumnManager bucketColumnManager;
        [SerializeField] private CollectAreaManager collectAreaManager;

        [Header("Queue Conveyor (Optional)")]
        [SerializeField] private QueueConveyor queueConveyor;
        [SerializeField] private ConveyorFeeder conveyorFeeder;
        [SerializeField] private QueueLaneBinding[] queueLaneBindings;

        [Header("Prefabs")]
        [SerializeField] private Ball ballPrefab;
        [SerializeField] private RowBall rowBallPrefab;
        [SerializeField] private Bucket bucketPrefab;

        [Header("Configs")]
        [SerializeField] private ConveyorConfig conveyorConfig;
        [SerializeField] private int collectAreaCount = GameConstants.CollectAreaConfig.DefaultAreaCount;

        #endregion

        #region Inject

        private SignalBus signalBus;
        private ILevelManager levelManager;
        private GameStateMachine gameStateMachine;
        private ILoggerManager loggerManager;
        private ILogger logger;
        private CollectAreaBucketService collectAreaBucketService;
        private MultiQueueCoordinator multiQueueCoordinator;

        public void Inject(
            SignalBus signalBus,
            ILevelManager levelManager,
            GameStateMachine gameStateMachine,
            ILoggerManager loggerManager,
            CollectAreaBucketService collectAreaBucketService)
        {
            Debug.Log($"[LevelController] Inject() called. collectAreaBucketService={collectAreaBucketService != null}");
            this.signalBus = signalBus;
            this.levelManager = levelManager;
            this.gameStateMachine = gameStateMachine;
            this.loggerManager = loggerManager;
            this.logger = loggerManager.GetLogger(this);
            this.collectAreaBucketService = collectAreaBucketService;
        }

        #endregion

        #region IInitializable

        public void Initialize()
        {
            Debug.Log("[LevelController] Initialize() called");
            this.InitializeSystems();
            this.StartGame().Forget();
        }

        #endregion

        #region Properties

        public LevelData LevelData => this.levelData;
        public ConveyorController ConveyorController => this.conveyorController;
        public BucketColumnManager BucketColumnManager => this.bucketColumnManager;
        public CollectAreaManager CollectAreaManager => this.collectAreaManager;

        #endregion

        #region Private Fields

        private bool isInitialized;

        #endregion

        #region Private Methods

        private void InitializeSystems()
        {
            if (this.isInitialized) return;
            this.isInitialized = true;

            // Initialize conveyor
            this.conveyorController.Initialize(this.signalBus, this.loggerManager);

            // Initialize queue conveyor if present
            var activeQueueLanes = this.levelData != null ? this.levelData.GetActiveQueueLanes() : Array.Empty<QueueLaneData>();
            var useExplicitQueueLanes = this.levelData != null && this.levelData.QueueLanes != null && this.levelData.QueueLanes.Length > 0;
            var effectiveQueueBindings = this.GetEffectiveQueueBindings(!useExplicitQueueLanes);
            if (activeQueueLanes.Length > 0 && effectiveQueueBindings.Length == 0)
            {
                throw new MissingReferenceException("Queue level requires queue lane bindings.");
            }

            if (effectiveQueueBindings.Length > 0 && activeQueueLanes.Length > effectiveQueueBindings.Length)
            {
                throw new MissingReferenceException("Queue lane bindings count is smaller than active queue lanes count.");
            }

            this.multiQueueCoordinator = new MultiQueueCoordinator(this.loggerManager);
            if (activeQueueLanes.Length > 0)
            {
                this.multiQueueCoordinator.Initialize(
                    this.conveyorController,
                    this.signalBus,
                    effectiveQueueBindings);
            }

            // Initialize collect areas
            this.collectAreaManager.SpawnAreas(this.collectAreaCount);

            // Wire up services
            Debug.Log($"[LevelController] collectAreaBucketService is {(this.collectAreaBucketService != null ? "valid" : "NULL")}");
            this.collectAreaBucketService.SetCollectAreaManager(this.collectAreaManager);
            this.conveyorController.SetCollectAreaBucketService(this.collectAreaBucketService);

            // Initialize bucket manager
            this.bucketColumnManager.Initialize(this.signalBus, this.collectAreaManager);

            // Subscribe to bucket tap signal
            this.signalBus.Subscribe<BucketTappedSignal>(this.OnBucketTapped);

            var playState = this.gameStateMachine.GetState<GamePlayState>();
            if (playState != null)
            {
                playState.SetReferences(
                    this.conveyorController,
                    this.bucketColumnManager,
                    this.collectAreaManager,
                    this.collectAreaBucketService,
                    this.multiQueueCoordinator);
            }

            this.logger.Info("LevelController initialized");
        }

        private void OnBucketTapped(BucketTappedSignal signal)
        {
            // Find the bucket by index and trigger jump
            foreach (var bucket in this.bucketColumnManager.SpawnedBuckets)
            {
                if (bucket.Data.IndexBucket == signal.BucketIndex)
                {
                    this.bucketColumnManager.OnBucketTapped(bucket).Forget();
                    break;
                }
            }
        }

        private async UniTask StartGame()
        {
            // Use levelData from prefab (assigned in inspector)
            if (this.levelData != null)
            {
                this.SetupLevel(this.levelData);
                this.gameStateMachine.TransitionTo<GamePlayState>();
            }
            else
            {
                this.logger.Error("LevelData not assigned in prefab");
            }

            await UniTask.CompletedTask;
        }

        private void SetupLevel(LevelData levelData)
        {
            // Setup conveyor with balls
            this.conveyorController.SetupLevel(levelData, this.ballPrefab, this.rowBallPrefab);

            if (this.multiQueueCoordinator != null && levelData.HasAnyQueue)
            {
                this.multiQueueCoordinator.SetupLevel(levelData, this.ballPrefab, this.rowBallPrefab);
            }

            this.multiQueueCoordinator?.SyncMainQueueState();

            // Setup bucket grid — include queue balls in target count
            this.bucketColumnManager.SpawnBuckets(levelData, this.conveyorController.BallsPerRow, this.multiQueueCoordinator);

            this.logger.Info($"Level {levelData.LevelNumber} setup complete (queue={levelData.HasQueue})");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reload current level (called externally, will destroy this instance).
        /// </summary>
        public void RetryLevel()
        {
            this.levelManager.LoadCurrentLevel().Forget();
        }

        /// <summary>
        /// Load next level (called externally, will destroy this instance).
        /// </summary>
        public void NextLevel()
        {
            this.levelManager.LoadNextLevel().Forget();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            this.signalBus?.Unsubscribe<BucketTappedSignal>(this.OnBucketTapped);
            this.bucketColumnManager?.Cleanup();
            this.collectAreaManager?.Cleanup();
            this.multiQueueCoordinator?.Stop();
        }

        private QueueLaneBinding[] GetEffectiveQueueBindings(bool allowLegacyFallback)
        {
            if (this.queueLaneBindings != null && this.queueLaneBindings.Length > 0)
            {
                return this.queueLaneBindings;
            }

            if (allowLegacyFallback && this.queueConveyor != null && this.conveyorFeeder != null)
            {
                return new[]
                {
                    new QueueLaneBinding
                    {
                        LaneId = "queue-0",
                        QueueConveyor = this.queueConveyor,
                        ConveyorFeeder = this.conveyorFeeder,
                        InsertAnchor = null
                    }
                };
            }

            return Array.Empty<QueueLaneBinding>();
        }

        #endregion
    }
}
