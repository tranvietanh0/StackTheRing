namespace HyperCasualGame.Scripts.Core
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.CollectArea;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class GameManager : MonoBehaviour, IInitializable
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private ConveyorController conveyorController;
        [SerializeField] private BucketColumnManager bucketColumnManager;
        [SerializeField] private CollectAreaManager collectAreaManager;

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
            this.InitializeSystems();
            this.StartGame().Forget();
        }

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

            // Initialize collect areas
            this.collectAreaManager.SpawnAreas(this.collectAreaCount);

            // Wire up services
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
                    this.collectAreaBucketService);
            }

            this.logger.Info("GameManager initialized");
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
            var levelData = await this.levelManager.LoadLevel(1);

            if (levelData != null)
            {
                this.SetupLevel(levelData);
                this.gameStateMachine.TransitionTo<GamePlayState>();
            }
            else
            {
                this.logger.Error("Failed to load level data");
            }
        }

        private void SetupLevel(LevelData levelData)
        {
            // Setup conveyor with balls
            this.conveyorController.SetupLevel(levelData, this.ballPrefab, this.rowBallPrefab);

            // Setup bucket grid
            this.bucketColumnManager.SpawnBuckets(levelData);

            this.logger.Info($"Level {levelData.LevelNumber} setup complete");
        }

        #endregion

        #region Public Methods

        public async UniTask LoadLevel(int levelNumber)
        {
            var levelData = await this.levelManager.LoadLevel(levelNumber);

            if (levelData != null)
            {
                this.SetupLevel(levelData);
                this.gameStateMachine.TransitionTo<GamePlayState>();
            }
        }

        public void RetryLevel()
        {
            this.LoadLevel(this.levelManager.CurrentLevel).Forget();
        }

        public void NextLevel()
        {
            this.LoadLevel(this.levelManager.CurrentLevel + 1).Forget();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            this.signalBus?.Unsubscribe<BucketTappedSignal>(this.OnBucketTapped);
            this.bucketColumnManager?.Cleanup();
            this.collectAreaManager?.Cleanup();
        }

        #endregion
    }
}
