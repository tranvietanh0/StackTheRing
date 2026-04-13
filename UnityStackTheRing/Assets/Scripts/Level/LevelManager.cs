namespace HyperCasualGame.Scripts.Level
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Signals;
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
        void UnloadCurrentLevel();
        void CompleteLevel();
        void FailLevel();
        void SaveProgress();
    }

    public class LevelManager : ILevelManager
    {
        private const string ProgressKey = "LevelProgress";
        private const string LevelPrefabPrefix = "Level_";

        #region Inject

        private readonly IGameAssets gameAssets;
        private readonly SignalBus signalBus;
        private readonly ILogger logger;
        private readonly Transform levelRoot;
        private Action<LevelController> injectCallback;

        public LevelManager(
            IGameAssets gameAssets,
            SignalBus signalBus,
            ILoggerManager loggerManager,
            Transform levelRoot)
        {
            this.gameAssets = gameAssets;
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);
            this.levelRoot = levelRoot;

            this.LoadProgress();
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
            // Cleanup previous level
            this.UnloadCurrentLevel();

            this.CurrentLevel = levelNumber;
            var prefabKey = $"{LevelPrefabPrefix}{levelNumber:D2}";

            this.logger.Info($"Loading level prefab: {prefabKey}");

            GameObject prefab = null;

            // Try Addressables first
            try
            {
                var handle = this.gameAssets.LoadAssetAsync<GameObject>(prefabKey);
                await handle.Task;
                prefab = handle.Result;
            }
            catch (Exception ex)
            {
                this.logger.Warning($"Addressables load failed: {ex.Message}");
            }

            // Fallback to Resources
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Levels/{prefabKey}");
                if (prefab != null)
                {
                    this.logger.Info("Loaded level from Resources fallback");
                }
            }

            if (prefab == null)
            {
                this.logger.Error($"Failed to load level prefab: {prefabKey}");
                return null;
            }

            // Instantiate
            var parent = this.levelRoot != null ? this.levelRoot : null;
            var levelGO = Object.Instantiate(prefab, parent);
            levelGO.name = $"Level_{levelNumber:D2}";

            // Get controller
            this.CurrentLevelController = levelGO.GetComponent<LevelController>();
            if (this.CurrentLevelController == null)
            {
                this.logger.Error("Level prefab missing LevelController component!");
                Object.Destroy(levelGO);
                return null;
            }

            // Inject dependencies
            this.injectCallback?.Invoke(this.CurrentLevelController);

            // Initialize the controller (IInitializable)
            this.CurrentLevelController.Initialize();

            // Cache LevelData from controller
            this.CurrentLevelData = this.CurrentLevelController.LevelData;

            // Fire signal
            this.signalBus.Fire(new LevelStartSignal { LevelNumber = levelNumber });

            this.logger.Info($"Level {levelNumber} loaded successfully");
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

        public void CompleteLevel()
        {
            if (this.CurrentLevel >= this.HighestUnlockedLevel)
            {
                this.HighestUnlockedLevel = this.CurrentLevel + 1;
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
            PlayerPrefs.SetInt(ProgressKey, this.HighestUnlockedLevel);
            PlayerPrefs.Save();
            this.logger.Info($"Progress saved. Highest unlocked: {this.HighestUnlockedLevel}");
        }

        #endregion

        #region Private Methods

        private void LoadProgress()
        {
            this.HighestUnlockedLevel = PlayerPrefs.GetInt(ProgressKey, 1);
            this.logger.Info($"Progress loaded. Highest unlocked: {this.HighestUnlockedLevel}");
        }

        private int CalculateScore()
        {
            return 100;
        }

        #endregion
    }
}
