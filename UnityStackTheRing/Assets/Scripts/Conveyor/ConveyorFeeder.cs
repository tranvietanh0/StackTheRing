namespace HyperCasualGame.Scripts.Conveyor
{
    using HyperCasualGame.Scripts.Core;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class ConveyorFeeder : MonoBehaviour
    {
        private ConveyorController ringConveyor;
        private QueueConveyor queueConveyor;
        private ILogger logger;
        private bool isActive;

        public bool IsActive => this.isActive;
        public bool HasQueueRows => this.queueConveyor != null && !this.queueConveyor.IsEmpty;

        public void Initialize(
            ConveyorController ringConveyor,
            QueueConveyor queueConveyor,
            ILoggerManager loggerManager)
        {
            this.ringConveyor = ringConveyor;
            this.queueConveyor = queueConveyor;
            this.logger = loggerManager.GetLogger(this);
            this.logger.Info("ConveyorFeeder initialized");
        }

        public void StartFeeding()
        {
            this.isActive = true;
            this.logger.Info("ConveyorFeeder started");
        }

        public void StopFeeding()
        {
            this.isActive = false;
            this.logger.Info("ConveyorFeeder stopped");
        }

        private void Update()
        {
            if (!this.isActive || this.queueConveyor == null || this.ringConveyor == null)
            {
                return;
            }

            if (!this.queueConveyor.HasReadyRow)
            {
                return;
            }

            var desiredSpacing = this.queueConveyor.GetDesiredRowSpacing();
            if (!this.ringConveyor.TryGetSubInsertDistance(desiredSpacing, out var insertDistance))
            {
                return;
            }

            var row = this.queueConveyor.PopReadyRow();
            if (row == null)
            {
                return;
            }

            this.ringConveyor.InsertRowBall(row, insertDistance);
            this.ringConveyor.SetHasQueueRows(!this.queueConveyor.IsEmpty);
        }
    }
}
