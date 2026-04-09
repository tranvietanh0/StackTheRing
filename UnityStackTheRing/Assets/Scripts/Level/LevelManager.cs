namespace HyperCasualGame.Scripts.Level
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public interface ILevelManager
    {
        int CurrentLevel { get; }
        int HighestUnlockedLevel { get; }
        LevelData CurrentLevelData { get; }

        UniTask<LevelData> LoadLevel(int levelNumber);
        void CompleteLevel();
        void FailLevel();
        void SaveProgress();
    }

    public class LevelManager : ILevelManager
    {
        private const string ProgressKey = "LevelProgress";
        private const string LevelAssetPrefix = "Level_";

        #region Inject

        private readonly IGameAssets gameAssets;
        private readonly SignalBus signalBus;
        private readonly ILogger logger;

        public LevelManager(IGameAssets gameAssets, SignalBus signalBus, ILoggerManager loggerManager)
        {
            this.gameAssets = gameAssets;
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            this.LoadProgress();
        }

        #endregion

        #region Properties

        public int CurrentLevel { get; private set; } = 1;
        public int HighestUnlockedLevel { get; private set; } = 1;
        public LevelData CurrentLevelData { get; private set; }

        #endregion

        #region Public Methods

        public async UniTask<LevelData> LoadLevel(int levelNumber)
        {
            this.CurrentLevel = levelNumber;
            var levelKey = $"{LevelAssetPrefix}{levelNumber:D2}";

            this.logger.Info($"Loading level: {levelKey}");

            // Try Resources first (for development/testing)
            this.CurrentLevelData = Resources.Load<LevelData>($"Levels/{levelKey}");

            // Fallback to Addressables if not in Resources
            if (this.CurrentLevelData == null)
            {
                try
                {
                    var handle = this.gameAssets.LoadAssetAsync<LevelData>(levelKey);
                    await handle.Task;
                    this.CurrentLevelData = handle.Result;
                }
                catch (System.Exception ex)
                {
                    this.logger.Warning($"Addressables load failed: {ex.Message}");
                }
            }

            if (this.CurrentLevelData != null)
            {
                this.signalBus.Fire(new LevelStartSignal { LevelNumber = levelNumber });
                this.logger.Info($"Level {levelNumber} loaded successfully");
            }
            else
            {
                this.logger.Error($"Failed to load level: {levelKey}");
            }

            return this.CurrentLevelData;
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