namespace HyperCasualGame.Scripts.Conveyor
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using Dreamteck.Splines;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class ConveyorController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private ConveyorConfig config;
        [SerializeField] private Transform ringContainer;
        [SerializeField] private SplineComputer spline;

        #endregion

        #region Private Fields

        private readonly List<Ring> activeRings = new();
        private float currentSpeed = 1f;
        private bool isRunning;
        private SignalBus signalBus;
        private ILogger logger;

        #endregion

        #region Properties

        public IReadOnlyList<Ring> ActiveRings => this.activeRings;
        public int ActiveRingCount => this.activeRings.Count;
        public bool IsRunning => this.isRunning;

        #endregion

        #region Events

        public event Action<Ring> OnRingCompletedLoop;
        public event Action OnAllRingsCleared;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus, ILoggerManager loggerManager)
        {
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            if (this.spline == null)
            {
                this.logger.Error("SplineComputer not assigned!");
            }
        }

        public void SetupLevel(LevelData levelData, Ring ringPrefab, Transform poolParent)
        {
            this.ClearAllRings();
            this.currentSpeed = levelData.ConveyorSpeed;

            this.SpawnRings(levelData, ringPrefab, poolParent);
        }

        public void StartConveyor()
        {
            this.isRunning = true;
            this.logger.Info("Conveyor started");
        }

        public void StopConveyor()
        {
            this.isRunning = false;
            this.logger.Info("Conveyor stopped");
        }

        public void PauseConveyor()
        {
            this.isRunning = false;
        }

        public void ResumeConveyor()
        {
            this.isRunning = true;
        }

        public Ring GetRingAtProgress(float progress, float tolerance = 0.05f)
        {
            foreach (var ring in this.activeRings)
            {
                if (ring.State != RingState.OnConveyor) continue;

                var diff = Mathf.Abs(ring.PathProgress - progress);
                if (diff < tolerance || diff > 1f - tolerance)
                {
                    return ring;
                }
            }
            return null;
        }

        public List<Ring> GetRingsInRange(float startProgress, float endProgress)
        {
            var result = new List<Ring>();
            foreach (var ring in this.activeRings)
            {
                if (ring.State != RingState.OnConveyor) continue;

                if (ring.PathProgress >= startProgress && ring.PathProgress <= endProgress)
                {
                    result.Add(ring);
                }
            }
            return result;
        }

        public void RemoveRing(Ring ring)
        {
            if (this.activeRings.Remove(ring))
            {
                this.logger.Info($"Ring removed. Remaining: {this.activeRings.Count}");

                if (this.activeRings.Count == 0)
                {
                    this.OnAllRingsCleared?.Invoke();
                    this.signalBus.Fire(new AllRingsClearedSignal());
                }
            }
        }

        public Vector3 GetPositionAtProgress(float progress)
        {
            if (this.spline == null)
            {
                return Vector3.zero;
            }

            // Dreamteck Spline uses 0-1 range, wraps automatically for closed splines
            progress = Mathf.Repeat(progress, 1f);
            return this.spline.EvaluatePosition(progress);
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!this.isRunning) return;

            this.UpdateRingPositions();
        }

        #endregion

        #region Private Methods

        private void SpawnRings(LevelData levelData, Ring ringPrefab, Transform poolParent)
        {
            var totalRings = levelData.TotalRingCount;
            var spacing = 1f / totalRings;

            var ringIndex = 0;
            foreach (var ringSpawn in levelData.Rings)
            {
                for (var i = 0; i < ringSpawn.Count; i++)
                {
                    var ring = Instantiate(ringPrefab, this.ringContainer);
                    ring.Initialize(ringSpawn.Color);
                    ring.PathProgress = ringIndex * spacing;
                    ring.transform.position = this.GetPositionAtProgress(ring.PathProgress);

                    this.activeRings.Add(ring);
                    ringIndex++;
                }
            }

            this.ShuffleRings();
            this.logger.Info($"Spawned {totalRings} rings");
        }

        private void ShuffleRings()
        {
            var n = this.activeRings.Count;
            for (var i = n - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);

                var tempProgress = this.activeRings[i].PathProgress;
                this.activeRings[i].PathProgress = this.activeRings[j].PathProgress;
                this.activeRings[j].PathProgress = tempProgress;
            }

            foreach (var ring in this.activeRings)
            {
                ring.transform.position = this.GetPositionAtProgress(ring.PathProgress);
            }
        }

        private void UpdateRingPositions()
        {
            var deltaProgress = (this.currentSpeed / this.config.LoopDuration) * Time.deltaTime;

            foreach (var ring in this.activeRings)
            {
                if (ring.State != RingState.OnConveyor) continue;

                var previousProgress = ring.PathProgress;
                ring.PathProgress += deltaProgress;

                if (ring.PathProgress >= 1f)
                {
                    ring.PathProgress -= 1f;
                    ring.IncrementLoopCount();

                    this.OnRingCompletedLoop?.Invoke(ring);
                    this.signalBus.Fire(new RingCompletedLoopSignal
                    {
                        Ring = ring,
                        LoopCount = ring.ConveyorLoopCount
                    });
                }

                ring.transform.position = this.GetPositionAtProgress(ring.PathProgress);
            }
        }

        private void ClearAllRings()
        {
            foreach (var ring in this.activeRings)
            {
                if (ring != null)
                {
                    Destroy(ring.gameObject);
                }
            }
            this.activeRings.Clear();
        }

        #endregion
    }
}
