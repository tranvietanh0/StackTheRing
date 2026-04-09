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
    }

    [Serializable]
    public class RingSpawn
    {
        public ColorType Color;
        [Range(1, 20)]
        public int Count;
    }
}
