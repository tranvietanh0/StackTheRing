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
        protected virtual string LevelPrefabKey => "Level_01";

        #region Inject

        private readonly UserDataManager userDataManager;
        private readonly IGameAssets gameAssets;

        public LoadingScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            UserDataManager userDataManager,
            IGameAssets gameAssets
        ) : base(signalBus, loggerManager)
        {
            this.userDataManager = userDataManager;
            this.gameAssets = gameAssets;
        }

        #endregion

        public override async UniTask BindData()
        {
            // Show view immediately so loading bar is visible during loading
            this.View.Show();
            this.View.SetProgress(0f);

            // Phase 1: Load user data (0% - 20%)
            await this.LoadUserDataWithProgress();

            // Phase 2: Preload level prefab (20% - 80%)
            await this.PreloadLevelWithProgress();

            // Phase 3: Load main scene (80% - 100%)
            await this.LoadSceneWithProgress();
        }

        private async UniTask LoadUserDataWithProgress()
        {
            this.View.SetProgress(0.05f);
            await this.userDataManager.LoadUserData();
            this.View.SetProgress(0.2f);
        }

        private async UniTask PreloadLevelWithProgress()
        {
            var handle = this.gameAssets.LoadAssetAsync<GameObject>(this.LevelPrefabKey);

            while (!handle.IsDone)
            {
                // Map progress from 0.2 to 0.8
                var progress = 0.2f + handle.PercentComplete * 0.6f;
                this.View.SetProgress(progress);
                await UniTask.Yield();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                this.Logger.Info($"Level prefab '{this.LevelPrefabKey}' preloaded successfully");
            }
            else
            {
                this.Logger.Warning($"Failed to preload level prefab '{this.LevelPrefabKey}', will use Resources fallback");
            }

            this.View.SetProgress(0.8f);
        }

        private async UniTask LoadSceneWithProgress()
        {
            var handle = this.gameAssets.LoadSceneAsync(this.NextSceneName);

            while (!handle.IsDone)
            {
                // Map progress from 0.8 to 1.0
                var progress = 0.8f + handle.PercentComplete * 0.2f;
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
