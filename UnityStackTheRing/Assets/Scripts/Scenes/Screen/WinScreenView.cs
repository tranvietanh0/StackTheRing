namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.Level;
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

        public WinScreenPresenter(
            SignalBus      signalBus,
            ILoggerManager loggerManager,
            ILevelManager  levelManager
        ) : base(signalBus, loggerManager)
        {
            this.levelManager = levelManager;
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
            this.levelManager.LoadNextLevel().Forget();
        }
    }
}