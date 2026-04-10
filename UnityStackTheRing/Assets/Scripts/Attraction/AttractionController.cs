namespace HyperCasualGame.Scripts.Attraction
{
    using System.Collections.Generic;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.Slot;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class AttractionController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private AttractionConfig config;

        #endregion

        #region Private Fields

        private ConveyorController conveyor;
        private SlotManager slotManager;
        private SignalBus signalBus;
        private ILogger logger;

        private readonly HashSet<Ball> ballsBeingAttracted = new();

        #endregion

        #region Properties

        public bool IsEnabled { get; private set; } = true;

        #endregion

        #region Public Methods

        public void Initialize(
            ConveyorController conveyor,
            SlotManager slotManager,
            SignalBus signalBus,
            ILoggerManager loggerManager)
        {
            this.conveyor = conveyor;
            this.slotManager = slotManager;
            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);
        }

        public void SetEnabled(bool enabled)
        {
            this.IsEnabled = enabled;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!this.IsEnabled) return;
            if (this.conveyor == null || this.slotManager == null) return;

            this.CheckAttraction();
        }

        #endregion

        #region Private Methods

        private void CheckAttraction()
        {
            foreach (var rowBall in this.conveyor.ActiveRowBalls)
            {
                var follower = rowBall.GetComponent<PathFollower>();
                if (follower == null) continue;

                foreach (var ball in rowBall.GetActiveBalls())
                {
                    if (this.ballsBeingAttracted.Contains(ball)) continue;

                    var slot = this.FindMatchingSlot(ball);
                    if (slot == null) continue;

                    if (this.IsInAttractionZone(follower, slot))
                    {
                        this.AttractBall(ball, rowBall, slot);
                    }
                }
            }
        }

        private Slot FindMatchingSlot(Ball ball)
        {
            return this.slotManager.GetSlotForColor(ball.BallColor);
        }

        private bool IsInAttractionZone(PathFollower follower, Slot slot)
        {
            var totalLength = follower.GetTotalPathLength();
            if (totalLength <= 0) return false;

            var currentProgress = follower.GetCurrentDistance() / totalLength;
            var slotProgress = slot.AttractionProgress;

            var diff = Mathf.Abs(currentProgress - slotProgress);

            if (diff > 0.5f)
            {
                diff = 1f - diff;
            }

            return diff <= this.config.AttractionZone;
        }

        private void AttractBall(Ball ball, RowBall rowBall, Slot slot)
        {
            this.ballsBeingAttracted.Add(ball);

            // Remove ball from row
            rowBall.RemoveBallAt(ball.BallIndex);

            this.signalBus.Fire(new BallAttractedSignal
            {
                Ball = ball,
                SlotIndex = slot.SlotIndex
            });

            // Detach from parent
            ball.transform.SetParent(null);

            var startPos = ball.transform.position;
            var endPos = slot.GetNextStackWorldPosition();
            var path = this.CalculateCurvedPath(startPos, endPos);

            ball.transform.DOPath(path, this.config.AttractionDuration, PathType.CatmullRom)
                .SetEase(this.config.MoveEase)
                .OnComplete(() => this.OnAttractionComplete(ball, slot));
        }

        private Vector3[] CalculateCurvedPath(Vector3 start, Vector3 end)
        {
            var mid = (start + end) / 2f;
            mid.y += this.config.CurveHeight;

            return new[] { start, mid, end };
        }

        private void OnAttractionComplete(Ball ball, Slot slot)
        {
            this.ballsBeingAttracted.Remove(ball);

            if (this.config.ArrivalPunch > 0)
            {
                ball.transform.DOPunchScale(Vector3.one * this.config.ArrivalPunch, 0.15f, 5, 0.5f);
            }

            slot.AddBall(ball);

            this.logger.Info($"Ball attracted to slot {slot.SlotIndex}. Stack: {slot.CurrentStackCount}");
        }

        #endregion
    }
}
