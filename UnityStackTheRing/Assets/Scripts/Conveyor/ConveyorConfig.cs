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

        [Header("Row Spacing")]
        [Tooltip("World-space distance between row centers")]
        [Range(0.02f, 0.5f)]
        public float RowSpacing = 0.06f;

        [Header("Ball Configuration")]
        [Tooltip("Number of balls per row (lane count)")]
        [Range(1, 7)]
        public int BallsPerRow = 5;

        [Tooltip("Spacing between balls within a row (perpendicular to path)")]
        [Range(0.02f, 0.3f)]
        public float BallLaneSpacing = 0.08f;

        [Tooltip("Scale of each ball")]
        [Range(0.05f, 0.5f)]
        public float BallScale = 0.15f;

        [Header("Visual")]
        [Tooltip("Height offset for rings on conveyor")]
        public float RingHeightOffset = 0.1f;

        /// <summary>
        /// Calculate Z positions for balls in a row, centered around 0.
        /// </summary>
        public float[] GetBallZPositions()
        {
            var positions = new float[this.BallsPerRow];
            var totalWidth = (this.BallsPerRow - 1) * this.BallLaneSpacing;
            var startZ = totalWidth / 2f;

            for (var i = 0; i < this.BallsPerRow; i++)
            {
                positions[i] = startZ - i * this.BallLaneSpacing;
            }

            return positions;
        }
    }
}
