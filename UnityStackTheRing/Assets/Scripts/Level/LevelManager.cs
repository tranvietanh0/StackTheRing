namespace HyperCasualGame.Scripts.Level
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Level.Blueprint;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.Services;
    using UniT.Logging;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using ILogger = UniT.Logging.ILogger;

    public interface ILevelManager
    {
        int CurrentLevel { get; }
        int HighestUnlockedLevel { get; }
        LevelData CurrentLevelData { get; }
        LevelController CurrentLevelController { get; }

        UniTask<LevelController> LoadLevel(int levelNumber);
        UniTask<LevelController> LoadCurrentLevel();
        UniTask<LevelController> LoadNextLevel();
        void UnloadCurrentLevel();
        void CompleteLevel();
        void FailLevel();
        void SaveProgress();
    }

    public class LevelManager : ILevelManager
    {
        #region Inject

        private readonly IGameAssets gameAssets;
        private readonly SignalBus signalBus;
        private readonly ILogger logger;
        private readonly Transform levelRoot;
        private readonly LevelBlueprintReader levelBlueprintReader;
        private readonly LocalDataController localDataController;
        private Action<LevelController> injectCallback;

        public LevelManager(
            IGameAssets gameAssets,
            SignalBus signalBus,
            ILoggerManager loggerManager,
            Transform levelRoot,
            LevelBlueprintReader levelBlueprintReader,
            LocalDataController localDataController)
        {
            this.gameAssets = gameAssets;
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);
            this.levelRoot = levelRoot;
            this.levelBlueprintReader = levelBlueprintReader;
            this.localDataController = localDataController;
        }

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int HighestUnlockedLevel { get; private set; } = 1;
        public LevelData CurrentLevelData { get; private set; }
        public LevelController CurrentLevelController { get; private set; }

        #endregion

        #region Public Methods

        public void SetInjectCallback(Action<LevelController> callback)
        {
            this.injectCallback = callback;
        }

        public async UniTask<LevelController> LoadLevel(int levelNumber)
        {
            if (this.levelBlueprintReader.Count == 0)
            {
                this.logger.Error("Level blueprint catalog is empty. Cannot load level.");
                return null;
            }

            var normalizedLevel = this.levelBlueprintReader.NormalizeLevel(levelNumber);
            var levelRecord = this.levelBlueprintReader.GetRecord(normalizedLevel);
            if (levelRecord == null || string.IsNullOrWhiteSpace(levelRecord.LevelName))
            {
                this.logger.Error($"Level blueprint record is missing or invalid for level {normalizedLevel}.");
                return null;
            }

            // Cleanup previous level
            this.UnloadCurrentLevel();

            this.CurrentLevel = normalizedLevel;
            await this.localDataController.SetCurrentLevel(this.CurrentLevel);
            this.HighestUnlockedLevel = Mathf.Max(1, await this.localDataController.GetHighestUnlockedLevel());
            var levelName = levelRecord.LevelName;

            this.logger.Info($"Loading level prefab: {levelName}");

            GameObject prefab = null;

            try
            {
                var handle = this.gameAssets.LoadAssetAsync<GameObject>(levelName);
                await handle.Task;
                prefab = handle.Result;
            }
            catch (Exception ex)
            {
                this.logger.Warning($"Addressables load failed: {ex.Message}");
            }

            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Levels/{levelName}");
                if (prefab != null)
                {
                    this.logger.Info("Loaded level from Resources fallback");
                }
            }

            if (prefab == null)
            {
                this.logger.Error($"Failed to load level prefab: {levelName}");
                return null;
            }

            var parent = this.levelRoot != null ? this.levelRoot : null;
            var levelGO = Object.Instantiate(prefab, parent);
            levelGO.name = levelName;

            this.CurrentLevelController = levelGO.GetComponent<LevelController>();
            if (this.CurrentLevelController == null)
            {
                this.logger.Error("Level prefab missing LevelController component!");
                Object.Destroy(levelGO);
                return null;
            }

            this.injectCallback?.Invoke(this.CurrentLevelController);
            this.CurrentLevelController.Initialize();
            this.CurrentLevelData = this.CurrentLevelController.LevelData;

            this.signalBus.Fire(new LevelStartSignal { LevelNumber = this.CurrentLevel });

            this.logger.Info($"Level {this.CurrentLevel} loaded successfully");
            return this.CurrentLevelController;
        }

        public void UnloadCurrentLevel()
        {
            if (this.CurrentLevelController != null)
            {
                Object.Destroy(this.CurrentLevelController.gameObject);
                this.CurrentLevelController = null;
                this.CurrentLevelData = null;

                this.logger.Info("Previous level unloaded");
            }
        }

        public async UniTask<LevelController> LoadCurrentLevel()
        {
            var currentLevel = await this.localDataController.GetCurrentLevel();
            return await this.LoadLevel(currentLevel);
        }

        public async UniTask<LevelController> LoadNextLevel()
        {
            var nextLevel = this.levelBlueprintReader.GetNextLevel(this.CurrentLevel);
            return await this.LoadLevel(nextLevel);
        }

        public void CompleteLevel()
        {
            var nextLevel = this.levelBlueprintReader.GetNextLevel(this.CurrentLevel);
            var candidateHighestLevel = nextLevel == this.levelBlueprintReader.GetMinLevel()
                ? this.levelBlueprintReader.GetMaxLevel()
                : nextLevel;
            if (candidateHighestLevel > this.HighestUnlockedLevel)
            {
                this.HighestUnlockedLevel = candidateHighestLevel;
                this.SaveProgress();
            }

            this.signalBus.Fire(new LevelWinSignal
            {
                LevelNumber = this.CurrentLevel,
                Score = this.CalculateScore()
            });
        }

        public void FailLevel()
        {
            this.signalBus.Fire(new LevelLoseSignal
            {
                LevelNumber = this.CurrentLevel
            });
        }

        public void SaveProgress()
        {
            this.localDataController.SetCurrentLevel(this.CurrentLevel, false).Forget();
            this.localDataController.SetHighestUnlockedLevel(this.HighestUnlockedLevel, false).Forget();
            this.localDataController.Save().Forget();
            this.logger.Info($"Progress saved. Highest unlocked: {this.HighestUnlockedLevel}");
        }

        #endregion

        #region Private Methods

        private int CalculateScore()
        {
            return 100;
        }

        #endregion
    }
}
