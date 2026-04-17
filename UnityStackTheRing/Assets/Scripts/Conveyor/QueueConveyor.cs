namespace HyperCasualGame.Scripts.Conveyor
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Dreamteck.Splines;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class QueueConveyor : MonoBehaviour
    {
        [SerializeField] private ConveyorConfig config;
        [SerializeField] private Transform rowBallContainer;
        [SerializeField] private SplineComputer spline;
        [SerializeField] private Transform transferExitAnchor;
        [SerializeField] private RowBall rowBallPrefab;
        [SerializeField] private Ball ballPrefab;

        private readonly List<RowBall> queuedRowBalls = new();
        private ConveyorPath queuePath;
        private float baseSpeed = 1f;
        private float visualSyncSpeed = 1f;
        private float queueEntryDistance;
        private bool isRunning;
        private bool isCompacting;
        private bool isTransferInProgress;
        private int compactVersion;
        private RowBall transferringRow;
        private SignalBus signalBus;
        private ILogger logger;

        public IReadOnlyList<RowBall> QueuedRowBalls => this.queuedRowBalls;
        public IEnumerable<RowBall> PendingRowBalls => this.transferringRow != null
            ? this.queuedRowBalls.Prepend(this.transferringRow)
            : this.queuedRowBalls;
        public int QueuedRowCount => this.queuedRowBalls.Count;
        public bool IsEmpty => this.queuedRowBalls.Count == 0 && this.transferringRow == null;
        public bool IsRunning => this.isRunning;
        public bool IsCompacting => this.isCompacting;
        public bool IsReadyToTransfer => !this.isTransferInProgress && this.HasFrontRowAtEntry();
        public int TotalBallCount => this.PendingRowBalls.Sum(row => row.GetBallCount());

        public void Initialize(SignalBus signalBus, ILoggerManager loggerManager)
        {
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            if (this.spline == null)
            {
                this.logger.Error("QueueConveyor: SplineComputer not assigned!");
                return;
            }

            this.queuePath = new ConveyorPath(this.spline, 100);
            this.queueEntryDistance = this.CalculatePathLength();
            this.logger.Info($"QueueConveyor initialized. Path length: {this.queueEntryDistance:F2}");
        }

        public void SetupLevel(LevelData levelData, Ball ballPrefab, RowBall rowBallPrefab)
        {
            this.ClearAllRows();

            this.ballPrefab = ballPrefab;
            this.rowBallPrefab = rowBallPrefab;
            this.baseSpeed = levelData.QueueSpeed > 0 ? levelData.QueueSpeed : GameConstants.QueueConveyorConfig.DefaultQueueSpeed;
            this.visualSyncSpeed = levelData.ConveyorSpeed > 0 ? levelData.ConveyorSpeed : this.baseSpeed;
            this.isTransferInProgress = false;
            this.compactVersion = 0;
            this.transferringRow = null;

            this.SpawnQueueRows(levelData);

            if (this.isRunning)
            {
                this.CompactTowardEntry().Forget();
            }
        }

        public void StartQueue()
        {
            this.isRunning = true;
            this.CompactTowardEntry().Forget();
            this.logger.Info("QueueConveyor started");
        }

        public void StopQueue()
        {
            this.isRunning = false;
            this.isTransferInProgress = false;
            this.compactVersion++;

            foreach (var row in this.queuedRowBalls)
            {
                var follower = row.GetComponent<PathFollower>();
                follower?.StopMoving();
            }

            this.isCompacting = false;
            this.logger.Info("QueueConveyor stopped");
        }

        public RowBall BeginTransfer()
        {
            if (!this.IsReadyToTransfer)
            {
                return null;
            }

            var frontRow = this.queuedRowBalls[0];
            this.queuedRowBalls.RemoveAt(0);
            this.isTransferInProgress = true;
            this.transferringRow = frontRow;

            var follower = frontRow.GetComponent<PathFollower>();
            follower?.StopMoving();

            this.signalBus?.Fire(new QueueRowTransferredSignal
            {
                RowId = frontRow.RowId,
                RemainingQueueRows = this.queuedRowBalls.Count
            });

            if (this.queuedRowBalls.Count == 0)
            {
                this.signalBus?.Fire(new QueueEmptySignal());
            }
            else if (this.isRunning)
            {
                this.CompactTowardEntry().Forget();
            }

            return frontRow;
        }

        public void CompleteTransfer()
        {
            this.isTransferInProgress = false;
            this.transferringRow = null;
        }

        public void CancelTransfer(RowBall rowBall)
        {
            this.isTransferInProgress = false;
            this.transferringRow = null;

            if (rowBall == null)
            {
                return;
            }

            this.queuedRowBalls.Insert(0, rowBall);
            if (this.isRunning)
            {
                this.CompactTowardEntry().Forget();
            }
        }

        public Vector3 GetTransferWorldPosition()
        {
            if (this.transferExitAnchor != null)
            {
                return this.transferExitAnchor.position;
            }

            if (this.transferringRow != null)
            {
                return this.transferringRow.transform.position;
            }

            if (this.queuedRowBalls.Count > 0)
            {
                return this.queuedRowBalls[0].transform.position;
            }

            return this.GetPositionAtDistance(this.queueEntryDistance);
        }

        public float GetDesiredRowSpacing()
        {
            return this.config != null ? this.config.RowSpacing : GameConstants.DistanceThresholds.BallSpacing;
        }

        private async UniTask CompactTowardEntry()
        {
            if (this.queuedRowBalls.Count == 0)
            {
                this.isCompacting = false;
                return;
            }

            var spacing = this.GetDesiredRowSpacing();
            this.compactVersion++;
            this.isCompacting = true;

            for (var index = 0; index < this.queuedRowBalls.Count; index++)
            {
                var row = this.queuedRowBalls[index];
                if (row == null)
                {
                    continue;
                }

                var follower = row.GetComponent<PathFollower>();
                if (follower == null)
                {
                    continue;
                }

                var targetDistance = Mathf.Max(0f, this.queueEntryDistance - (index * spacing));
                follower.SlideToDistance(targetDistance, this.CalculateCompactDuration(follower, targetDistance)).Forget();
            }

            await UniTask.Yield();
            this.isCompacting = false;
        }

        private bool HasFrontRowAtEntry()
        {
            if (this.queuedRowBalls.Count == 0)
            {
                return false;
            }

            var follower = this.queuedRowBalls[0].GetComponent<PathFollower>();
            if (follower == null)
            {
                return false;
            }

            if (follower.IsSlidingOnPath() || follower.IsBlendingToPath())
            {
                return false;
            }

            return Mathf.Abs(this.queueEntryDistance - follower.GetCurrentDistance()) <= GameConstants.QueueConveyorConfig.EntryReadyThreshold;
        }

        private void SpawnQueueRows(LevelData levelData)
        {
            if (levelData.QueueRings == null || levelData.QueueRings.Length == 0)
            {
                return;
            }

            var spacing = this.GetDesiredRowSpacing();
            var pathLength = this.CalculatePathLength();
            this.queueEntryDistance = pathLength;

            var colorPool = new List<ColorType>();
            foreach (var ringSpawn in levelData.QueueRings)
            {
                for (var count = 0; count < ringSpawn.Count; count++)
                {
                    colorPool.Add(ringSpawn.Color);
                }
            }

            var maxRows = Mathf.FloorToInt(pathLength / Mathf.Max(spacing, 0.001f));
            var rowCount = Mathf.Min(maxRows + 1, colorPool.Count);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var colors = new ColorType[this.config != null ? this.config.BallsPerRow : GameConstants.RowBallConfig.MaxBalls];
                for (var colorIndex = 0; colorIndex < colors.Length; colorIndex++)
                {
                    colors[colorIndex] = colorPool[rowIndex];
                }

                var rowConfig = new RowBallConfig
                {
                    SpawnPosition = Vector3.zero,
                    BallColors = colors,
                    RowId = rowIndex
                };

                var startDistance = Mathf.Max(0f, this.queueEntryDistance - (rowIndex * spacing));
                this.SpawnQueueRow(rowConfig, startDistance);
            }

            this.logger.Info($"QueueConveyor spawned {rowCount} rows");
        }

        private void SpawnQueueRow(RowBallConfig rowConfig, float startDistance)
        {
            if (this.rowBallPrefab == null || this.ballPrefab == null)
            {
                this.logger.Error("QueueConveyor: prefabs not assigned!");
                return;
            }

            var container = this.rowBallContainer != null ? this.rowBallContainer : this.transform;
            var rowBall = Instantiate(this.rowBallPrefab, container);

            var follower = rowBall.GetComponent<PathFollower>();
            if (follower == null)
            {
                follower = rowBall.gameObject.AddComponent<PathFollower>();
            }

            follower.MoveSpeed = this.baseSpeed;
            follower.LoopPath = false;
            follower.ReverseDirection = false;
            follower.Initialize(this.queuePath, startDistance, "QUEUE");
            follower.StopMoving();

            rowBall.Initialize(rowConfig, this.ballPrefab, this.signalBus, this.config);
            this.queuedRowBalls.Add(rowBall);
        }

        private Vector3 GetPositionAtDistance(float distance)
        {
            if (this.queuePath == null)
            {
                return Vector3.zero;
            }

            var sampleCount = this.queuePath.GetSampleCount();
            if (sampleCount <= 1)
            {
                return this.queuePath.GetSample(0);
            }

            var pathLength = Mathf.Max(this.queueEntryDistance, 0.001f);
            var normalized = Mathf.Clamp01(distance / pathLength);
            var floatIndex = normalized * (sampleCount - 1);
            var index = Mathf.FloorToInt(floatIndex);
            var ratio = floatIndex - index;

            var current = this.queuePath.GetSample(index);
            var next = this.queuePath.GetSample(Mathf.Min(index + 1, sampleCount - 1));
            return Vector3.Lerp(current, next, ratio);
        }

        private float CalculateCompactDuration(PathFollower follower, float targetDistance)
        {
            if (follower == null)
            {
                return 0f;
            }

            var distanceDelta = Mathf.Abs(follower.GetCurrentDistance() - targetDistance);
            if (distanceDelta <= 0.0001f)
            {
                return 0f;
            }

            var speed = Mathf.Max(this.visualSyncSpeed, 0.0001f);
            return distanceDelta / speed;
        }

        private float CalculatePathLength()
        {
            if (this.queuePath == null)
            {
                return 0f;
            }

            var length = 0f;
            var sampleCount = this.queuePath.GetSampleCount();
            for (var index = 0; index < sampleCount - 1; index++)
            {
                length += Vector3.Distance(this.queuePath.GetSample(index), this.queuePath.GetSample(index + 1));
            }

            return length;
        }

        private void ClearAllRows()
        {
            if (this.transferringRow != null)
            {
                Destroy(this.transferringRow.gameObject);
                this.transferringRow = null;
            }

            foreach (var row in this.queuedRowBalls)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            this.queuedRowBalls.Clear();
        }

        private void OnDestroy()
        {
            this.ClearAllRows();
        }
    }
}
