namespace HyperCasualGame.Scripts.Conveyor
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "ConveyorConfig", menuName = "StackTheRing/ConveyorConfig")]
    public class ConveyorConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Base speed multiplier for conveyor movement")]
        [Range(0.5f, 3f)]
        public float BaseSpeed = 1f;

        [Tooltip("Time in seconds for one complete loop at base speed")]
        [Range(3f, 15f)]
        public float LoopDuration = 8f;

        [Header("Ring Spacing")]
        [Tooltip("Minimum spacing between rings as percentage of path (0-1)")]
        [Range(0.02f, 0.1f)]
        public float MinRingSpacing = 0.05f;

        [Header("Attraction Zone")]
        [Tooltip("How close to slot position ring must be to get attracted (0-1 path progress)")]
        [Range(0.01f, 0.1f)]
        public float AttractionZoneSize = 0.05f;

        [Header("Visual")]
        [Tooltip("Height offset for rings on conveyor")]
        public float RingHeightOffset = 0.1f;
    }
}
