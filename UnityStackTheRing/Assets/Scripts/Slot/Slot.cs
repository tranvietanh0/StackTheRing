namespace HyperCasualGame.Scripts.Slot
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Ring;
    using UnityEngine;

    public class Slot : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private int slotIndex;
        [SerializeField] private Transform stackContainer;
        [SerializeField] private Transform collectorVisual;
        [SerializeField] private float ballStackSpacing = 0.15f;

        #endregion

        #region Private Fields

        private readonly Stack<Ball> ballStack = new();
        private int stackLimit = 8;
        private MeshRenderer collectorRenderer;

        #endregion

        #region Properties

        public int SlotIndex => this.slotIndex;
        public ColorType? CurrentColor { get; private set; }
        public bool IsEmpty => this.CurrentColor == null;
        public bool IsFull => this.ballStack.Count >= this.stackLimit;
        public int CurrentStackCount => this.ballStack.Count;
        public float AttractionProgress { get; set; }

        #endregion

        #region Events

        public event Action<Slot> OnStackFull;
        public event Action<Slot, Ball> OnBallAdded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (this.collectorVisual != null)
            {
                this.collectorRenderer = this.collectorVisual.GetComponent<MeshRenderer>();
                this.collectorVisual.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Public Methods

        public void Initialize(int stackLimit, float attractionProgress)
        {
            this.stackLimit = stackLimit;
            this.AttractionProgress = attractionProgress;
            this.ClearSlot();
        }

        public void AssignColor(ColorType color)
        {
            this.CurrentColor = color;

            if (this.collectorVisual != null)
            {
                this.collectorVisual.gameObject.SetActive(true);
                this.ApplyCollectorColor(color);
                this.PlayPlaceAnimation();
            }
        }

        public void AddBall(Ball ball)
        {
            ball.transform.SetParent(this.stackContainer);

            var stackPosition = this.GetNextStackPosition();
            ball.transform.DOLocalMove(stackPosition, 0.1f).SetEase(Ease.OutBack);

            this.ballStack.Push(ball);
            this.OnBallAdded?.Invoke(this, ball);

            if (this.IsFull)
            {
                this.OnStackFull?.Invoke(this);
            }
        }

        public Vector3 GetNextStackPosition()
        {
            var heightOffset = this.ballStack.Count * this.ballStackSpacing;
            return new Vector3(0f, heightOffset, 0f);
        }

        public Vector3 GetNextStackWorldPosition()
        {
            return this.stackContainer.TransformPoint(this.GetNextStackPosition());
        }

        public List<Ball> GetAllBalls()
        {
            return new List<Ball>(this.ballStack);
        }

        public void ClearStack(Action<Ball> onBallCleared = null)
        {
            while (this.ballStack.Count > 0)
            {
                var ball = this.ballStack.Pop();
                onBallCleared?.Invoke(ball);
            }

            this.ClearSlot();
        }

        public void ClearSlot()
        {
            this.CurrentColor = null;

            if (this.collectorVisual != null)
            {
                this.collectorVisual.gameObject.SetActive(false);
            }

            foreach (var ball in this.ballStack)
            {
                if (ball != null)
                {
                    Destroy(ball.gameObject);
                }
            }
            this.ballStack.Clear();
        }

        #endregion

        #region Private Methods

        private void ApplyCollectorColor(ColorType color)
        {
            if (this.collectorRenderer == null) return;

            var materialColor = GameConstants.GetColor(color);

            var mat = this.collectorRenderer.material;
            mat.SetColor("_BaseColor", materialColor);
            mat.color = materialColor;
        }

        private void PlayPlaceAnimation()
        {
            if (this.collectorVisual == null) return;

            this.collectorVisual.localScale = Vector3.zero;
            this.collectorVisual.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        #endregion
    }
}
