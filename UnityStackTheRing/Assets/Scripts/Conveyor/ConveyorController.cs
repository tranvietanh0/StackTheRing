namespace HyperCasualGame.Scripts.Conveyor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dreamteck.Splines;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
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
        private ConveyorPath conveyorPath;
        private float baseSpeed = 1f;
        private bool isRunning;
        private SignalBus signalBus;
        private ILogger logger;

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

                // Create RowBall config - all 5 balls same color
                var colors = new ColorType[GameConstants.RowBallConfig.MaxBalls];
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

            this.logger.Info($"Spawned {rowCount} rows ({rowCount * GameConstants.RowBallConfig.MaxBalls} balls). Path length: {pathLength:F2}, Max capacity: {maxRows}");
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

            if (this.entryNodes.Count > 0)
            {
                follower.SetEntryNodes(this.entryNodes);
            }

            // Initialize path following
            follower.Initialize(this.conveyorPath, startIndex, "MAIN");

            // Initialize RowBall with balls
            rowBall.Initialize(config, this.ballPrefab, this.signalBus);

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

        private void OnDestroy()
        {
            this.ClearAllRowBalls();
        }

        #endregion
    }
}
