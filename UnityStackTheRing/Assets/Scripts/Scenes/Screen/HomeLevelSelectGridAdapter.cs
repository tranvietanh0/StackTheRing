#nullable enable

namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using System;
    using Com.ForbiddenByte.OSA.CustomAdapters.GridView;
    using GameFoundationCore.Scripts.AssetLibrary;
    using GameFoundationCore.Scripts.UIModule.Adapter;
    using GameFoundationCore.Scripts.UIModule.MVP;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public sealed class HomeLevelSelectGridAdapter : BasicGridAdapter<HomeLevelItemModel, HomeLevelSelectItemView, HomeLevelSelectItemPresenter>
    {
        private RectTransform? viewport;
        private RectTransform? content;
        private RectTransform? cellPrefab;
        private int maxCellsPerGroup;
        private float spacing;

        public void Configure(RectTransform viewport, RectTransform content, RectTransform cellPrefab, int maxCellsPerGroup, float spacing)
        {
            this.viewport = viewport;
            this.content = content;
            this.cellPrefab = cellPrefab;
            this.maxCellsPerGroup = maxCellsPerGroup;
            this.spacing = spacing;
        }

        protected override void Start()
        {
            this._Params ??= new GridParams();
            if (this.viewport != null && this.content != null && this.cellPrefab != null)
            {
                this._Params.Viewport = this.viewport;
                this._Params.Content = this.content;
                this._Params.ContentSpacing = this.spacing;
                this._Params.Grid.CellPrefab = this.cellPrefab;
                this._Params.Grid.MaxCellsPerGroup = this.maxCellsPerGroup;
                this._Params.Grid.SpacingInGroup = this.spacing;
                this._Params.Grid.AlignmentOfCellsInGroup = TextAnchor.UpperCenter;
            }

            base.Start();
        }
    }

    public sealed class HomeLevelItemModel
    {
        public int LevelNumber;
        public bool IsSelected;
        public bool IsCurrentLevel;
        public bool IsUnlocked;
        public Action<int>? OnSelected;
    }

    public sealed class HomeLevelSelectItemView : MonoBehaviour, IUIView
    {
        [SerializeField] private Button button = null!;
        [SerializeField] private TMP_Text levelText = null!;
        [SerializeField] private GameObject? selectedState;
        [SerializeField] private GameObject? currentState;
        [SerializeField] private GameObject? lockedState;

        public Button Button => this.button;
        public TMP_Text LevelText => this.levelText;
        public GameObject? SelectedState => this.selectedState;
        public GameObject? CurrentState => this.currentState;
        public GameObject? LockedState => this.lockedState;

        private void Awake()
        {
            this.button ??= this.GetComponent<Button>();
            this.levelText ??= this.transform.Find("LevelText")?.GetComponent<TMP_Text>();
            this.selectedState ??= this.transform.Find("SelectedState")?.gameObject;
            this.currentState ??= this.transform.Find("CurrentState")?.gameObject;
            this.lockedState ??= this.transform.Find("LockedState")?.gameObject;
        }

        public void Configure(Button button, TMP_Text levelText, GameObject? selectedState, GameObject? currentState, GameObject? lockedState)
        {
            this.button = button;
            this.levelText = levelText;
            this.selectedState = selectedState;
            this.currentState = currentState;
            this.lockedState = lockedState;
        }
    }

    public sealed class HomeLevelSelectItemPresenter : BaseUIItemPresenter<HomeLevelSelectItemView, HomeLevelItemModel>
    {
        private HomeLevelItemModel? model;

        public HomeLevelSelectItemPresenter(IGameAssets gameAssets) : base(gameAssets)
        {
        }

        public override void BindData(HomeLevelItemModel param)
        {
            this.UnbindClick();
            this.model = param;

            this.View.LevelText.text = param.LevelNumber.ToString();
            this.View.Button.interactable = param.IsUnlocked;
            if (this.View.SelectedState != null)
            {
                this.View.SelectedState.SetActive(param.IsSelected);
            }

            if (this.View.CurrentState != null)
            {
                this.View.CurrentState.SetActive(param.IsCurrentLevel);
            }

            if (this.View.LockedState != null)
            {
                this.View.LockedState.SetActive(!param.IsUnlocked);
            }

            this.View.Button.onClick.AddListener(this.OnClick);
        }

        public override void Dispose()
        {
            this.UnbindClick();
            this.model = null;
        }

        private void OnClick()
        {
            if (this.model == null || !this.model.IsUnlocked)
            {
                return;
            }

            this.model.OnSelected?.Invoke(this.model.LevelNumber);
        }

        private void UnbindClick()
        {
            if (this.View != null && this.View.Button != null)
            {
                this.View.Button.onClick.RemoveListener(this.OnClick);
            }
        }
    }
}
