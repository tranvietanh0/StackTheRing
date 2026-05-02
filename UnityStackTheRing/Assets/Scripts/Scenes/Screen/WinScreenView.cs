namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.UI;

    public class WinScreenView : BaseView
    {
        [field: SerializeField] public Button BtnNext { get; private set; }
    }

    [ScreenInfo(nameof(WinScreenView))]
    public class WinScreenPresenter : BaseScreenPresenter<WinScreenView>
    {
        private readonly ILevelManager levelManager;
        private readonly IScreenManager screenManager;
        private readonly GameStateMachine gameStateMachine;

        public WinScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            ILevelManager levelManager,
            IScreenManager screenManager,
            GameStateMachine gameStateMachine
        ) : base(signalBus, loggerManager)
        {
            this.levelManager = levelManager;
            this.screenManager = screenManager;
            this.gameStateMachine = gameStateMachine;
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            this.View.BtnNext.onClick.AddListener(this.OnClickNext);
        }

        public override UniTask BindData()
        {
            return UniTask.CompletedTask;
        }

        private void OnClickNext()
        {
            this.LoadNextLevel().Forget();
        }

        private async UniTask LoadNextLevel()
        {
            this.View.BtnNext.interactable = false;

            try
            {
                var gameplayScreen = await this.screenManager.GetScreen<GameplayScreenPresenter>();
                if (gameplayScreen == null)
                {
                    return;
                }

                await gameplayScreen.OpenViewAsync();

                var nextLevelController = await this.levelManager.LoadNextLevel();
                if (nextLevelController == null)
                {
                    await gameplayScreen.CloseViewAsync();
                    return;
                }

                this.gameStateMachine.TransitionTo<GamePlayState>();
                await this.CloseViewAsync();
            }
            finally
            {
                if (this.View != null && this.View.BtnNext != null)
                {
                    this.View.BtnNext.interactable = true;
                }
            }
        }
    }
}