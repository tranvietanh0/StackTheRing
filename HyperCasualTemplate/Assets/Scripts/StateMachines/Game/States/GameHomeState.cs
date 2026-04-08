namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;

    public class GameHomeState : IGameState
    {
        #region Inject

        private readonly IScreenManager screenManager;

        public GameHomeState(IScreenManager screenManager)
        {
            this.screenManager = screenManager;
        }

        #endregion

        public void Enter()
        {
        }

        public void Exit()
        {
        }
    }
}