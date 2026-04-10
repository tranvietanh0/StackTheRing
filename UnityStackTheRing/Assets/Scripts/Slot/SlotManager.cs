namespace HyperCasualGame.Scripts.Slot
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Signals;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class SlotManager : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Slot[] slots;

        #endregion

        #region Private Fields

        private SignalBus signalBus;
        private ILogger logger;
        private int stackLimit = 8;
        private bool isInitialized;

        #endregion

        #region Properties

        public IReadOnlyList<Slot> Slots => this.slots;

        #endregion

        #region Events

        public event Action<Slot, ColorType> OnCollectorPlaced;
        public event Action<Slot> OnSlotCleared;
        public event Action<Slot, Ball> OnBallStackedInSlot;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus, ILoggerManager loggerManager)
        {
            if (this.isInitialized) return;
            this.isInitialized = true;

            this.signalBus = signalBus;
            this.logger = loggerManager.GetLogger(this);

            this.signalBus.Subscribe<CollectorTappedSignal>(this.OnCollectorTapped);
        }

        public void SetupLevel(LevelData levelData)
        {
            this.stackLimit = levelData.StackLimit;

            for (var i = 0; i < this.slots.Length; i++)
            {
                var attractionProgress = (float)i / this.slots.Length;
                this.slots[i].Initialize(this.stackLimit, attractionProgress);
                this.slots[i].OnStackFull += this.HandleStackFull;
                this.slots[i].OnBallAdded += this.HandleBallAdded;
            }

            this.logger.Info($"SlotManager setup with stackLimit: {this.stackLimit}");
        }

        public bool TryPlaceCollector(ColorType color)
        {
            var emptySlot = this.GetFirstEmptySlot();
            if (emptySlot == null)
            {
                this.logger.Warning("No empty slot available");
                return false;
            }

            emptySlot.AssignColor(color);
            this.OnCollectorPlaced?.Invoke(emptySlot, color);

            this.signalBus.Fire(new CollectorPlacedSignal
            {
                SlotIndex = emptySlot.SlotIndex,
                Color = color
            });

            this.logger.Info($"Collector {color} placed in slot {emptySlot.SlotIndex}");
            return true;
        }

        public Slot GetSlotForColor(ColorType color)
        {
            foreach (var slot in this.slots)
            {
                if (slot.CurrentColor == color && !slot.IsFull)
                {
                    return slot;
                }
            }
            return null;
        }

        public Slot GetFirstEmptySlot()
        {
            foreach (var slot in this.slots)
            {
                if (slot.IsEmpty)
                {
                    return slot;
                }
            }
            return null;
        }

        public bool HasEmptySlot()
        {
            return this.GetFirstEmptySlot() != null;
        }

        public bool AllSlotsOccupied()
        {
            foreach (var slot in this.slots)
            {
                if (slot.IsEmpty)
                {
                    return false;
                }
            }
            return true;
        }

        public bool AllSlotsFull()
        {
            foreach (var slot in this.slots)
            {
                if (!slot.IsFull)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanCollectColor(ColorType color)
        {
            var slot = this.GetSlotForColor(color);
            return slot != null && !slot.IsFull;
        }

        public void Cleanup()
        {
            this.signalBus.Unsubscribe<CollectorTappedSignal>(this.OnCollectorTapped);

            foreach (var slot in this.slots)
            {
                slot.OnStackFull -= this.HandleStackFull;
                slot.OnBallAdded -= this.HandleBallAdded;
                slot.ClearSlot();
            }
        }

        #endregion

        #region Private Methods

        private void OnCollectorTapped(CollectorTappedSignal signal)
        {
            this.TryPlaceCollector(signal.Color);
        }

        private void HandleStackFull(Slot slot)
        {
            this.logger.Info($"Stack full in slot {slot.SlotIndex}");

            var ballsCleared = slot.CurrentStackCount;
            var color = slot.CurrentColor ?? ColorType.Red;

            slot.ClearStack(ball =>
            {
                ball.transform.localScale = Vector3.one * 1.2f;
                DOTween.Sequence()
                    .Append(ball.transform.DOScale(0f, 0.2f))
                    .OnComplete(() =>
                    {
                        if (ball != null)
                        {
                            Destroy(ball.gameObject);
                        }
                    });
            });

            this.OnSlotCleared?.Invoke(slot);

            this.signalBus.Fire(new StackClearedSignal
            {
                SlotIndex = slot.SlotIndex,
                Color = color,
                BallsCleared = ballsCleared
            });
        }

        private void HandleBallAdded(Slot slot, Ball ball)
        {
            this.OnBallStackedInSlot?.Invoke(slot, ball);

            this.signalBus.Fire(new BallStackedSignal
            {
                Ball = ball,
                SlotIndex = slot.SlotIndex,
                CurrentStackCount = slot.CurrentStackCount
            });
        }

        #endregion
    }
}
