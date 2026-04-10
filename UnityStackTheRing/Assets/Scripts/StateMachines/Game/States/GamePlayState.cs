namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Attraction;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.Slot;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UniT.Logging;

    public class GamePlayState : IGameState, IHaveStateMachine, ITickable
    {
        #region Inject

        private readonly SignalBus signalBus;
        private readonly ILevelManager levelManager;
        private readonly ILogger logger;

        public GamePlayState(
            SignalBus signalBus,
            ILevelManager levelManager,
            ILoggerManager loggerManager)
        {
            this.signalBus = signalBus;
            this.levelManager = levelManager;
            this.logger = loggerManager.GetLogger(this);
        }

        #endregion

        #region Properties

        public GameStateMachine StateMachine { get; set; }

        #endregion

        #region References (set externally)

        private ConveyorController conveyor;
        private SlotManager slotManager;
        private AttractionController attractionController;

        #endregion

        #region State

        private bool isPlaying;
        private bool hasCollectedThisLoop;
        private int lastCheckedLoopCount;

        #endregion

        #region IGameState

        public void Enter()
        {
            this.logger.Info("Entering GamePlayState");

            this.isPlaying = true;
            this.hasCollectedThisLoop = false;
            this.lastCheckedLoopCount = 0;

            this.signalBus.Subscribe<AllRingsClearedSignal>(this.OnAllBallsCleared);
            this.signalBus.Subscribe<RowBallCompletedLoopSignal>(this.OnRowBallCompletedLoop);
            this.signalBus.Subscribe<BallAttractedSignal>(this.OnBallAttracted);

            this.StartGameplay();
        }

        public void Exit()
        {
            this.logger.Info("Exiting GamePlayState");

            this.isPlaying = false;

            this.signalBus.Unsubscribe<AllRingsClearedSignal>(this.OnAllBallsCleared);
            this.signalBus.Unsubscribe<RowBallCompletedLoopSignal>(this.OnRowBallCompletedLoop);
            this.signalBus.Unsubscribe<BallAttractedSignal>(this.OnBallAttracted);

            this.StopGameplay();
        }

        #endregion

        #region ITickable

        public void Tick()
        {
            if (!this.isPlaying) return;

            this.CheckLoseCondition();
        }

        #endregion

        #region Public Methods

        public void SetReferences(
            ConveyorController conveyor,
            SlotManager slotManager,
            AttractionController attractionController)
        {
            this.conveyor = conveyor;
            this.slotManager = slotManager;
            this.attractionController = attractionController;
        }

        #endregion

        #region Private Methods

        private void StartGameplay()
        {
            this.conveyor?.StartConveyor();
            this.attractionController?.SetEnabled(true);
        }

        private void StopGameplay()
        {
            this.conveyor?.StopConveyor();
            this.attractionController?.SetEnabled(false);
        }

        private void OnAllBallsCleared()
        {
            if (!this.isPlaying) return;

            this.logger.Info("All balls cleared - WIN!");
            this.isPlaying = false;

            this.levelManager.CompleteLevel();
            this.StateMachine.TransitionTo<GameWinState>();
        }

        private void OnRowBallCompletedLoop(RowBallCompletedLoopSignal signal)
        {
            if (!this.isPlaying) return;

            if (signal.LoopCount > this.lastCheckedLoopCount)
            {
                if (!this.hasCollectedThisLoop && this.slotManager.AllSlotsOccupied())
                {
                    this.CheckLoseCondition();
                }

                this.hasCollectedThisLoop = false;
                this.lastCheckedLoopCount = signal.LoopCount;
            }
        }

        private void OnBallAttracted(BallAttractedSignal signal)
        {
            this.hasCollectedThisLoop = true;
        }

        private void CheckLoseCondition()
        {
            if (!this.isPlaying) return;
            if (this.conveyor == null || this.slotManager == null) return;

            if (!this.slotManager.AllSlotsOccupied()) return;

            var canCollectAny = false;
            foreach (var rowBall in this.conveyor.ActiveRowBalls)
            {
                foreach (var ball in rowBall.GetActiveBalls())
                {
                    if (this.slotManager.CanCollectColor(ball.BallColor))
                    {
                        canCollectAny = true;
                        break;
                    }
                }
                if (canCollectAny) break;
            }

            if (!canCollectAny)
            {
                this.logger.Info("No possible moves - LOSE!");
                this.isPlaying = false;

                this.levelManager.FailLevel();
                this.StateMachine.TransitionTo<GameLoseState>();
            }
        }

        #endregion
    }
}
