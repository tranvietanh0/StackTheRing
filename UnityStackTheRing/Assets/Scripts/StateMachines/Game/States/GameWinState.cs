namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UniT.Logging;
    using ILogger = UniT.Logging.ILogger;

    public class GameWinState : IGameState, IHaveStateMachine
    {
        #region Inject

        private readonly IScreenManager screenManager;
        private readonly SignalBus signalBus;
        private readonly ILogger logger;

        public GameWinState(
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
            this.logger.Info("Entering GameWinState");

            this.ShowWinScreen();
        }

        public void Exit()
        {
            this.logger.Info("Exiting GameWinState");
        }

        #endregion

        #region Private Methods

        private async void ShowWinScreen()
        {
            // TODO: Open win popup
            // await this.screenManager.OpenScreen<WinPopupPresenter>();
        }

        #endregion
    }
}
