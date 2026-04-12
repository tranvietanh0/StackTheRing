namespace HyperCasualGame.Scripts.Bucket
{
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Signals;
    using UnityEngine;

    /// <summary>
    /// Handles bucket tap input via Physics.Raycast.
    /// More reliable than OnMouseDown for top-down camera.
    /// </summary>
    public class BucketInputController : MonoBehaviour
    {
        #region Private Fields

        private SignalBus signalBus;
        private Camera mainCamera;
        private bool isInitialized;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus)
        {
            this.signalBus = signalBus;
            this.mainCamera = Camera.main;
            this.isInitialized = true;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!this.isInitialized) return;

            // Handle mouse click (works for both Editor and mobile touch)
            if (Input.GetMouseButtonDown(0))
            {
                this.HandleTap(Input.mousePosition);
            }
        }

        #endregion

        #region Private Methods

        private void HandleTap(Vector3 screenPosition)
        {
            if (this.mainCamera == null)
            {
                this.mainCamera = Camera.main;
                if (this.mainCamera == null) return;
            }

            var ray = this.mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var bucket = hit.collider.GetComponent<Bucket>();
                if (bucket == null)
                {
                    // Try parent (in case collider is on child)
                    bucket = hit.collider.GetComponentInParent<Bucket>();
                }

                if (bucket != null && !bucket.IsInCollectArea)
                {
                    Debug.Log($"[BucketInputController] Tapped bucket: {bucket.name}");

                    this.signalBus?.Fire(new BucketTappedSignal
                    {
                        BucketIndex = bucket.Data.IndexBucket,
                        Color = bucket.Data.Color
                    });
                }
            }
        }

        #endregion
    }
}
