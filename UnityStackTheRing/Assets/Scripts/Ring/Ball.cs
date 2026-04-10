namespace HyperCasualGame.Scripts.Ring
{
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Signals;
    using UnityEngine;

    /// <summary>
    /// Individual ball component. Matches Cocos Ball class.
    /// </summary>
    public class Ball : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;

        public int RowId { get; set; } = -1;
        public int BallIndex { get; set; } = -1;
        public ColorType BallColor { get; private set; } = ColorType.Red;

        private bool isCollected;
        private SignalBus signalBus;

        public void Initialize(int rowId, int index, ColorType color, Vector3 localPos, SignalBus signalBus)
        {
            this.RowId = rowId;
            this.BallIndex = index;
            this.BallColor = color;
            this.signalBus = signalBus;
            this.isCollected = false;

            this.transform.localPosition = localPos;
            this.UpdateColor(color);
        }

        public void UpdateColor(ColorType color)
        {
            this.BallColor = color;

            if (this.meshRenderer == null)
            {
                this.meshRenderer = this.GetComponentInChildren<MeshRenderer>();
            }

            if (this.meshRenderer != null)
            {
                var mat = this.meshRenderer.material;
                var materialColor = GameConstants.GetColor(color);
                mat.color = materialColor;
                // URP uses _BaseColor
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", materialColor);
                }
            }
        }

        public void JumpToBucket(Transform targetBucket, System.Action onComplete = null)
        {
            if (this.isCollected)
            {
                return;
            }

            this.isCollected = true;

            // Fire collected signal
            this.signalBus?.Fire(new BallCollectedSignal
            {
                RowId = this.RowId,
                BallIndex = this.BallIndex,
                Color = this.BallColor
            });

            // Jump animation using DOTween
            var startPos = this.transform.position;
            var endPos = targetBucket.position;
            var jumpHeight = GameConstants.BallConfig.JumpHeight;
            var duration = GameConstants.BallConfig.JumpDuration;

            // Detach from parent for jump
            this.transform.SetParent(null);

            this.transform.DOJump(endPos, jumpHeight, 1, duration)
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                    Destroy(this.gameObject);
                });
        }
    }
}
