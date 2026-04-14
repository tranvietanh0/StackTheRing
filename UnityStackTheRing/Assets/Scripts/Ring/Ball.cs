namespace HyperCasualGame.Scripts.Ring
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Effects;
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
            var hasReservation = incomingAlreadyReserved;
            var reservationCompleted = false;

            try
            {
                if (this.isCollected || targetBucket == null || targetBucket.IsBucketCompleted())
                {
                    Debug.Log($"[Ball] JumpAbort row={this.RowId} ball={this.BallColor}:{this.BallIndex} reason=invalid-target reserved={hasReservation}");
                    return;
                }

                // Check available slots
                var incomingToCheck = incomingAlreadyReserved ? -1 : 1;
                var availableSlots = targetBucket.GetRemainingSlotCount(incomingToCheck);
                if (availableSlots <= 0)
                {
                    Debug.Log($"[Ball] JumpAbort row={this.RowId} ball={this.BallColor}:{this.BallIndex} reason=no-slots bucket={targetBucket.Data.IndexBucket} reserved={hasReservation} collected={targetBucket.CollectedBallCount} incoming={targetBucket.IncomingBallCount} target={targetBucket.TargetBallCount}");
                    return;
                }

                this.isCollected = true;

                if (!incomingAlreadyReserved)
                {
                    targetBucket.StartIncomingBall();
                    hasReservation = true;
                }

                Debug.Log($"[Ball] JumpStart row={this.RowId} ball={this.BallColor}:{this.BallIndex} bucket={targetBucket.Data.IndexBucket} reserved={hasReservation} collected={targetBucket.CollectedBallCount} incoming={targetBucket.IncomingBallCount} target={targetBucket.TargetBallCount}");

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

                // === Landing Effect Sequence ===

                // 1. Reserve stack position (prevents race condition with concurrent rings)
                var stackPos = targetBucket.ReserveNextStackPosition();

                // 2. Reparent to bucket's stack root for local space calculations
                this.transform.SetParent(targetBucket.StackRoot);

                // 3. Play sparkle VFX (fire and forget)
                SparkleEffectPool.Instance?.PlayAtFireForget(this.transform.position, this.BallColor);

                // 4. Play wobble animation
                var landingEffect = this.GetComponent<RingLandingEffect>();
                if (landingEffect == null)
                {
                    landingEffect = this.gameObject.AddComponent<RingLandingEffect>();
                }

                // Scale down for stack
                this.transform.localScale = Vector3.one * GameConstants.RingStackConfig.RingScaleOnStack;

                await landingEffect.PlayLandingEffect(stackPos);

                // 5. Register with bucket tracking
                targetBucket.AddBall(this);
                targetBucket.AddRingToStack(this.transform);

                // === End Landing Effect ===

                targetBucket.CompleteIncomingBall();
                reservationCompleted = true;
                Debug.Log($"[Ball] JumpComplete row={this.RowId} ball={this.BallColor}:{this.BallIndex} bucket={targetBucket.Data.IndexBucket} collected={targetBucket.CollectedBallCount} incoming={targetBucket.IncomingBallCount} target={targetBucket.TargetBallCount}");

                // Ring stays visible on stack (no longer deactivated)
            }
            finally
            {
                if (targetBucket != null && hasReservation && !reservationCompleted)
                {
                    Debug.Log($"[Ball] JumpRelease row={this.RowId} ball={this.BallColor}:{this.BallIndex} bucket={targetBucket.Data.IndexBucket} reason=finally-release collected={targetBucket.CollectedBallCount} incoming={targetBucket.IncomingBallCount} target={targetBucket.TargetBallCount}");
                    targetBucket.CompleteIncomingBall();
                }
            }
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
