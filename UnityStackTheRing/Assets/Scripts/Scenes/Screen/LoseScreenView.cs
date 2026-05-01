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

    public class LoseScreenView : BaseView
    {
        [field: SerializeField] public Button BtnReplay { get; private set; }
    }

    [ScreenInfo(nameof(LoseScreenView))]
    public class LoseScreenPresenter : BaseScreenPresenter<LoseScreenView>
    {
        private readonly ILevelManager levelManager;
        private bool isReplaying;

        public LoseScreenPresenter(
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
            this.View.BtnReplay.onClick.AddListener(this.OnClickReplay);
        }

        public override UniTask BindData()
        {
            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            this.View.BtnReplay.onClick.RemoveListener(this.OnClickReplay);
            base.Dispose();
        }

        private void OnClickReplay()
        {
            if (this.isReplaying)
            {
                return;
            }

            this.isReplaying = true;
            this.View.BtnReplay.interactable = false;
            this.levelManager.LoadCurrentLevel().Forget();
        }
    }
}
