namespace HyperCasualGame.Scripts.Core
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Attraction;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Slot;
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
        [SerializeField] private SlotManager slotManager;
        [SerializeField] private AttractionController attractionController;
        [SerializeField] private CollectorPanel collectorPanel;

        [Header("Prefabs")]
        [SerializeField] private Ball ballPrefab;
        [SerializeField] private RowBall rowBallPrefab;

        [Header("Configs")]
        [SerializeField] private ConveyorConfig conveyorConfig;
        [SerializeField] private AttractionConfig attractionConfig;

        #endregion

        #region Inject

        private SignalBus signalBus;
        private ILevelManager levelManager;
        private GameStateMachine gameStateMachine;
        private ILoggerManager loggerManager;
        private ILogger logger;

        public void Inject(
            SignalBus signalBus,
            ILevelManager levelManager,
            GameStateMachine gameStateMachine,
            ILoggerManager loggerManager)
        {
            this.signalBus = signalBus;
            this.levelManager = levelManager;
            this.gameStateMachine = gameStateMachine;
            this.loggerManager = loggerManager;
            this.logger = loggerManager.GetLogger(this);
        }

        #endregion

        #region IInitializable

        public void Initialize()
        {
            this.InitializeSystems();
            this.StartGame().Forget();
        }

        #endregion

        #region Private Methods

        private void InitializeSystems()
        {
            this.conveyorController.Initialize(this.signalBus, this.loggerManager);
            this.slotManager.Initialize(this.signalBus, this.loggerManager);
            this.collectorPanel.Initialize(this.signalBus, this.loggerManager);
            this.attractionController.Initialize(
                this.conveyorController,
                this.slotManager,
                this.signalBus,
                this.loggerManager);

            var playState = this.gameStateMachine.GetState<GamePlayState>();
            if (playState != null)
            {
                playState.SetReferences(
                    this.conveyorController,
                    this.slotManager,
                    this.attractionController);
            }

            this.logger.Info("GameManager initialized");
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
            this.conveyorController.SetupLevel(levelData, this.ballPrefab, this.rowBallPrefab);
            this.slotManager.SetupLevel(levelData);
            this.collectorPanel.SetupLevel(levelData);

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
            this.slotManager?.Cleanup();
            this.collectorPanel?.Cleanup();
        }

        #endregion
    }
}
