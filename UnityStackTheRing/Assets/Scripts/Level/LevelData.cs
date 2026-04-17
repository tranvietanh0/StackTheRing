namespace HyperCasualGame.Scripts.Level
{
    using System;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Level_00", menuName = "StackTheRing/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Basic Info")]
        public int LevelNumber;

        [Header("Conveyor Settings")]
        [Range(0.5f, 3f)]
        public float ConveyorSpeed = 1f;

        [Header("Stack Settings")]
        [Range(4, 12)]
        public int StackLimit = 8;

        [Header("Ring Configuration")]
        public RingSpawn[] Rings;

        [Header("Available Collectors")]
        public ColorType[] AvailableCollectors;

        [Header("Bucket Grid Configuration")]
        public BucketColumn[] BucketColumns;
        public float BucketColumnSpacing = 1.2f;
        public float BucketRowSpacing = 1.2f;

        [Header("Queue Conveyor")]
        [Tooltip("Enable queue conveyor that feeds rows into the ring when gaps appear")]
        public bool HasQueue;

        [Tooltip("Rings waiting in the queue (fed into ring when space opens)")]
        public RingSpawn[] QueueRings;

        [Tooltip("Speed multiplier for queue conveyor movement")]
        [Range(0.5f, 3f)]
        public float QueueSpeed = 1f;

        [Header("Variations")]
        public bool HasHiddenRings;
        public int BlockedSlotCount;

        public int TotalRingCount
        {
            get
            {
                var count = 0;
                if (this.Rings == null) return count;
                foreach (var ring in this.Rings)
                {
                    count += ring.Count;
                }
                return count;
            }
        }

        public int TotalQueueRingCount
        {
            get
            {
                var count = 0;
                if (this.QueueRings == null) return count;
                foreach (var ring in this.QueueRings)
                {
                    count += ring.Count;
                }
                return count;
            }
        }

        /// <summary>
        /// Total rings across both main conveyor and queue.
        /// </summary>
        public int TotalAllRingCount => this.TotalRingCount + this.TotalQueueRingCount;
    }

    [Serializable]
    public class RingSpawn
    {
        public ColorType Color;
        [Range(1, 100)]
        public int Count;
    }

    /// <summary>
    /// Bucket column configuration (matches Cocos GridColumn)
    /// </summary>
    [Serializable]
    public class BucketColumn
    {
        public ColorType[] BucketColors;
    }
}