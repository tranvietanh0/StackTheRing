namespace HyperCasualGame.Scripts.Conveyor
{
    using System.Threading;
    using Cysharp.Threading.Tasks;
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
        private bool isTransferInProgress;
        private float transferCooldownTimer;
        private CancellationTokenSource transferCancellationTokenSource;

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
            this.ResetTransferCancellation();
            this.logger.Info("ConveyorFeeder initialized");
        }

        public void StartFeeding()
        {
            this.isActive = true;
            this.isTransferInProgress = false;
            this.transferCooldownTimer = 0f;
            this.ResetTransferCancellation();
            this.logger.Info("ConveyorFeeder started");
        }

        public void StopFeeding()
        {
            this.isActive = false;
            this.isTransferInProgress = false;
            this.ResetTransferCancellation();
            this.logger.Info("ConveyorFeeder stopped");
        }

        private void OnDestroy()
        {
            this.transferCancellationTokenSource?.Cancel();
            this.transferCancellationTokenSource?.Dispose();
        }

        private void Update()
        {
            if (!this.isActive || this.isTransferInProgress || this.queueConveyor == null || this.ringConveyor == null)
            {
                return;
            }

            if (this.queueConveyor.IsEmpty)
            {
                return;
            }

            if (this.transferCooldownTimer > 0f)
            {
                this.transferCooldownTimer -= Time.deltaTime;
                return;
            }

            this.TryTransferRow().Forget();
        }

        private async UniTask TryTransferRow()
        {
            this.isTransferInProgress = true;

            try
            {
                var cancellationToken = this.transferCancellationTokenSource?.Token ?? CancellationToken.None;
                var desiredSpacing = this.queueConveyor.GetDesiredRowSpacing();
                var sourcePosition = this.queueConveyor.GetTransferWorldPosition();
                if (!this.ringConveyor.TryCreateQueueInsertReservation(sourcePosition, desiredSpacing, out var reservation))
                {
                    return;
                }

                var row = this.queueConveyor.BeginTransfer();
                if (row == null)
                {
                    return;
                }

                var transferCompleted = await this.ringConveyor.HandoffRowBallFromQueueAsync(row, sourcePosition, reservation, cancellationToken);
                if (!transferCompleted)
                {
                    this.queueConveyor.CancelTransfer(row);
                    return;
                }

                this.queueConveyor.CompleteTransfer();
                this.transferCooldownTimer = 0f;

                if (this.queueConveyor.IsEmpty)
                {
                    this.ringConveyor.SetHasQueueRows(false);
                }
            }
            finally
            {
                this.isTransferInProgress = false;
            }
        }

        private void ResetTransferCancellation()
        {
            this.transferCancellationTokenSource?.Cancel();
            this.transferCancellationTokenSource?.Dispose();
            this.transferCancellationTokenSource = new CancellationTokenSource();
        }
    }
}
