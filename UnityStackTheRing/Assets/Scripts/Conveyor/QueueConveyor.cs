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
        private readonly List<RowBall> readyToFill = new();
        private ConveyorPath queuePath;
        private float queueSpeed = 1f;
        private float queueEntryDistance;
        private bool isRunning;
        private SignalBus signalBus;
        private ILogger logger;

        public IReadOnlyList<RowBall> QueuedRowBalls => this.queuedRowBalls;
        public IEnumerable<RowBall> PendingRowBalls => this.queuedRowBalls;
        public IEnumerable<RowBall> ReadyRows => this.readyToFill;
        public bool IsEmpty => this.queuedRowBalls.Count == 0;
        public bool IsRunning => this.isRunning;
        public bool HasReadyRow => this.readyToFill.Count > 0;
        public int TotalBallCount => this.queuedRowBalls.Sum(row => row.GetBallCount());

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
            this.queueEntryDistance = this.transferExitAnchor != null
                ? this.FindClosestPathDistance(this.transferExitAnchor.position)
                : 0f;
        }

        public void SetupLevel(QueueLaneData laneData, Ball ballPrefab, RowBall rowBallPrefab, float conveyorSpeed)
        {
            this.ClearAllRows();

            this.ballPrefab = ballPrefab;
            this.rowBallPrefab = rowBallPrefab;
            this.queueSpeed = laneData.QueueSpeed > 0 ? laneData.QueueSpeed : conveyorSpeed;

            this.SpawnQueueRows(laneData);
            this.RefreshReadyRows();
        }

        public void StartQueue()
        {
            this.isRunning = true;
            this.ResumeActiveRows();
            this.RefreshReadyRows();
            this.logger.Info("QueueConveyor started");
        }

        public void StopQueue()
        {
            this.isRunning = false;

            foreach (var row in this.queuedRowBalls)
            {
                row.GetComponent<PathFollower>()?.StopMoving();
            }

            this.logger.Info("QueueConveyor stopped");
        }

        public RowBall PopReadyRow()
        {
            if (this.readyToFill.Count == 0 || this.queuedRowBalls.Count == 0)
            {
                return null;
            }

            var rowBall = this.queuedRowBalls[0];
            this.readyToFill.RemoveAt(0);
            this.queuedRowBalls.RemoveAt(0);

            var follower = rowBall.GetComponent<PathFollower>();
            follower?.StopMoving();
            rowBall.transform.SetParent(null, true);

            this.InvalidateQueueFollowerCaches();
            this.ResumeActiveRows();
            this.RefreshReadyRows();

            return rowBall;
        }

        public Vector3 GetTransferWorldPosition()
        {
            if (this.transferExitAnchor != null)
            {
                return this.transferExitAnchor.position;
            }

            if (this.readyToFill.Count > 0)
            {
                return this.readyToFill[0].transform.position;
            }

            return this.GetPositionAtDistance(this.queueEntryDistance);
        }

        public float GetDesiredRowSpacing()
        {
            return this.config != null ? this.config.RowSpacing : GameConstants.DistanceThresholds.BallSpacing;
        }

        private void Update()
        {
            if (!this.isRunning)
            {
                return;
            }

            this.UpdateQueueSlots();
        }

        private void RefreshReadyRows()
        {
            this.readyToFill.Clear();

            if (this.queuedRowBalls.Count == 0)
            {
                return;
            }

            var frontRow = this.queuedRowBalls[0];
            var frontFollower = frontRow != null ? frontRow.GetComponent<PathFollower>() : null;
            if (frontFollower == null)
            {
                return;
            }

            if (Mathf.Abs(frontFollower.GetCurrentDistance() - this.queueEntryDistance) <= GameConstants.QueueConveyorConfig.EntryReadyThreshold)
            {
                frontFollower.StopMoving();
                this.readyToFill.Add(frontRow);
            }
        }

        private void UpdateQueueSlots()
        {
            var spacing = this.GetDesiredRowSpacing();
            var pathLength = this.CalculatePathLength();

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

                var targetDistance = Mathf.Min(pathLength, this.queueEntryDistance + (index * spacing));
                var currentDistance = follower.GetCurrentDistance();
                var delta = currentDistance - targetDistance;

                if (delta > GameConstants.QueueConveyorConfig.EntryReadyThreshold)
                {
                    follower.MoveTowardDistance(targetDistance, Time.deltaTime);
                }
                else if (delta < -GameConstants.QueueConveyorConfig.EntryReadyThreshold)
                {
                    follower.SetDistance(targetDistance);
                }
                else
                {
                    follower.SetDistance(targetDistance);
                }
            }

            foreach (var row in this.queuedRowBalls)
            {
                var follower = row != null ? row.GetComponent<PathFollower>() : null;
                follower?.InvalidateSiblingCache();
            }

            this.RefreshReadyRows();
        }

        private void ResumeActiveRows()
        {
            this.UpdateQueueSlots();
        }

        private void SpawnQueueRows(QueueLaneData laneData)
        {
            if (laneData.QueueRings == null || laneData.QueueRings.Length == 0)
            {
                return;
            }

            var spacing = this.GetDesiredRowSpacing();
            var pathLength = this.CalculatePathLength();
            var spawnableLength = Mathf.Max(0f, pathLength - this.queueEntryDistance);

            var colorPool = new List<ColorType>();
            foreach (var ringSpawn in laneData.QueueRings)
            {
                for (var count = 0; count < ringSpawn.Count; count++)
                {
                    colorPool.Add(ringSpawn.Color);
                }
            }

            var maxRows = Mathf.FloorToInt(spawnableLength / Mathf.Max(spacing, 0.001f)) + 1;
            var rowCount = Mathf.Min(maxRows, colorPool.Count);

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

                var startDistance = Mathf.Min(pathLength, this.queueEntryDistance + (rowIndex * spacing));
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

            follower.MoveSpeed = this.queueSpeed;
            follower.LoopPath = false;
            follower.ReverseDirection = true;
            follower.UseSiblingSpacing = false;
            follower.UseExternalMovement = true;
            follower.Initialize(this.queuePath, startDistance, "QUEUE");

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

            var pathLength = Mathf.Max(this.CalculatePathLength(), 0.001f);
            var normalized = Mathf.Clamp01(distance / pathLength);
            var floatIndex = normalized * (sampleCount - 1);
            var index = Mathf.FloorToInt(floatIndex);
            var ratio = floatIndex - index;

            var current = this.queuePath.GetSample(index);
            var next = this.queuePath.GetSample(Mathf.Min(index + 1, sampleCount - 1));
            return Vector3.Lerp(current, next, ratio);
        }

        private float FindClosestPathDistance(Vector3 worldPosition)
        {
            if (this.queuePath == null)
            {
                return 0f;
            }

            var sampleCount = this.queuePath.GetSampleCount();
            var closestDistance = 0f;
            var closestMagnitude = float.MaxValue;
            var distanceCursor = 0f;

            for (var sampleIndex = 0; sampleIndex < sampleCount - 1; sampleIndex++)
            {
                var current = this.queuePath.GetSample(sampleIndex);
                var next = this.queuePath.GetSample(sampleIndex + 1);
                var segment = next - current;
                var segmentLength = segment.magnitude;
                if (segmentLength <= 0.0001f)
                {
                    continue;
                }

                var direction = segment / segmentLength;
                var projection = Mathf.Clamp(Vector3.Dot(worldPosition - current, direction), 0f, segmentLength);
                var pointOnSegment = current + (direction * projection);
                var magnitude = Vector3.SqrMagnitude(pointOnSegment - worldPosition);
                if (magnitude < closestMagnitude)
                {
                    closestMagnitude = magnitude;
                    closestDistance = distanceCursor + projection;
                }

                distanceCursor += segmentLength;
            }

            return closestDistance;
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
            foreach (var row in this.queuedRowBalls)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            this.queuedRowBalls.Clear();
            this.readyToFill.Clear();
        }

        private void InvalidateQueueFollowerCaches()
        {
            foreach (var row in this.queuedRowBalls)
            {
                var follower = row != null ? row.GetComponent<PathFollower>() : null;
                follower?.InvalidateSiblingCache();
            }
        }

        private void OnDestroy()
        {
            this.ClearAllRows();
        }
    }
}
