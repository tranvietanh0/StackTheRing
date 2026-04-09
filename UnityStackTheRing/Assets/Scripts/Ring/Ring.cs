namespace HyperCasualGame.Scripts.Ring
{
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    public class Ring : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        #endregion

        #region Properties

        public ColorType ColorType { get; private set; }
        public RingState State { get; private set; }
        public float PathProgress { get; set; }
        public int ConveyorLoopCount { get; set; }

        #endregion

        #region Public Methods

        public void Initialize(ColorType colorType)
        {
            this.ColorType = colorType;
            this.State = RingState.OnConveyor;
            this.PathProgress = 0f;
            this.ConveyorLoopCount = 0;

            this.ApplyColor(colorType);
        }

        public void SetState(RingState state)
        {
            this.State = state;

            if (this.trailRenderer != null)
            {
                this.trailRenderer.emitting = state == RingState.Attracted;
            }
        }

        public void OnSpawn()
        {
            this.gameObject.SetActive(true);
            this.State = RingState.OnConveyor;
            this.PathProgress = 0f;
            this.ConveyorLoopCount = 0;

            if (this.trailRenderer != null)
            {
                this.trailRenderer.Clear();
                this.trailRenderer.emitting = false;
            }
        }

        public void OnDespawn()
        {
            this.gameObject.SetActive(false);

            if (this.trailRenderer != null)
            {
                this.trailRenderer.Clear();
            }
        }

        public void IncrementLoopCount()
        {
            this.ConveyorLoopCount++;
        }

        #endregion

        #region Private Methods

        private void ApplyColor(ColorType colorType)
        {
            if (this.meshRenderer == null)
            {
                this.meshRenderer = this.GetComponent<MeshRenderer>();
            }

            if (this.meshRenderer == null)
            {
                Debug.LogWarning($"Ring: MeshRenderer not found on {this.name}");
                return;
            }

            var color = GameConstants.GetColor(colorType);

            // Create instance material to avoid shared material issues
            var mat = this.meshRenderer.material;
            mat.SetColor("_BaseColor", color);
            mat.color = color; // Fallback for standard shader

            if (this.trailRenderer != null)
            {
                this.trailRenderer.startColor = color;
                this.trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
            }
        }

        #endregion
    }
}
