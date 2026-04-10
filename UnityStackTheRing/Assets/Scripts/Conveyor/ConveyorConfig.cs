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
        [Tooltip("World-space distance between row centers (like Cocos BALL_SPACING)")]
        [Range(0.05f, 0.5f)]
        public float RowSpacing = 0.115f;

        [Header("Attraction Zone")]
        [Tooltip("How close to slot position ring must be to get attracted (0-1 path progress)")]
        [Range(0.01f, 0.1f)]
        public float AttractionZoneSize = 0.05f;

        [Header("Visual")]
        [Tooltip("Height offset for rings on conveyor")]
        public float RingHeightOffset = 0.1f;

        [Header("Multi-Lane")]
        [Tooltip("Number of parallel lanes for rings (1 = single line, 5 = 5 rows)")]
        [Range(1, 7)]
        public int LaneCount = 5;

        [Tooltip("Spacing between lanes (perpendicular to path)")]
        [Range(0.05f, 0.3f)]
        public float LaneSpacing = 0.12f;

        [Tooltip("Ring scale for dense packing")]
        [Range(0.05f, 0.3f)]
        public float RingScale = 0.1f;
    }
}
