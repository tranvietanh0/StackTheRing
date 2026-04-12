namespace HyperCasualGame.Scripts.Ring
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Services;
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

        public bool IsCollected => this.isCollected;

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

        /// <summary>
        /// Jump ball to a Bucket component. Handles incoming reservation.
        /// Matches Cocos Ball.jumpToBucket()
        /// </summary>
        public async UniTask JumpToBucket(Bucket targetBucket, bool incomingAlreadyReserved = false)
        {
            if (this.isCollected || targetBucket == null || targetBucket.IsBucketCompleted())
            {
                return;
            }

            // Check available slots
            var incomingToCheck = incomingAlreadyReserved ? 0 : 1;
            var availableSlots = targetBucket.GetRemainingSlotCount(incomingToCheck);
            if (availableSlots <= 0)
            {
                return;
            }

            this.isCollected = true;

            if (!incomingAlreadyReserved)
            {
                targetBucket.StartIncomingBall();
            }

            // Fire collected signal
            this.signalBus?.Fire(new BallCollectedSignal
            {
                RowId = this.RowId,
                BallIndex = this.BallIndex,
                Color = this.BallColor
            });

            // Detach from parent for jump
            this.transform.SetParent(null);

            // Jump animation using JumpService
            await JumpService.Instance.JumpToDestination(
                this.transform,
                targetBucket.transform,
                GameConstants.BallConfig.JumpHeight,
                GameConstants.BallConfig.JumpDuration,
                Vector3.zero
            );

            // Add to bucket and complete incoming reservation
            targetBucket.AddBall(this);
            targetBucket.CompleteIncomingBall();

            // Hide after adding (bucket controls visibility)
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// Legacy jump to Transform (for backwards compatibility).
        /// </summary>
        public void JumpToTransform(Transform targetBucket, System.Action onComplete = null)
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
