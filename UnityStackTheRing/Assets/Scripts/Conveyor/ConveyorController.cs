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

        [Header("Entry Points")]
        [SerializeField] private List<Transform> entryNodes = new();

        #endregion

        #region Private Fields

        private readonly List<RowBall> activeRowBalls = new();
        private readonly HashSet<RowBall> processingAtEntry = new();
        private ConveyorPath conveyorPath;
        private float baseSpeed = 1f;
        private bool isRunning;
        private SignalBus signalBus;
        private ILogger logger;
        private CollectAreaBucketService collectAreaBucketService;

        #endregion

        #region Properties

        public IReadOnlyList<RowBall> ActiveRowBalls => this.activeRowBalls;

        public int ActiveRowCount => this.activeRowBalls.Count;

        public int TotalBallCount => this.activeRowBalls.Sum(r => r.GetBallCount());

        public bool IsRunning => this.isRunning;

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
            Destroy(rowBall.gameObject);

            this.logger.Info($"RowBall removed. Remaining: {this.ActiveRowCount}");

            if (this.TotalBallCount == 0)
            {
                this.OnAllBallsCleared?.Invoke();
                this.signalBus.Fire(new AllRingsClearedSignal());
            }
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

                // Calculate start index on path
                var startIndex = Mathf.RoundToInt((startDistance / pathLength) * (this.conveyorPath.GetSampleCount() - 1));

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

                this.SpawnRowBall(rowConfig, startIndex);
            }

            var ballsPerRowForLog = this.config != null ? this.config.BallsPerRow : GameConstants.RowBallConfig.MaxBalls;
            this.logger.Info($"Spawned {rowCount} rows ({rowCount * ballsPerRowForLog} balls). Path length: {pathLength:F2}, Max capacity: {maxRows}");
        }

        private void SpawnRowBall(RowBallConfig config, int startIndex)
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
            follower.Initialize(this.conveyorPath, startIndex, "MAIN");
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

        private float CalculatePathLength()
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
            follower.IsWaitingAtEntry = true;

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
                follower.IsWaitingAtEntry = false;
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
