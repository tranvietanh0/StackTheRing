namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using UITemplate.Scripts.UserData;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;

    public class LoadingScreenView : BaseView
    {
    }


    [ScreenInfo(nameof(LoadingScreenView))]
    public class LoadingScreenPresenter : BaseScreenPresenter<LoadingScreenView>
    {
        protected virtual string NextSceneName => "1.MainScene";
        #region Inject
        private readonly UserDataManager userDataManager;
        private readonly IGameAssets     gameAssets;

        public LoadingScreenPresenter(
            SignalBus      signalBus,
            ILoggerManager loggerManager,
            UserDataManager userDataManager,
            IGameAssets    gameAssets
        ) : base(signalBus, loggerManager)
        {
            this.userDataManager = userDataManager;
            this.gameAssets      = gameAssets;
        }
        #endregion


        public async override UniTask BindData()
        {
            await this.userDataManager.LoadUserData();
            await this.LoadSceneAsync();
        }

        protected virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync()
        {
            return this.gameAssets.LoadSceneAsync(this.NextSceneName);
        }
    }
}