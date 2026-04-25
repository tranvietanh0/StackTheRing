namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using BlueprintFlow.BlueprintControlFlow;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using UITemplate.Scripts.UserData;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.Scripting;
    using UnityEngine.UI;

    [Preserve]
    public class LoadingScreenView : BaseView
    {
        [SerializeField] private Image loadingFill;

        public void SetProgress(float progress)
        {
            if (this.loadingFill != null)
            {
                this.loadingFill.fillAmount = Mathf.Clamp01(progress);
            }
        }
    }

    [Preserve]
    [ScreenInfo(nameof(LoadingScreenView))]
    public class LoadingScreenPresenter : BaseScreenPresenter<LoadingScreenView>
    {
        protected virtual string NextSceneName => "1.MainScene";

        #region Inject

        private readonly UserDataManager userDataManager;
        private readonly IGameAssets gameAssets;
        private readonly BlueprintReaderManager blueprintReaderManager;

        public LoadingScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            UserDataManager userDataManager,
            IGameAssets gameAssets,
            BlueprintReaderManager blueprintReaderManager
        ) : base(signalBus, loggerManager)
        {
            this.userDataManager = userDataManager;
            this.gameAssets = gameAssets;
            this.blueprintReaderManager = blueprintReaderManager;
        }

        #endregion

        public override async UniTask BindData()
        {
            // Show view immediately so loading bar is visible during loading
            this.View.Show();
            this.View.SetProgress(0f);

            // Phase 1: Load user data (0% - 20%)
            await this.LoadUserDataWithProgress();

            // Phase 2: Load blueprint catalog (20% - 60%)
            await this.LoadBlueprintWithProgress();

            // Phase 3: Load main scene (60% - 100%)
            await this.LoadSceneWithProgress();
        }

        private async UniTask LoadUserDataWithProgress()
        {
            this.View.SetProgress(0.05f);
            await this.userDataManager.LoadUserData();
            this.View.SetProgress(0.2f);
        }

        private async UniTask LoadBlueprintWithProgress()
        {
            this.View.SetProgress(0.25f);
            await this.blueprintReaderManager.LoadBlueprint();
            this.View.SetProgress(0.6f);
        }

        private async UniTask LoadSceneWithProgress()
        {
            var handle = this.gameAssets.LoadSceneAsync(this.NextSceneName);

            while (!handle.IsDone)
            {
                // Map progress from 0.6 to 1.0
                var progress = 0.6f + handle.PercentComplete * 0.4f;
                this.View.SetProgress(progress);
                await UniTask.Yield();
            }

            this.View.SetProgress(1f);
        }

        protected virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync()
        {
            return this.gameAssets.LoadSceneAsync(this.NextSceneName);
        }
    }
}
