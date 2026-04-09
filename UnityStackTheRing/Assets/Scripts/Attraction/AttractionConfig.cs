namespace HyperCasualGame.Scripts.Attraction
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AttractionConfig", menuName = "StackTheRing/AttractionConfig")]
    public class AttractionConfig : ScriptableObject
    {
        [Header("Timing")]
        [Tooltip("Duration of attraction animation in seconds")]
        [Range(0.2f, 1f)]
        public float AttractionDuration = 0.4f;

        [Header("Detection")]
        [Tooltip("How close to slot's attraction point ring must be (0-1 path progress)")]
        [Range(0.01f, 0.15f)]
        public float AttractionZone = 0.08f;

        [Header("Curve Settings")]
        [Tooltip("Height of the curve arc")]
        [Range(0.5f, 3f)]
        public float CurveHeight = 1.5f;

        [Tooltip("Animation ease type")]
        public DG.Tweening.Ease MoveEase = DG.Tweening.Ease.InOutQuad;

        [Header("Visual")]
        [Tooltip("Enable trail effect during attraction")]
        public bool EnableTrail = true;

        [Tooltip("Scale punch on arrival")]
        [Range(0f, 0.5f)]
        public float ArrivalPunch = 0.15f;
    }
}
