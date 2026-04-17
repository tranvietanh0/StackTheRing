namespace HyperCasualGame.Scripts.Level
{
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
            if (this.levelData != null && this.levelData.HasQueue && (this.queueConveyor == null || this.conveyorFeeder == null))
            {
                throw new MissingReferenceException("Queue level requires QueueConveyor and ConveyorFeeder references.");
            }

            var hasQueue = this.levelData != null && this.levelData.HasQueue && this.queueConveyor != null;
            Debug.Log($"[LevelController] hasQueue={hasQueue}, levelData.HasQueue={this.levelData?.HasQueue}, queueConveyor null={this.queueConveyor == null}, conveyorFeeder null={this.conveyorFeeder == null}");

            if (hasQueue)
            {
                this.queueConveyor.Initialize(this.signalBus, this.loggerManager);
            }

            // Initialize collect areas
            this.collectAreaManager.SpawnAreas(this.collectAreaCount);

            // Wire up services
            Debug.Log($"[LevelController] collectAreaBucketService is {(this.collectAreaBucketService != null ? "valid" : "NULL")}");
            this.collectAreaBucketService.SetCollectAreaManager(this.collectAreaManager);
            this.conveyorController.SetCollectAreaBucketService(this.collectAreaBucketService);

            // Initialize bucket manager
            this.bucketColumnManager.Initialize(this.signalBus, this.collectAreaManager);

            // Initialize feeder if queue is present
            if (hasQueue && this.conveyorFeeder != null)
            {
                this.conveyorFeeder.Initialize(
                    this.conveyorController,
                    this.queueConveyor,
                    this.loggerManager);
            }

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
                    this.queueConveyor,
                    this.conveyorFeeder);
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

            // Setup queue conveyor if present
            if (levelData.HasQueue && this.queueConveyor != null)
            {
                this.queueConveyor.SetupLevel(levelData, this.ballPrefab, this.rowBallPrefab);
            }

            this.conveyorController.SetHasQueueRows(levelData.HasQueue && this.queueConveyor != null && !this.queueConveyor.IsEmpty);

            // Setup bucket grid — include queue balls in target count
            this.bucketColumnManager.SpawnBuckets(levelData, this.conveyorController.BallsPerRow);

            this.logger.Info($"Level {levelData.LevelNumber} setup complete (queue={levelData.HasQueue})");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reload current level (called externally, will destroy this instance).
        /// </summary>
        public void RetryLevel()
        {
            // LevelManager will unload this level and load a new instance
            this.levelManager.LoadLevel(this.levelManager.CurrentLevel).Forget();
        }

        /// <summary>
        /// Load next level (called externally, will destroy this instance).
        /// </summary>
        public void NextLevel()
        {
            // LevelManager will unload this level and load a new instance
            this.levelManager.LoadLevel(this.levelManager.CurrentLevel + 1).Forget();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            this.signalBus?.Unsubscribe<BucketTappedSignal>(this.OnBucketTapped);
            this.bucketColumnManager?.Cleanup();
            this.collectAreaManager?.Cleanup();
            this.conveyorFeeder?.StopFeeding();
        }

        #endregion
    }
}
