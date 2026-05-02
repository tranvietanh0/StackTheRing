namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using System.Collections.Generic;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.Level.Blueprint;
    using HyperCasualGame.Scripts.Services;
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

        public float GetProgress()
        {
            return this.loadingFill != null ? this.loadingFill.fillAmount : 0f;
        }

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
        private const float ProgressAnimationSpeed = 2.5f;

        protected virtual string NextSceneName => "1.MainScene";

        #region Inject

        private readonly UserDataManager userDataManager;
        private readonly IGameAssets gameAssets;
        private readonly BlueprintReaderManager blueprintReaderManager;
        private readonly LevelBlueprintReader levelBlueprintReader;

        public LoadingScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            UserDataManager userDataManager,
            IGameAssets gameAssets,
            BlueprintReaderManager blueprintReaderManager,
            LevelBlueprintReader levelBlueprintReader
        ) : base(signalBus, loggerManager)
        {
            this.userDataManager = userDataManager;
            this.gameAssets = gameAssets;
            this.blueprintReaderManager = blueprintReaderManager;
            this.levelBlueprintReader = levelBlueprintReader;
        }

        #endregion

        public override async UniTask BindData()
        {
            this.View.Show();
            this.View.SetProgress(0f);

            await this.LoadUserDataWithProgress();
            await this.LoadBlueprintWithProgress();
            await this.PreloadStartupLevelsWithProgress();
            await this.LoadSceneWithProgress();
        }

        private async UniTask LoadUserDataWithProgress()
        {
            await this.AnimateProgressTo(0.05f);
            await this.userDataManager.LoadUserData();
            await this.AnimateProgressTo(0.1f);
        }

        private async UniTask LoadBlueprintWithProgress()
        {
            await this.AnimateProgressTo(0.15f);
            await this.blueprintReaderManager.LoadBlueprint();
            await this.AnimateProgressTo(0.25f);
        }

        private async UniTask PreloadStartupLevelsWithProgress()
        {
            await this.AnimateProgressTo(0.25f);

            var preloadKeys = this.ResolveStartupLevelKeys();
            if (preloadKeys.Count == 0)
            {
                await this.AnimateProgressTo(0.85f);
                return;
            }

            var handles = new List<(string key, AsyncOperationHandle<GameObject> handle)>(preloadKeys.Count);
            foreach (var preloadKey in preloadKeys)
            {
                var handle = this.gameAssets.LoadAssetAsync<GameObject>(preloadKey, true, this.NextSceneName);
                if (!handle.IsValid())
                {
                    this.Logger.Warning($"Startup level preload returned an invalid handle for key {preloadKey}.");
                    continue;
                }

                handles.Add((preloadKey, handle));
            }

            if (handles.Count == 0)
            {
                await this.AnimateProgressTo(0.85f);
                return;
            }

            var displayedProgress = 0.25f;
            while (true)
            {
                var totalProgress = 0f;
                var completedCount = 0;
                foreach (var preload in handles)
                {
                    totalProgress += preload.handle.PercentComplete;
                    if (preload.handle.IsDone)
                    {
                        completedCount++;
                    }
                }

                var averageProgress = totalProgress / handles.Count;
                var targetProgress = 0.25f + averageProgress * 0.6f;
                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * ProgressAnimationSpeed);
                this.View.SetProgress(displayedProgress);

                if (completedCount == handles.Count && displayedProgress >= targetProgress)
                {
                    break;
                }

                await UniTask.Yield();
            }

            this.Logger.Info($"Startup level preload completed: {handles.Count}/{preloadKeys.Count} level prefabs cached for {this.NextSceneName}.");

            foreach (var preload in handles)
            {
                if (preload.handle.Status == AsyncOperationStatus.Failed)
                {
                    this.Logger.Warning($"Startup level preload failed for {preload.key}: {preload.handle.OperationException?.Message}");
                    this.gameAssets.ReleaseAsset(preload.key);
                }
            }

            await this.AnimateProgressTo(0.85f);
        }

        private List<string> ResolveStartupLevelKeys()
        {
            var orderedLevels = this.levelBlueprintReader.GetOrderedLevels();
            var preloadKeys = new List<string>(orderedLevels.Count);
            for (var index = 0; index < orderedLevels.Count; index++)
            {
                this.TryAddLevelKey(preloadKeys, orderedLevels[index]);
            }

            return preloadKeys;
        }

        private void TryAddLevelKey(List<string> preloadKeys, int levelNumber)
        {
            var record = this.levelBlueprintReader.GetRecord(levelNumber);
            if (record == null || string.IsNullOrWhiteSpace(record.LevelName))
            {
                this.Logger.Warning($"Cannot resolve preload key for level {levelNumber}.");
                return;
            }

            if (preloadKeys.Contains(record.LevelName))
            {
                return;
            }

            preloadKeys.Add(record.LevelName);
        }

        private async UniTask LoadSceneWithProgress()
        {
            var handle = this.gameAssets.LoadSceneAsync(this.NextSceneName);
            var displayedProgress = 0.85f;

            while (!handle.IsDone)
            {
                var targetProgress = 0.85f + handle.PercentComplete * 0.15f;
                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * ProgressAnimationSpeed);
                this.View.SetProgress(displayedProgress);
                await UniTask.Yield();
            }

            await this.AnimateProgressTo(1f);
        }

        private async UniTask AnimateProgressTo(float targetProgress)
        {
            var currentProgress = this.View == null ? 0f : this.View.GetProgress();
            while (currentProgress < targetProgress)
            {
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.unscaledDeltaTime * ProgressAnimationSpeed);
                this.View.SetProgress(currentProgress);
                await UniTask.Yield();
            }
        }

        protected virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync()
        {
            return this.gameAssets.LoadSceneAsync(this.NextSceneName);
        }
    }
}
