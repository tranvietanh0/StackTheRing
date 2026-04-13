namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using UniT.Logging;

    public class HomeScreenView : BaseView
    {

    }

    [ScreenInfo(nameof(HomeScreenView))]
    public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView>
    {
        public HomeScreenPresenter(
            SignalBus      signalBus,
            ILoggerManager loggerManager
        ) : base(signalBus, loggerManager) { }

        public override UniTask BindData()
        {
            return UniTask.CompletedTask;
        }
    }
}