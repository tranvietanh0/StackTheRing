namespace HyperCasualGame.Scripts.Slot
{
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Signals;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class ColorCollector : MonoBehaviour, IPointerClickHandler
    {
        #region Serialized Fields

        [SerializeField] private ColorType colorType;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Transform visualTransform;

        #endregion

        #region Private Fields

        private SignalBus signalBus;
        private bool isPlaced;
        private bool isInteractable = true;

        #endregion

        #region Properties

        public ColorType ColorType => this.colorType;
        public bool IsPlaced => this.isPlaced;
        public bool IsInteractable => this.isInteractable;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            this.ApplyColor();
        }

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus)
        {
            this.signalBus = signalBus;
            this.isPlaced = false;
            this.isInteractable = true;

            if (this.visualTransform != null)
            {
                this.visualTransform.localScale = Vector3.one;
            }
        }

        public void SetColor(ColorType color)
        {
            this.colorType = color;
            this.ApplyColor();
        }

        public void SetInteractable(bool interactable)
        {
            this.isInteractable = interactable;
        }

        public void MarkAsPlaced()
        {
            this.isPlaced = true;
            this.isInteractable = false;

            if (this.visualTransform != null)
            {
                this.visualTransform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(() => this.gameObject.SetActive(false));
            }
            else
            {
                this.gameObject.SetActive(false);
            }
        }

        public void Reset()
        {
            this.isPlaced = false;
            this.isInteractable = true;
            this.gameObject.SetActive(true);

            if (this.visualTransform != null)
            {
                this.visualTransform.localScale = Vector3.zero;
                this.visualTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!this.isInteractable || this.isPlaced) return;

            this.PlayTapAnimation();

            this.signalBus?.Fire(new CollectorTappedSignal
            {
                Color = this.colorType
            });
        }

        #endregion

        #region Private Methods

        private void ApplyColor()
        {
            if (this.meshRenderer == null)
            {
                this.meshRenderer = this.GetComponent<MeshRenderer>();
                if (this.meshRenderer == null)
                {
                    this.meshRenderer = this.GetComponentInChildren<MeshRenderer>();
                }
            }

            if (this.meshRenderer == null)
            {
                Debug.LogWarning($"ColorCollector: MeshRenderer not found on {this.name}");
                return;
            }

            var color = GameConstants.GetColor(this.colorType);

            // Create instance material to avoid shared material issues
            var mat = this.meshRenderer.material;
            mat.SetColor("_BaseColor", color);
            mat.color = color; // Fallback for standard shader
        }

        private void PlayTapAnimation()
        {
            if (this.visualTransform == null) return;

            this.visualTransform.DOKill();
            this.visualTransform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 5, 0.5f);
        }

        #endregion
    }
}
