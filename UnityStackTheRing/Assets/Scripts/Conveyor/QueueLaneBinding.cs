namespace HyperCasualGame.Scripts.Conveyor
{
    using System;
    using UnityEngine;

    [Serializable]
    public class QueueLaneBinding
    {
        public string LaneId = "queue-0";
        public QueueConveyor QueueConveyor;
        public ConveyorFeeder ConveyorFeeder;
        public Transform InsertAnchor;
    }
}
