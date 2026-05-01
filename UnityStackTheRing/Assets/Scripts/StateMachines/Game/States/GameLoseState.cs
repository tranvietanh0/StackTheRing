namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.Scenes.Screen;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UniT.Logging;
    using ILogger = UniT.Logging.ILogger;

    public class GameLoseState : IGameState, IHaveStateMachine
    {
        #region Inject

        private readonly IScreenManager screenManager;
        private readonly SignalBus signalBus;
        private readonly ILogger logger;

        public GameLoseState(
            IScreenManager screenManager,
            SignalBus signalBus,
            ILoggerManager loggerManager)
        {
            this.screenManager = screenManager;
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);
        }

        #endregion

        #region Properties

        public GameStateMachine StateMachine { get; set; }

        #endregion

        #region IGameState

        public void Enter()
        {
            this.logger.Info("Entering GameLoseState");

            this.ShowLoseScreen().Forget();
        }

        public void Exit()
        {
            this.logger.Info("Exiting GameLoseState");
        }

        #endregion

        #region Private Methods

        private async UniTask ShowLoseScreen()
        {
            await this.screenManager.OpenScreen<LoseScreenPresenter>();
        }

        #endregion
    }
}
