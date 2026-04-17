namespace HyperCasualGame.Scripts.Conveyor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using Dreamteck.Splines;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    /// <summary>
    /// Main conveyor controller. Matches Cocos MainConveyorController.
    /// </summary>
    public class ConveyorController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ConveyorConfig config;
        [SerializeField] private Transform rowBallContainer;
        [SerializeField] private SplineComputer spline;
        [SerializeField] private RowBall rowBallPrefab;
        [SerializeField] private Ball ballPrefab;
        [SerializeField] private Transform queueInsertAnchor;

        [Header("Entry Points")]
        [SerializeField] private List<Transform> entryNodes = new();

        #endregion

        #region Private Fields

        private readonly List<RowBall> activeRowBalls = new();
        private readonly HashSet<RowBall> processingAtEntry = new();
        private ConveyorPath conveyorPath;
        private float baseSpeed = 1f;
        private bool isRunning;
        private bool hasQueueRows;
        private SignalBus signalBus;
        private ILogger logger;
        private CollectAreaBucketService collectAreaBucketService;

        #endregion

        #region Properties

        public IReadOnlyList<RowBall> ActiveRowBalls => this.activeRowBalls;

        public int ActiveRowCount => this.activeRowBalls.Count;

        public int TotalBallCount => this.activeRowBalls.Sum(r => r.GetBallCount());

        public bool IsRunning => this.isRunning;

        public int BallsPerRow => this.config != null ? this.config.BallsPerRow : GameConstants.RowBallConfig.MaxBalls;

        public ConveyorPath Path => this.conveyorPath;

        #endregion

        #region Events

        public event Action<RowBall> OnRowBallCompletedLoop;
        public event Action OnAllBallsCleared;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus, ILoggerManager loggerManager)
        {
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            if (this.spline == null)
            {
                this.logger.Error("SplineComputer not assigned!");
                return;
            }

            // Build path from spline samples
            this.conveyorPath = new ConveyorPath(this.spline, 100);

            this.logger.Info($"ConveyorController initialized. Path length: {this.conveyorPath.GetSampleCount()} samples");
        }

        public void SetCollectAreaBucketService(CollectAreaBucketService service)
        {
            Debug.Log($"[ConveyorController] SetCollectAreaBucketService called with {(service != null ? "valid service" : "NULL")}");
            this.collectAreaBucketService = service;
        }

        public void SetupLevel(LevelData levelData, Ball ballPrefab, RowBall rowBallPrefab)
        {
            this.ClearAllRowBalls();

            this.ballPrefab = ballPrefab;
            this.rowBallPrefab = rowBallPrefab;
            this.baseSpeed = levelData.ConveyorSpeed;

            this.SpawnRowBalls(levelData);
        }

        public void StartConveyor()
        {
            this.isRunning = true;

            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                follower?.StartMoving();
            }

            this.logger.Info("Conveyor started");
        }

        public void StopConveyor()
        {
            this.isRunning = false;

            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                follower?.StopMoving();
            }

            this.logger.Info("Conveyor stopped");
        }

        public void PauseConveyor()
        {
            this.isRunning = false;
            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                follower?.StopMoving();
            }
        }

        public void ResumeConveyor()
        {
            this.isRunning = true;
            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                follower?.StartMoving();
            }
        }

        public RowBall GetRowBallAtDistance(float distance, float tolerance = 0.5f)
        {
            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                if (follower == null)
                {
                    continue;
                }

                var diff = Mathf.Abs(follower.GetCurrentDistance() - distance);
                if (diff < tolerance)
                {
                    return rowBall;
                }
            }

            return null;
        }

        public List<Ball> GetBallsInRange(float startDistance, float endDistance)
        {
            var result = new List<Ball>();

            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                if (follower == null)
                {
                    continue;
                }

                var dist = follower.GetCurrentDistance();
                if (dist >= startDistance && dist <= endDistance)
                {
                    result.AddRange(rowBall.GetActiveBalls());
                }
            }

            return result;
        }

        public void RemoveRowBall(RowBall rowBall)
        {
            if (!this.activeRowBalls.Contains(rowBall))
            {
                return;
            }

            this.activeRowBalls.Remove(rowBall);
            this.InvalidateActiveFollowerCaches();
            Destroy(rowBall.gameObject);

            this.logger.Info($"RowBall removed. Remaining: {this.ActiveRowCount}");

            if (this.TotalBallCount == 0 && !this.hasQueueRows)
            {
                this.OnAllBallsCleared?.Invoke();
                this.signalBus.Fire(new AllRingsClearedSignal());
            }
        }

        /// <summary>
        /// Set whether the queue still has rows to send.
        /// Prevents premature AllRingsClearedSignal.
        /// </summary>
        public void SetHasQueueRows(bool hasRows)
        {
            this.hasQueueRows = hasRows;
        }

        /// <summary>
        /// Insert a RowBall from the queue conveyor into the ring at the given distance.
        /// Re-parents the row, configures PathFollower for loop path.
        /// </summary>
        public void InsertRowBall(RowBall rowBall, float insertDistance)
        {
            if (rowBall == null)
            {
                return;
            }

            var container = this.rowBallContainer != null ? this.rowBallContainer : this.transform;
            rowBall.transform.SetParent(container, true);

            // Reconfigure PathFollower for the ring (loop path)
            var follower = rowBall.GetComponent<PathFollower>();
            if (follower == null)
            {
                follower = rowBall.gameObject.AddComponent<PathFollower>();
            }

            follower.MoveSpeed = this.baseSpeed;
            follower.LoopPath = true;
            follower.ReverseDirection = false;

            if (this.entryNodes.Count > 0)
            {
                follower.SetEntryNodes(this.entryNodes);
            }

            follower.Initialize(this.conveyorPath, insertDistance, "MAIN");
            follower.ComputeEntryPathDistances();

            this.activeRowBalls.Add(rowBall);
            this.InvalidateActiveFollowerCaches();

            if (this.isRunning)
            {
                follower.StartMoving();
            }

            this.logger.Info($"InsertRowBall id={rowBall.RowId} at distance={insertDistance:F2}. Active: {this.ActiveRowCount}");
        }

        public bool TryGetSubInsertDistance(float desiredSpacing, out float insertDistance)
        {
            insertDistance = 0f;
            if (this.conveyorPath == null)
            {
                return false;
            }

            var anchorTransform = this.queueInsertAnchor != null
                ? this.queueInsertAnchor
                : (this.entryNodes.Count > 0 ? this.entryNodes[0] : null);
            if (anchorTransform == null)
            {
                return false;
            }

            var anchorDistance = this.FindClosestPathDistance(anchorTransform.position);
            if (this.activeRowBalls.Count == 0)
            {
                insertDistance = anchorDistance;
                return true;
            }

            var pathLength = this.CalculatePathLength();
            var nearestDistanceAroundAnchor = float.MaxValue;

            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                if (follower == null)
                {
                    continue;
                }

                var anchorDelta = Mathf.Abs(follower.GetCurrentDistance() - anchorDistance);
                if (anchorDelta > pathLength * 0.5f)
                {
                    anchorDelta = pathLength - anchorDelta;
                }

                if (anchorDelta < nearestDistanceAroundAnchor)
                {
                    nearestDistanceAroundAnchor = anchorDelta;
                }
            }

            if (nearestDistanceAroundAnchor == float.MaxValue)
            {
                insertDistance = anchorDistance;
                return true;
            }

            var requiredClearance = desiredSpacing + GameConstants.QueueConveyorConfig.EntryInsertBuffer;
            if (nearestDistanceAroundAnchor < requiredClearance)
            {
                return false;
            }

            insertDistance = anchorDistance;
            return true;
        }

        /// <summary>
        /// Find the largest gap on the ring conveyor path.
        /// Returns the distance at the center of the gap and the gap size.
        /// </summary>
        public bool FindLargestGap(out float gapCenterDistance, out float gapSize)
        {
            gapCenterDistance = 0f;
            gapSize = 0f;

            if (this.activeRowBalls.Count == 0)
            {
                // Entire path is empty
                var totalLength = this.CalculatePathLength();
                gapCenterDistance = totalLength / 2f;
                gapSize = totalLength;
                return true;
            }

            // Collect all distances and sort
            var distances = new List<float>();
            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                if (follower != null)
                {
                    distances.Add(follower.GetCurrentDistance());
                }
            }

            if (distances.Count == 0) return false;

            distances.Sort();

            var pathLength = this.CalculatePathLength();
            var bestGap = 0f;
            var bestCenter = 0f;

            // Check gaps between consecutive rows
            for (var i = 0; i < distances.Count - 1; i++)
            {
                var gap = distances[i + 1] - distances[i];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestCenter = distances[i] + gap / 2f;
                }
            }

            // Check wrap-around gap (last to first)
            var wrapGap = pathLength - distances[^1] + distances[0];
            if (wrapGap > bestGap)
            {
                bestGap = wrapGap;
                bestCenter = (distances[^1] + wrapGap / 2f) % pathLength;
            }

            gapCenterDistance = bestCenter;
            gapSize = bestGap;
            return bestGap > 0;
        }

        public Vector3 GetPositionAtDistance(float distance)
        {
            if (this.conveyorPath == null)
            {
                return Vector3.zero;
            }

            // Simple linear interpolation on path samples
            var sampleCount = this.conveyorPath.GetSampleCount();
            var pathLength = this.CalculatePathLength();

            var t = distance / pathLength;
            t = Mathf.Repeat(t, 1f);

            var floatIndex = t * (sampleCount - 1);
            var index = Mathf.FloorToInt(floatIndex);
            var frac = floatIndex - index;

            var p1 = this.conveyorPath.GetSample(index);
            var p2 = this.conveyorPath.GetSample((index + 1) % sampleCount);

            return Vector3.Lerp(p1, p2, frac);
        }

        public Quaternion GetRotationAtDistance(float distance)
        {
            var position = this.GetPositionAtDistance(distance);
            var nextPosition = this.GetPositionAtDistance(distance + 0.2f);
            var direction = nextPosition - position;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            direction.Normalize();
            return Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
        }

        #endregion

        #region Private Methods

        private void SpawnRowBalls(LevelData levelData)
        {
            // Calculate path length
            var pathLength = this.CalculatePathLength();
            var spacing = this.config != null ? this.config.RowSpacing : 1f;
            var maxRows = Mathf.FloorToInt(pathLength / spacing);

            this.logger.Info($"SpawnRowBalls: pathLength={pathLength:F2}, spacing={spacing}, maxRows={maxRows}");

            // Build color pool from level data (keep colors grouped like Cocos)
            var colorPool = new List<ColorType>();
            foreach (var ringSpawn in levelData.Rings)
            {
                for (var i = 0; i < ringSpawn.Count; i++)
                {
                    colorPool.Add(ringSpawn.Color);
                }
            }

            // Spawn rows
            var rowCount = Mathf.Min(maxRows, colorPool.Count);
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var color = colorPool[rowIndex];
                var startDistance = rowIndex * spacing;

                // Create RowBall config - all balls same color
                var ballsPerRow = this.config != null ? this.config.BallsPerRow : GameConstants.RowBallConfig.MaxBalls;
                var colors = new ColorType[ballsPerRow];
                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = color;
                }

                var rowConfig = new RowBallConfig
                {
                    SpawnPosition = Vector3.zero,
                    BallColors = colors,
                    RowId = rowIndex
                };

                this.SpawnRowBall(rowConfig, startDistance);
            }

            var ballsPerRowForLog = this.config != null ? this.config.BallsPerRow : GameConstants.RowBallConfig.MaxBalls;
            this.logger.Info($"Spawned {rowCount} rows ({rowCount * ballsPerRowForLog} balls). Path length: {pathLength:F2}, Max capacity: {maxRows}");
        }

        private float FindClosestPathDistance(Vector3 worldPosition)
        {
            var pathLength = this.CalculatePathLength();
            if (pathLength <= 0f || this.conveyorPath == null)
            {
                return 0f;
            }

            var sampleCount = this.conveyorPath.GetSampleCount();
            var closestDistance = 0f;
            var closestMagnitude = float.MaxValue;
            var distanceCursor = 0f;

            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var current = this.conveyorPath.GetSample(sampleIndex);
                var next = this.conveyorPath.GetSample((sampleIndex + 1) % sampleCount);
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
                if (!this.IsLoopPathSegment(sampleIndex, sampleCount))
                {
                    break;
                }
            }

            return Mathf.Repeat(closestDistance, pathLength);
        }

        private bool IsLoopPathSegment(int sampleIndex, int sampleCount)
        {
            return sampleIndex < sampleCount - 1 || (sampleIndex == sampleCount - 1 && sampleCount > 1);
        }

        private void InvalidateActiveFollowerCaches()
        {
            foreach (var rowBall in this.activeRowBalls)
            {
                var follower = rowBall != null ? rowBall.GetComponent<PathFollower>() : null;
                follower?.InvalidateSiblingCache();
            }
        }

        private void SpawnRowBall(RowBallConfig config, float startDistance)
        {
            if (this.rowBallPrefab == null || this.ballPrefab == null)
            {
                this.logger.Error("RowBall or Ball prefab not assigned!");
                return;
            }

            var container = this.rowBallContainer != null ? this.rowBallContainer : this.transform;
            var rowBall = Instantiate(this.rowBallPrefab, container);

            // Add PathFollower
            var follower = rowBall.GetComponent<PathFollower>();
            if (follower == null)
            {
                follower = rowBall.gameObject.AddComponent<PathFollower>();
            }

            // Configure follower
            follower.MoveSpeed = this.baseSpeed;
            follower.LoopPath = true;
            follower.ReverseDirection = false;

            Debug.Log($"[ConveyorController] entryNodes.Count = {this.entryNodes.Count}");
            if (this.entryNodes.Count > 0)
            {
                follower.SetEntryNodes(this.entryNodes);
            }
            else
            {
                Debug.LogWarning("[ConveyorController] No entry nodes configured!");
            }

            // Initialize path following
            follower.Initialize(this.conveyorPath, startDistance, "MAIN");
            follower.ComputeEntryPathDistances();

            // Initialize RowBall with balls and conveyor config
            rowBall.Initialize(config, this.ballPrefab, this.signalBus, this.config);

            this.activeRowBalls.Add(rowBall);

            // Start moving if conveyor is running
            if (this.isRunning)
            {
                follower.StartMoving();
            }
        }

        public float CalculatePathLength()
        {
            if (this.conveyorPath == null)
            {
                return 0;
            }

            var length = 0f;
            var count = this.conveyorPath.GetSampleCount();

            for (var i = 0; i < count - 1; i++)
            {
                length += Vector3.Distance(
                    this.conveyorPath.GetSample(i),
                    this.conveyorPath.GetSample(i + 1));
            }

            // Add loop closure
            length += Vector3.Distance(
                this.conveyorPath.GetSample(count - 1),
                this.conveyorPath.GetSample(0));

            return length;
        }

        private void ClearAllRowBalls()
        {
            foreach (var rowBall in this.activeRowBalls)
            {
                if (rowBall != null && rowBall.gameObject != null)
                {
                    Destroy(rowBall.gameObject);
                }
            }

            this.activeRowBalls.Clear();
            this.processingAtEntry.Clear();
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!this.isRunning)
            {
                return;
            }

            if (this.collectAreaBucketService == null)
            {
                Debug.LogWarning("[ConveyorController] collectAreaBucketService is null!");
                return;
            }

            this.CheckEntryPoints();
        }

        private void OnDestroy()
        {
            this.ClearAllRowBalls();
        }

        #endregion

        #region Entry Point Detection

        /// <summary>
        /// Check all row balls for entry point proximity.
        /// Matches Cocos MainConveyorController entry detection.
        /// </summary>
        private void CheckEntryPoints()
        {
            foreach (var rowBall in this.activeRowBalls)
            {
                if (rowBall == null)
                {
                    continue;
                }

                var follower = rowBall.GetComponent<PathFollower>();
                if (follower == null)
                {
                    continue;
                }

                follower.UpdateEntryPointDetection(entryIndex =>
                {
                    this.OnRowBallReachedEntry(rowBall, entryIndex).Forget();
                });
            }
        }

        /// <summary>
        /// Handle row ball reaching entry point.
        /// Matches Cocos MainConveyorController.onRowBallReachedEntry()
        /// </summary>
        private async UniTask OnRowBallReachedEntry(RowBall rowBall, int entryIndex)
        {
            if (rowBall == null || this.processingAtEntry.Contains(rowBall))
            {
                return;
            }

            var follower = rowBall.GetComponent<PathFollower>();
            if (follower == null)
            {
                return;
            }

            this.processingAtEntry.Add(rowBall);
            try
            {
                Debug.Log($"[ConveyorController] EntryStart row={rowBall.RowId} entry={entryIndex} balls={this.FormatBallList(rowBall.GetActiveBalls())} targets={this.FormatTargetBuckets()}");

                // Fire signal
                this.signalBus?.Fire(new RowBallReachEntrySignal
                {
                    RowId = rowBall.Config.RowId,
                    EntryIndex = entryIndex
                });

                await this.CollectMatchingBallsAtEntry(rowBall, entryIndex);

                // Remove row ball if all balls collected
                if (rowBall.GetBallCount() == 0)
                {
                    this.RemoveRowBall(rowBall);
                }
            }
            finally
            {
                Debug.Log($"[ConveyorController] EntryEnd row={rowBall.RowId} entry={entryIndex} remaining={this.FormatBallList(rowBall.GetActiveBalls())} targets={this.FormatTargetBuckets()}");
                this.processingAtEntry.Remove(rowBall);
            }
        }

        private async UniTask JumpBallWithDelay(Ball ball, Bucket bucket, float delay)
        {
            if (delay > 0)
            {
                await UniTask.Delay((int)(delay * 1000));
            }

            await ball.JumpToBucket(bucket, incomingAlreadyReserved: true);
        }

        private async UniTask CollectMatchingBallsAtEntry(RowBall rowBall, int entryIndex)
        {
            var waveIndex = 0;

            while (waveIndex < GameConstants.RowBallConfig.MaxBalls)
            {
                var rowColor = this.GetRowTargetColor(rowBall);
                if (!rowColor.HasValue)
                {
                    Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=no-row-color");
                    return;
                }

                var targetBucket = this.collectAreaBucketService.GetStableTargetBucketForColor(rowColor.Value);
                Debug.Log($"[ConveyorController] EntryWave row={rowBall.RowId} entry={entryIndex} wave={waveIndex} rowColor={rowColor.Value} activeTarget={this.collectAreaBucketService.GetActiveTargetDebug()} targets={this.FormatTargetBuckets()}");
                if (targetBucket == null)
                {
                    Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=no-target-bucket rowColor={rowColor.Value}");
                    return;
                }

                var ballsToCollect = rowBall.GetActiveBalls()
                    .Where(b => !b.IsCollected && b.BallColor == rowColor.Value)
                    .ToList();

                Debug.Log($"[ConveyorController] EntryCandidates row={rowBall.RowId} entry={entryIndex} wave={waveIndex} candidates={this.FormatBallList(ballsToCollect)}");
                if (ballsToCollect.Count == 0)
                {
                    Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=no-matching-balls");
                    return;
                }

                var limitedBallsToCollect = this.LimitBallsToAvailableSlots(targetBucket, ballsToCollect);
                Debug.Log($"[ConveyorController] EntryLimited row={rowBall.RowId} entry={entryIndex} wave={waveIndex} limited={this.FormatBallList(limitedBallsToCollect)}");
                if (limitedBallsToCollect.Count == 0)
                {
                    Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=no-available-slots activeTarget={this.collectAreaBucketService.GetActiveTargetDebug()}");
                    return;
                }

                var assignments = this.BuildAssignments(targetBucket, limitedBallsToCollect);
                if (assignments.Count == 0)
                {
                    Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=no-assignments activeTarget={this.collectAreaBucketService.GetActiveTargetDebug()}");
                    return;
                }

                Debug.Log($"[ConveyorController] EntryAssignments row={rowBall.RowId} entry={entryIndex} wave={waveIndex} assignments={this.FormatAssignments(assignments)}");

                var jumpTasks = new List<UniTask>();
                for (var i = 0; i < assignments.Count; i++)
                {
                    var (ball, bucket) = assignments[i];
                    var delay = i * GameConstants.RowBallConfig.BallJumpDelay;
                    jumpTasks.Add(this.JumpBallWithDelay(ball, bucket, delay));
                }

                await UniTask.WhenAll(jumpTasks);
                waveIndex++;
            }

            Debug.Log($"[ConveyorController] EntryStop row={rowBall.RowId} entry={entryIndex} wave={waveIndex} reason=max-wave-reached");
        }

        /// <summary>
        /// Limit balls to collect based on available slots per color.
        /// Returns a subset of balls that can actually be collected.
        /// </summary>
        private List<Ball> LimitBallsToAvailableSlots(Bucket targetBucket, List<Ball> ballsToCollect)
        {
            if (targetBucket == null)
            {
                return new List<Ball>();
            }

            var availableSlots = targetBucket.GetRemainingSlotCount();
            if (availableSlots <= 0)
            {
                return new List<Ball>();
            }

            return ballsToCollect.Take(availableSlots).ToList();
        }

        private List<(Ball ball, Bucket bucket)> BuildAssignments(Bucket targetBucket, List<Ball> ballsToCollect)
        {
            var assignments = new List<(Ball ball, Bucket bucket)>();
            if (targetBucket == null)
            {
                return assignments;
            }

            foreach (var ball in ballsToCollect)
            {
                targetBucket.StartIncomingBall();
                assignments.Add((ball, targetBucket));
            }

            return assignments;
        }

        private ColorType? GetRowTargetColor(RowBall rowBall)
        {
            var firstBall = rowBall.GetActiveBalls().FirstOrDefault(ball => !ball.IsCollected);
            return firstBall?.BallColor;
        }

        private string FormatBallList(IEnumerable<Ball> balls)
        {
            return string.Join(",", balls.Select(ball => $"{ball.BallColor}:{ball.BallIndex}"));
        }

        private string FormatAssignments(IEnumerable<(Ball ball, Bucket bucket)> assignments)
        {
            return string.Join(",", assignments.Select(pair => $"{pair.ball.BallColor}:{pair.ball.BallIndex}->b{pair.bucket.Data.IndexBucket}[c={pair.bucket.CollectedBallCount},in={pair.bucket.IncomingBallCount},target={pair.bucket.TargetBallCount}]"));
        }

        private string FormatTargetBuckets()
        {
            return string.Join(",", this.collectAreaBucketService.GetAvailableBucketsInCollectAreas()
                .Select(bucket => $"b{bucket.Data.IndexBucket}:{bucket.Data.Color}[c={bucket.CollectedBallCount},in={bucket.IncomingBallCount},target={bucket.TargetBallCount},rem={bucket.GetRemainingSlotCount()}]"));
        }

        #endregion
    }
}
