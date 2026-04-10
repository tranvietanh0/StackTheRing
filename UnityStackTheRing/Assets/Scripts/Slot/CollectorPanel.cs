namespace HyperCasualGame.Scripts.Slot
{
    using System.Collections.Generic;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class CollectorPanel : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ColorCollector[] collectors;

        #endregion

        #region Private Fields

        private SignalBus signalBus;
        private ILogger logger;
        private readonly Dictionary<ColorType, ColorCollector> colorToCollector = new();
        private bool isInitialized;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus, ILoggerManager loggerManager)
        {
            if (this.isInitialized) return;
            this.isInitialized = true;

            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            this.signalBus.Subscribe<CollectorPlacedSignal>(this.OnCollectorPlaced);
        }

        public void SetupLevel(LevelData levelData)
        {
            this.colorToCollector.Clear();

            for (var i = 0; i < this.collectors.Length; i++)
            {
                this.collectors[i].gameObject.SetActive(false);
            }

            for (var i = 0; i < levelData.AvailableCollectors.Length && i < this.collectors.Length; i++)
            {
                var color = levelData.AvailableCollectors[i];
                var collector = this.collectors[i];

                collector.SetColor(color);
                collector.Initialize(this.signalBus);
                collector.gameObject.SetActive(true);

                this.colorToCollector[color] = collector;
            }

            this.logger.Info($"CollectorPanel setup with {levelData.AvailableCollectors.Length} collectors");
        }

        public void Cleanup()
        {
            this.signalBus.Unsubscribe<CollectorPlacedSignal>(this.OnCollectorPlaced);
        }

        public void ResetAllCollectors()
        {
            foreach (var collector in this.collectors)
            {
                collector.Reset();
            }
        }

        public void SetAllInteractable(bool interactable)
        {
            foreach (var collector in this.collectors)
            {
                collector.SetInteractable(interactable);
            }
        }

        #endregion

        #region Private Methods

        private void OnCollectorPlaced(CollectorPlacedSignal signal)
        {
            if (this.colorToCollector.TryGetValue(signal.Color, out var collector))
            {
                collector.MarkAsPlaced();
            }
        }

        #endregion
    }
}