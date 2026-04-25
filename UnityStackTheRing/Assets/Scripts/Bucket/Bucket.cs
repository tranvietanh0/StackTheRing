namespace HyperCasualGame.Scripts.Bucket
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using TMPro;
    using UnityEngine;
    using CollectAreaComponent = HyperCasualGame.Scripts.CollectArea.CollectArea;

    /// <summary>
    /// Container for balls. Can jump from grid to CollectArea.
    /// Matches Cocos Bucket.ts
    /// </summary>
    public class Bucket : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private MeshRenderer[] meshRenderers;
        [SerializeField] private TextMeshPro labelPercent;
        [SerializeField] private GameObject hiddenIndicator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform stackRoot;
        [SerializeField] private Vector3 normalTextLocalPosition;
        [SerializeField] private Vector3 lockedTextLocalPosition;

        #endregion

        #region Private Fields

        private BucketConfig data;
        private readonly List<Ball> collectedBalls = new();
        private readonly List<Transform> stackedRings = new();
        private int incomingBalls;
        private int pendingRingCount;
        private bool isCompleted;
        private bool collectAreaReleased;
        private bool isHidden;
        private bool isLocked;
        private bool hasLockedRequirement;
        private int remainingBallsToUnlock;
        private SignalBus signalBus;
        private Vector3 shakeBaseScale = Vector3.one;
        private Tween shakeTween;

        #endregion

        #region Properties

        public BucketConfig Data => this.data;
        public bool IsInCollectArea { get; private set; }
        public bool IsHidden => this.isHidden;
        public bool IsLocked => this.isLocked;
        public int RemainingBallsToUnlock => Mathf.Max(0, this.remainingBallsToUnlock);
        public int TargetBallCount => Mathf.Max(1, this.data.TargetBallCount);
        public int CollectedBallCount => this.collectedBalls.Count;
        public int IncomingBallCount => this.incomingBalls;

        /// <summary>Transform for stacking rings on this bucket</summary>
        public Transform StackRoot => this.stackRoot != null ? this.stackRoot : this.transform;

        #endregion

        #region Events

        public event Action<Bucket> OnBucketCompleted;
        public event Action<Bucket, Ball> OnBallAdded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (this.visualRoot != null)
            {
                this.shakeBaseScale = this.visualRoot.localScale;
            }

            // Auto-find MeshRenderers if not assigned
            if (this.meshRenderers == null || this.meshRenderers.Length == 0)
            {
                this.meshRenderers = this.GetComponentsInChildren<MeshRenderer>();
            }

            if (this.labelPercent != null
                && this.normalTextLocalPosition == Vector3.zero
                && this.lockedTextLocalPosition == Vector3.zero
                && this.labelPercent.transform.localPosition != Vector3.zero)
            {
                this.normalTextLocalPosition = this.labelPercent.transform.localPosition;
                this.lockedTextLocalPosition = this.labelPercent.transform.localPosition;
            }
        }

        private void OnDestroy()
        {
            this.shakeTween?.Kill();
        }

        private void OnMouseDown()
        {
            if (!this.CanReceiveTap())
            {
                return;
            }

            this.signalBus?.Fire(new BucketTappedSignal
            {
                BucketIndex = this.data.IndexBucket,
                Color = this.data.Color
            });
        }

        #endregion

        #region Public Methods

        public void Initialize(BucketConfig config, SignalBus signalBus)
        {
            this.data = config;
            this.signalBus = signalBus;
            this.IsInCollectArea = false;
            this.collectedBalls.Clear();
            this.stackedRings.Clear();
            this.isCompleted = false;
            this.incomingBalls = 0;
            this.pendingRingCount = 0;
            this.collectAreaReleased = false;
            this.isHidden = config.IsHidden;
            this.hasLockedRequirement = config.IsLocked && config.RequiredBallsToUnlock > 0;
            this.isLocked = false;
            this.remainingBallsToUnlock = 0;

            this.RefreshLockedProgress(0);
            this.UpdateColor(this.isHidden ? ColorType.Black : config.Color);
            this.UpdateProgressUI();
            this.UpdateState();
        }

        public void Reveal()
        {
            if (!this.isHidden)
            {
                return;
            }

            this.isHidden = false;
            this.UpdateColor(this.data.Color);
            this.UpdateProgressUI();
            this.UpdateState();
        }

        public bool CanReceiveTap()
        {
            return !this.IsInCollectArea && !this.isHidden && !this.isLocked;
        }

        public void RefreshLockedProgress(int collectedUnlockBallCount)
        {
            if (!this.hasLockedRequirement)
            {
                this.isLocked = false;
                this.remainingBallsToUnlock = 0;
                this.UpdateProgressUI();
                this.UpdateState();
                return;
            }

            this.remainingBallsToUnlock = Mathf.Max(0, this.data.RequiredBallsToUnlock - Mathf.Max(0, collectedUnlockBallCount));
            this.isLocked = this.remainingBallsToUnlock > 0;
            this.UpdateProgressUI();
            this.UpdateState();
        }

        public void UpdateState()
        {
            if (this.labelPercent != null)
            {
                var shouldShowFallbackQuestionMark = this.isHidden && this.data.ShowQuestionMark && this.hiddenIndicator == null;
                var shouldShowLockedCounter = this.isLocked && !this.IsInCollectArea && !this.isHidden;
                this.UpdateTextPosition();
                this.labelPercent.gameObject.SetActive(this.IsInCollectArea || shouldShowFallbackQuestionMark || shouldShowLockedCounter);
            }

            if (this.hiddenIndicator != null)
            {
                this.hiddenIndicator.SetActive(this.isHidden && this.data.ShowQuestionMark && !this.IsInCollectArea);
            }
        }

        public void UpdateColor(ColorType color)
        {
            var materialColor = GameConstants.GetColor(color);

            foreach (var renderer in this.meshRenderers)
            {
                if (renderer == null) continue;

                var mat = renderer.material;
                mat.color = materialColor;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", materialColor);
                }
            }
        }

        /// <summary>
        /// Jump this bucket to a CollectArea.
        /// </summary>
        public async UniTask JumpToCollectArea(Transform targetArea)
        {
            var columnParent = this.transform.parent;

            this.IsInCollectArea = true;

            await JumpService.Instance.JumpToDestination(
                this.transform,
                targetArea,
                GameConstants.BucketConfig.DefaultJumpHeight,
                GameConstants.BucketConfig.DefaultJumpDuration,
                new Vector3(
                    GameConstants.BucketConfig.DefaultJumpEndRotationX,
                    GameConstants.BucketConfig.DefaultJumpEndRotationY,
                    GameConstants.BucketConfig.DefaultJumpEndRotationZ
                )
            );

            this.UpdateState();

            if (GameConstants.BucketConfig.ParentToCollectArea)
            {
                this.transform.SetParent(targetArea, true);

                if (GameConstants.BucketConfig.ResetPositionAfterLanding)
                {
                    this.transform.localPosition = Vector3.zero;
                }
            }

            this.UpdateNextBucketInColumn(columnParent);
        }

        /// <summary>
        /// Add a ball that has landed in this bucket.
        /// </summary>
        public void AddBall(Ball ball)
        {
            if (this.isCompleted)
            {
                Debug.LogWarning("[Bucket] Bucket is already completed, cannot add more balls");
                return;
            }

            this.collectedBalls.Add(ball);
            Debug.Log($"[Bucket] AddBall bucket={this.data.IndexBucket} color={this.data.Color} collected={this.collectedBalls.Count} incoming={this.incomingBalls} target={this.TargetBallCount} ball={ball.BallColor}:{ball.BallIndex}");
            this.UpdateProgressUI();
            this.TriggerShake();

            this.OnBallAdded?.Invoke(this, ball);
            this.CheckComplete();
        }

        /// <summary>
        /// Reserve a slot for a ball that is flying towards this bucket.
        /// </summary>
        public void StartIncomingBall()
        {
            this.incomingBalls++;
            Debug.Log($"[Bucket] StartIncoming bucket={this.data.IndexBucket} color={this.data.Color} collected={this.collectedBalls.Count} incoming={this.incomingBalls} target={this.TargetBallCount}");
        }

        /// <summary>
        /// Complete an incoming ball reservation (ball has landed or failed).
        /// </summary>
        public void CompleteIncomingBall()
        {
            this.incomingBalls = Mathf.Max(0, this.incomingBalls - 1);
            Debug.Log($"[Bucket] CompleteIncoming bucket={this.data.IndexBucket} color={this.data.Color} collected={this.collectedBalls.Count} incoming={this.incomingBalls} target={this.TargetBallCount}");
            this.CheckComplete();
        }

        /// <summary>
        /// Get remaining slot count for balls.
        /// </summary>
        public int GetRemainingSlotCount(int additionalIncoming = 0)
        {
            var totalIncoming = this.incomingBalls + additionalIncoming;
            return Mathf.Max(0, this.TargetBallCount - this.collectedBalls.Count - totalIncoming);
        }

        /// <summary>
        /// Check if this bucket is fully filled.
        /// </summary>
        public bool IsBucketCompleted()
        {
            return this.isCompleted;
        }

        /// <summary>
        /// Get the projected fill ratio including incoming balls.
        /// </summary>
        public float GetProjectedFillRatio(int additionalIncoming = 0)
        {
            if (this.TargetBallCount <= 0) return 1f;

            var projected = this.collectedBalls.Count + this.incomingBalls + additionalIncoming;
            return (float)projected / this.TargetBallCount;
        }

        /// <summary>
        /// Reserve the next stack position for a landing ring.
        /// Atomically increments pending count to prevent race conditions.
        /// </summary>
        public Vector3 ReserveNextStackPosition()
        {
            // Include pending rings to avoid position collisions
            var stackIndex = this.stackedRings.Count + this.pendingRingCount;
            this.pendingRingCount++;

            var y = GameConstants.RingStackConfig.BaseStackY
                  + stackIndex * GameConstants.RingStackConfig.RingHeight;
            return new Vector3(0, y, 0);
        }

        /// <summary>
        /// Add a ring to the visible stack after landing animation completes.
        /// Decrements pending count reserved by ReserveNextStackPosition.
        /// </summary>
        public void AddRingToStack(Transform ringTransform)
        {
            // Decrement pending count (self-healing: clamp to 0)
            this.pendingRingCount = Mathf.Max(0, this.pendingRingCount - 1);

            if (ringTransform == null) return;

            this.stackedRings.Add(ringTransform);
            // Note: No fade logic - rings stay visible until bucket completes
        }

        private void UpdateProgressUI()
        {
            if (this.labelPercent == null) return;

            if (this.isHidden && this.data.ShowQuestionMark && !this.IsInCollectArea)
            {
                this.labelPercent.text = "?";
                return;
            }

            if (this.isLocked && !this.IsInCollectArea)
            {
                this.labelPercent.text = this.RemainingBallsToUnlock.ToString();
                return;
            }

            var percent = Mathf.Min(100, Mathf.FloorToInt((float)this.collectedBalls.Count / this.TargetBallCount * 100));
            this.labelPercent.text = $"{percent}%";
        }

        private void UpdateTextPosition()
        {
            if (this.labelPercent == null)
            {
                return;
            }

            this.labelPercent.transform.localPosition = this.isLocked && !this.IsInCollectArea
                ? this.lockedTextLocalPosition
                : this.normalTextLocalPosition;
        }

        private void CheckComplete()
        {
            if (this.isCompleted) return;

            if (this.collectedBalls.Count >= this.TargetBallCount && this.incomingBalls == 0)
            {
                this.OnBucketCollectDone().Forget();
            }
        }

        private async UniTask OnBucketCollectDone()
        {
            if (this.isCompleted) return;

            this.isCompleted = true;
            await this.PlayCollectionAnimation();

            this.signalBus?.Fire(new BucketCompletedSignal
            {
                Color = this.data.Color,
                BucketIndex = this.data.IndexBucket
            });

            this.OnBucketCompleted?.Invoke(this);
            this.Cleanup();
        }

        private async UniTask PlayCollectionAnimation()
        {
            this.HideUIElements();
            this.DestroyAllBalls();
            this.ReleaseCollectAreaSlot();

            await this.AnimateMoveUp();
            await this.AnimateRotation();
            await this.AnimateScaleDown();
        }

        private async UniTask AnimateMoveUp()
        {
            var startPos = this.transform.position;
            var endPos = startPos + Vector3.up * GameConstants.BucketConfig.CollectionMoveUpOffset;

            await this.transform
                .DOMove(endPos, GameConstants.BucketConfig.CollectionMoveUpDuration)
                .AsyncWaitForCompletion();
        }

        private async UniTask AnimateRotation()
        {
            var startRot = this.transform.eulerAngles;
            var endRot = new Vector3(startRot.x, GameConstants.BucketConfig.CollectionRotationEnd, startRot.z);

            await this.transform
                .DORotate(endRot, GameConstants.BucketConfig.CollectionRotationDuration)
                .AsyncWaitForCompletion();
        }

        private async UniTask AnimateScaleDown()
        {
            await this.transform
                .DOScale(GameConstants.BucketConfig.CollectionScaleEnd, GameConstants.BucketConfig.CollectionScaleDuration)
                .AsyncWaitForCompletion();
        }

        private void HideUIElements()
        {
            if (this.labelPercent != null)
            {
                this.labelPercent.gameObject.SetActive(false);
            }
        }

        private void DestroyAllBalls()
        {
            // Destroy tracked balls
            foreach (var ball in this.collectedBalls)
            {
                if (ball != null)
                {
                    Destroy(ball.gameObject);
                }
            }
            this.collectedBalls.Clear();

            // Destroy stacked ring visuals
            foreach (var ringTransform in this.stackedRings)
            {
                if (ringTransform != null)
                {
                    Destroy(ringTransform.gameObject);
                }
            }
            this.stackedRings.Clear();
        }

        private void ReleaseCollectAreaSlot()
        {
            if (this.collectAreaReleased || !this.IsInCollectArea || this.transform.parent == null) return;

            var collectArea = this.transform.parent.GetComponent<CollectAreaComponent>();
            if (collectArea != null)
            {
                collectArea.Release();
                this.collectAreaReleased = true;
            }
        }

        private void Cleanup()
        {
            if (!this.collectAreaReleased)
            {
                this.ReleaseCollectAreaSlot();
            }

            Destroy(this.gameObject);
        }

        private void TriggerShake()
        {
            if (this.visualRoot == null) return;

            this.shakeTween?.Kill();
            this.visualRoot.localScale = this.shakeBaseScale;

            var bumpScale = this.shakeBaseScale + Vector3.one * GameConstants.BucketConfig.ShakeScaleBump;

            this.shakeTween = DOTween.Sequence()
                .Append(this.visualRoot.DOScale(bumpScale, GameConstants.BucketConfig.ShakeDuration / 2f))
                .Append(this.visualRoot.DOScale(this.shakeBaseScale, GameConstants.BucketConfig.ShakeDuration / 2f));
        }

        private void UpdateNextBucketInColumn(Transform columnParent)
        {
            if (columnParent == null) return;

            foreach (Transform child in columnParent)
            {
                var bucket = child.GetComponent<Bucket>();
                if (bucket != null && !bucket.IsInCollectArea)
                {
                    bucket.UpdateState();
                    break;
                }
            }
        }
        #endregion
    }
}
