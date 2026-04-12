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
            Debug.Log($"[ConveyorController] CheckEntryPoints called. activeRowBalls={this.activeRowBalls.Count}");
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

            this.processingAtEntry.Add(rowBall);

            try
            {
                Debug.Log($"[ConveyorController] RowBall {rowBall.RowId} reached entry {entryIndex}");

                // Fire signal
                this.signalBus?.Fire(new RowBallReachEntrySignal
                {
                    RowId = rowBall.Config.RowId,
                    EntryIndex = entryIndex
                });

                // Get target colors from buckets in CollectAreas
                var targetColors = this.collectAreaBucketService.GetTargetColorsFromBuckets();
                Debug.Log($"[ConveyorController] Target colors: {string.Join(", ", targetColors)} (count: {targetColors.Count})");
                if (targetColors.Count == 0)
                {
                    return;
                }

                // Get balls matching target colors
                var activeBalls = rowBall.GetActiveBalls();
                var ballsToCollect = activeBalls
                    .Where(b => !b.IsCollected && targetColors.Contains(b.BallColor))
                    .ToList();

                Debug.Log($"[ConveyorController] Balls to collect: {ballsToCollect.Count}");
                if (ballsToCollect.Count == 0)
                {
                    return;
                }

                // Count balls by color
                var colorCount = new Dictionary<ColorType, int>();
                foreach (var ball in ballsToCollect)
                {
                    if (colorCount.ContainsKey(ball.BallColor))
                    {
                        colorCount[ball.BallColor]++;
                    }
                    else
                    {
                        colorCount[ball.BallColor] = 1;
                    }
                }

                // Limit balls to available slots per color (collect as many as possible)
                var limitedBallsToCollect = this.LimitBallsToAvailableSlots(ballsToCollect);
                Debug.Log($"[ConveyorController] After slot limit: {limitedBallsToCollect.Count} balls");
                if (limitedBallsToCollect.Count == 0)
                {
                    return;
                }

                // Build balanced bucket assignment
                var bucketPlan = this.BuildBucketPlan(limitedBallsToCollect);

                // Reserve slots and build assignment pairs
                var assignments = new List<(Ball ball, Bucket bucket)>();
                foreach (var ball in limitedBallsToCollect)
                {
                    if (!bucketPlan.TryGetValue(ball.BallColor, out var buckets) || buckets.Count == 0)
                    {
                        continue;
                    }

                    var bucket = buckets[0];
                    buckets.RemoveAt(0);

                    bucket.StartIncomingBall();
                    assignments.Add((ball, bucket));
                }

                // Jump balls with staggered delay
                var jumpTasks = new List<UniTask>();
                for (var i = 0; i < assignments.Count; i++)
                {
                    var (ball, bucket) = assignments[i];
                    var delay = i * GameConstants.RowBallConfig.BallJumpDelay;

                    jumpTasks.Add(this.JumpBallWithDelay(ball, bucket, delay));
                }

                await UniTask.WhenAll(jumpTasks);

                // Remove row ball if all balls collected
                if (rowBall.GetBallCount() == 0)
                {
                    this.RemoveRowBall(rowBall);
                }
            }
            finally
            {
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

        /// <summary>
        /// Limit balls to collect based on available slots per color.
        /// Returns a subset of balls that can actually be collected.
        /// </summary>
        private List<Ball> LimitBallsToAvailableSlots(List<Ball> ballsToCollect)
        {
            var result = new List<Ball>();
            var slotsUsedByColor = new Dictionary<ColorType, int>();

            foreach (var ball in ballsToCollect)
            {
                var color = ball.BallColor;
                slotsUsedByColor.TryGetValue(color, out var usedSlots);

                var availableSlots = this.collectAreaBucketService.GetAvailableSlotCountByColor(color);
                if (usedSlots < availableSlots)
                {
                    result.Add(ball);
                    slotsUsedByColor[color] = usedSlots + 1;
                }
            }

            return result;
        }

        private Dictionary<ColorType, List<Bucket>> BuildBucketPlan(List<Ball> ballsToCollect)
        {
            var plan = new Dictionary<ColorType, List<Bucket>>();

            // Group balls by color
            var ballsByColor = new Dictionary<ColorType, int>();
            foreach (var ball in ballsToCollect)
            {
                if (ballsByColor.ContainsKey(ball.BallColor))
                {
                    ballsByColor[ball.BallColor]++;
                }
                else
                {
                    ballsByColor[ball.BallColor] = 1;
                }
            }

            // Build balanced plan for each color
            foreach (var (color, count) in ballsByColor)
            {
                var buckets = this.collectAreaBucketService.BuildBalancedBucketPlanByColor(color, count);
                plan[color] = buckets;
            }

            return plan;
        }

        #endregion
    }
}
