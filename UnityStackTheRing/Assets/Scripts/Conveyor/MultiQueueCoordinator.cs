namespace HyperCasualGame.Scripts.Conveyor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using UniT.Logging;
    using UnityEngine;
    using ILogger = UniT.Logging.ILogger;

    public class MultiQueueCoordinator
    {
        private readonly List<QueueConveyor> queueConveyors = new();
        private readonly List<ConveyorFeeder> conveyorFeeders = new();
        private readonly List<QueueLaneBinding> orderedBindings = new();
        private readonly Dictionary<string, QueueLaneBinding> bindingByLaneId = new();
        private readonly ILogger logger;
        private ConveyorController conveyorController;
        private ILoggerManager loggerManager;

        public MultiQueueCoordinator(ILoggerManager loggerManager)
        {
            this.loggerManager = loggerManager;
            this.logger = loggerManager.GetLogger(this);
        }

        public IReadOnlyList<QueueConveyor> QueueConveyors => this.queueConveyors;
        public bool HasPendingRows => this.queueConveyors.Any(queue => queue != null && !queue.IsEmpty);
        public IEnumerable<HyperCasualGame.Scripts.Ring.RowBall> PendingRows => this.queueConveyors.Where(queue => queue != null).SelectMany(queue => queue.PendingRowBalls);
        public IEnumerable<HyperCasualGame.Scripts.Ring.RowBall> ReadyRows => this.queueConveyors.Where(queue => queue != null).SelectMany(queue => queue.ReadyRows);

        public void Initialize(
            ConveyorController conveyorController,
            SignalBus signalBus,
            QueueLaneBinding[] laneBindings)
        {
            this.conveyorController = conveyorController;
            this.queueConveyors.Clear();
            this.conveyorFeeders.Clear();
            this.orderedBindings.Clear();
            this.bindingByLaneId.Clear();

            foreach (var binding in laneBindings ?? Array.Empty<QueueLaneBinding>())
            {
                if (binding == null || binding.QueueConveyor == null || binding.ConveyorFeeder == null)
                {
                    throw new MissingReferenceException("QueueLaneBinding requires QueueConveyor and ConveyorFeeder.");
                }

                if (string.IsNullOrWhiteSpace(binding.LaneId))
                {
                    throw new MissingReferenceException("QueueLaneBinding requires LaneId.");
                }

                if (!this.bindingByLaneId.TryAdd(binding.LaneId, binding))
                {
                    throw new InvalidOperationException($"Duplicate queue lane id: {binding.LaneId}");
                }

                this.orderedBindings.Add(binding);

                binding.QueueConveyor.Initialize(signalBus, this.loggerManager);
                binding.ConveyorFeeder.Initialize(conveyorController, binding.QueueConveyor, binding.InsertAnchor, this.SyncMainQueueState, this.loggerManager);
                this.queueConveyors.Add(binding.QueueConveyor);
                this.conveyorFeeders.Add(binding.ConveyorFeeder);
            }
        }

        public void SetupLevel(LevelData levelData, Ball ballPrefab, RowBall rowBallPrefab)
        {
            var activeLanes = levelData.GetActiveQueueLanes();
            for (var laneIndex = 0; laneIndex < activeLanes.Length; laneIndex++)
            {
                var lane = activeLanes[laneIndex];
                if (!lane.Enabled)
                {
                    continue;
                }

                if (!this.bindingByLaneId.TryGetValue(lane.LaneId, out var binding))
                {
                    throw new MissingReferenceException($"No QueueLaneBinding found for lane id '{lane.LaneId}'.");
                }

                binding.QueueConveyor.SetupLevel(lane, ballPrefab, rowBallPrefab, levelData.ConveyorSpeed);
            }

            this.SyncMainQueueState();
            this.logger.Info($"MultiQueueCoordinator setup complete. queues={this.queueConveyors.Count}");
        }

        public void Start()
        {
            foreach (var queue in this.queueConveyors)
            {
                queue?.StartQueue();
            }

            foreach (var feeder in this.conveyorFeeders)
            {
                feeder?.StartFeeding();
            }

            this.SyncMainQueueState();
        }

        public void Stop()
        {
            foreach (var feeder in this.conveyorFeeders)
            {
                feeder?.StopFeeding();
            }

            foreach (var queue in this.queueConveyors)
            {
                queue?.StopQueue();
            }

            this.SyncMainQueueState();
        }

        public void SyncMainQueueState()
        {
            this.conveyorController?.SetHasQueueRows(this.HasPendingRows);
        }
    }
}
