namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.UI;

    public class GameplayScreenView : BaseView
    {
        [field: SerializeField] public Button BtnHome { get; private set; }
    }
    [ScreenInfo(nameof(GameplayScreenView))]
    public class GameplayScreenPresenter : BaseScreenPresenter<GameplayScreenView>
    {
        #region Inject
        private readonly GameStateMachine gameStateMachine;

        public GameplayScreenPresenter(
            SignalBus      signalBus,
            ILoggerManager loggerManager,
            GameStateMachine gameStateMachine
        ) : base(signalBus, loggerManager)
        {
            this.gameStateMachine = gameStateMachine;
        }
        #endregion

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.BtnHome.onClick.AddListener(this.OnClickHome);
        }

        public override UniTask BindData()
        {
            return UniTask.CompletedTask;
        }


        private void OnClickHome()
        {
            this.gameStateMachine.TransitionTo<GameHomeState>();
        }
    }
}