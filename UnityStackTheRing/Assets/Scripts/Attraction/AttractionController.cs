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

        private readonly List<Ring> ringsBeingAttracted = new();

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
            foreach (var ring in this.conveyor.ActiveRings)
            {
                if (ring.State != RingState.OnConveyor) continue;
                if (this.ringsBeingAttracted.Contains(ring)) continue;

                var slot = this.FindMatchingSlot(ring);
                if (slot == null) continue;

                if (this.IsInAttractionZone(ring, slot))
                {
                    this.AttractRing(ring, slot);
                }
            }
        }

        private Slot FindMatchingSlot(Ring ring)
        {
            return this.slotManager.GetSlotForColor(ring.ColorType);
        }

        private bool IsInAttractionZone(Ring ring, Slot slot)
        {
            var ringProgress = ring.PathProgress;
            var slotProgress = slot.AttractionProgress;

            var diff = Mathf.Abs(ringProgress - slotProgress);

            if (diff > 0.5f)
            {
                diff = 1f - diff;
            }

            return diff <= this.config.AttractionZone;
        }

        private void AttractRing(Ring ring, Slot slot)
        {
            ring.SetState(RingState.Attracted);
            this.ringsBeingAttracted.Add(ring);
            this.conveyor.RemoveRing(ring);

            this.signalBus.Fire(new RingAttractedSignal
            {
                Ring = ring,
                SlotIndex = slot.SlotIndex
            });

            var startPos = ring.transform.position;
            var endPos = slot.GetNextStackWorldPosition();
            var path = this.CalculateCurvedPath(startPos, endPos);

            ring.transform.DOPath(path, this.config.AttractionDuration, PathType.CatmullRom)
                .SetEase(this.config.MoveEase)
                .OnComplete(() => this.OnAttractionComplete(ring, slot));
        }

        private Vector3[] CalculateCurvedPath(Vector3 start, Vector3 end)
        {
            var mid = (start + end) / 2f;
            mid.y += this.config.CurveHeight;

            return new[] { start, mid, end };
        }

        private void OnAttractionComplete(Ring ring, Slot slot)
        {
            this.ringsBeingAttracted.Remove(ring);

            if (this.config.ArrivalPunch > 0)
            {
                ring.transform.DOPunchScale(Vector3.one * this.config.ArrivalPunch, 0.15f, 5, 0.5f);
            }

            slot.AddRing(ring);

            this.logger.Info($"Ring attracted to slot {slot.SlotIndex}. Stack: {slot.CurrentStackCount}");
        }

        #endregion
    }
}