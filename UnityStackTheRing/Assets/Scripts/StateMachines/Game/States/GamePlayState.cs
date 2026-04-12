namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using System.Linq;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.CollectArea;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
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
        private BucketColumnManager bucketColumnManager;
        private CollectAreaManager collectAreaManager;
        private CollectAreaBucketService collectAreaBucketService;

        #endregion

        #region State

        private bool isPlaying;
        private bool hasCollectedThisLoop;
        private int lastCheckedLoopCount;
        private int totalBuckets;
        private int completedBuckets;

        #endregion

        #region IGameState

        public void Enter()
        {
            this.logger.Info("Entering GamePlayState");

            this.isPlaying = true;
            this.hasCollectedThisLoop = false;
            this.lastCheckedLoopCount = 0;

            // Count total buckets at start
            this.totalBuckets = this.bucketColumnManager?.SpawnedBuckets.Count ?? 0;
            this.completedBuckets = 0;

            this.signalBus.Subscribe<AllRingsClearedSignal>(this.OnAllBallsCleared);
            this.signalBus.Subscribe<RowBallCompletedLoopSignal>(this.OnRowBallCompletedLoop);
            this.signalBus.Subscribe<BallCollectedSignal>(this.OnBallCollected);
            this.signalBus.Subscribe<BucketCompletedSignal>(this.OnBucketCompleted);

            this.StartGameplay();
        }

        public void Exit()
        {
            this.logger.Info("Exiting GamePlayState");

            this.isPlaying = false;

            this.signalBus.Unsubscribe<AllRingsClearedSignal>(this.OnAllBallsCleared);
            this.signalBus.Unsubscribe<RowBallCompletedLoopSignal>(this.OnRowBallCompletedLoop);
            this.signalBus.Unsubscribe<BallCollectedSignal>(this.OnBallCollected);
            this.signalBus.Unsubscribe<BucketCompletedSignal>(this.OnBucketCompleted);

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
            BucketColumnManager bucketColumnManager,
            CollectAreaManager collectAreaManager,
            CollectAreaBucketService collectAreaBucketService)
        {
            this.conveyor = conveyor;
            this.bucketColumnManager = bucketColumnManager;
            this.collectAreaManager = collectAreaManager;
            this.collectAreaBucketService = collectAreaBucketService;
        }

        #endregion

        #region Private Methods

        private void StartGameplay()
        {
            this.conveyor?.StartConveyor();
        }

        private void StopGameplay()
        {
            this.conveyor?.StopConveyor();
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
                if (!this.hasCollectedThisLoop && this.collectAreaManager.AreAllCollectAreasOccupied())
                {
                    this.CheckLoseCondition();
                }

                this.hasCollectedThisLoop = false;
                this.lastCheckedLoopCount = signal.LoopCount;
            }
        }

        private void OnBallCollected(BallCollectedSignal signal)
        {
            this.hasCollectedThisLoop = true;
        }

        private void OnBucketCompleted(BucketCompletedSignal signal)
        {
            this.completedBuckets++;

            this.logger.Info($"Bucket completed: {signal.Color}. Progress: {this.completedBuckets}/{this.totalBuckets}");

            // Check win condition - all buckets completed
            if (this.completedBuckets >= this.totalBuckets)
            {
                this.logger.Info("All buckets completed - WIN!");
                this.isPlaying = false;

                this.levelManager.CompleteLevel();
                this.StateMachine.TransitionTo<GameWinState>();
            }
        }

        private void CheckLoseCondition()
        {
            if (!this.isPlaying) return;
            if (this.conveyor == null || this.collectAreaManager == null) return;

            // Only check lose when all collect areas are occupied
            if (!this.collectAreaManager.AreAllCollectAreasOccupied()) return;

            // Get target colors from buckets in CollectAreas
            var targetColors = this.collectAreaBucketService.GetTargetColorsFromBuckets();

            // Check if any ball on conveyor can be collected
            var canCollectAny = false;
            foreach (var rowBall in this.conveyor.ActiveRowBalls)
            {
                foreach (var ball in rowBall.GetActiveBalls())
                {
                    // Check if ball color matches any target AND has available slots
                    if (targetColors.Contains(ball.BallColor) &&
                        this.collectAreaBucketService.GetAvailableSlotCountByColor(ball.BallColor) > 0)
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
