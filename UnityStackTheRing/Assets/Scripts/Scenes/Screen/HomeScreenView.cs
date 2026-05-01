#nullable enable

namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using TMPro;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.UI;

    public class HomeScreenView : BaseView
    {
        private const int GRID_COLUMNS = 4;
        private const float GRID_SPACING = 20f;

        [SerializeField] private HomeLevelSelectGridAdapter? levelGridAdapter;
        [field: SerializeField] public Button? PlayButton { get; private set; }
        [field: SerializeField] public TMP_Text? SelectedLevelText { get; private set; }

        public HomeLevelSelectGridAdapter? LevelGridAdapter => this.levelGridAdapter;

        protected override void AwakeUnityEvent()
        {
            base.AwakeUnityEvent();
            this.ResolveRuntimeReferences();
            if (this.levelGridAdapter == null || this.PlayButton == null || this.SelectedLevelText == null)
            {
                this.BuildRuntimeUI();
            }
        }

        private void ResolveRuntimeReferences()
        {
            this.levelGridAdapter ??= this.GetComponentInChildren<HomeLevelSelectGridAdapter>(true);
            this.PlayButton ??= this.GetComponentInChildren<Button>(true);
            if (this.SelectedLevelText == null)
            {
                var selectedLevelTransform = this.transform.Find("SelectedLevelText");
                if (selectedLevelTransform != null)
                {
                    this.SelectedLevelText = selectedLevelTransform.GetComponent<TMP_Text>();
                }
            }
        }

        private void BuildRuntimeUI()
        {
            this.CreateLabel("TitleText", this.RectTransform, "SELECT LEVEL", 52, FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(520f, 90f));
            this.SelectedLevelText = this.CreateLabel("SelectedLevelText", this.RectTransform, "LEVEL 1", 34, FontStyles.Bold, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(320f, 60f));

            var gridRoot = this.CreateRectObject("LevelGrid", this.RectTransform);
            gridRoot.anchorMin = new Vector2(0.08f, 0.2f);
            gridRoot.anchorMax = new Vector2(0.92f, 0.78f);
            gridRoot.offsetMin = Vector2.zero;
            gridRoot.offsetMax = Vector2.zero;

            var gridBackground = gridRoot.gameObject.AddComponent<Image>();
            gridBackground.color = new Color(1f, 1f, 1f, 0.12f);

            var viewport = this.CreateRectObject("Viewport", gridRoot);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(18f, 18f);
            viewport.offsetMax = new Vector2(-18f, -18f);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = this.CreateRectObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var cellPrefab = this.CreateCellPrefab(gridRoot);
            cellPrefab.gameObject.SetActive(false);

            this.levelGridAdapter = gridRoot.gameObject.AddComponent<HomeLevelSelectGridAdapter>();
            this.levelGridAdapter.Configure(viewport, content, cellPrefab, GRID_COLUMNS, GRID_SPACING);

            this.PlayButton = this.CreateButton("PlayButton", this.RectTransform, "PLAY", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(280f, 88f));
        }

        private RectTransform CreateCellPrefab(Transform parent)
        {
            var cellRoot = this.CreateRectObject("CellPrefab", parent);
            cellRoot.anchorMin = new Vector2(0f, 1f);
            cellRoot.anchorMax = new Vector2(0f, 1f);
            cellRoot.pivot = new Vector2(0.5f, 0.5f);
            cellRoot.sizeDelta = new Vector2(150f, 150f);
            var layoutElement = cellRoot.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 150f;
            layoutElement.preferredHeight = 150f;

            var viewsRoot = this.CreateRectObject("Views", cellRoot);
            viewsRoot.anchorMin = Vector2.zero;
            viewsRoot.anchorMax = Vector2.one;
            viewsRoot.offsetMin = Vector2.zero;
            viewsRoot.offsetMax = Vector2.zero;

            var viewsBackground = viewsRoot.gameObject.AddComponent<Image>();
            viewsBackground.color = new Color(1f, 1f, 1f, 0.96f);
            var button = viewsRoot.gameObject.AddComponent<Button>();

            var selectedState = this.CreateRectObject("SelectedState", viewsRoot);
            selectedState.anchorMin = Vector2.zero;
            selectedState.anchorMax = Vector2.one;
            selectedState.offsetMin = Vector2.zero;
            selectedState.offsetMax = Vector2.zero;
            var selectedImage = selectedState.gameObject.AddComponent<Image>();
            selectedImage.color = new Color(0.2f, 0.65f, 1f, 0.28f);
            selectedState.gameObject.SetActive(false);

            var currentState = this.CreateLabel("CurrentState", viewsRoot, "CURRENT", 20, FontStyles.Bold, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(110f, 32f));
            currentState.alignment = TextAlignmentOptions.TopRight;
            currentState.color = new Color(0.2f, 0.65f, 1f, 1f);
            currentState.gameObject.SetActive(false);

            var levelText = this.CreateLabel("LevelText", viewsRoot, "1", 42, FontStyles.Bold, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 60f));
            levelText.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            var itemView = viewsRoot.gameObject.AddComponent<HomeLevelSelectItemView>();
            itemView.Configure(button, levelText, selectedState.gameObject, currentState.gameObject, null);
            return cellRoot;
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var buttonRoot = this.CreateRectObject(name, parent);
            buttonRoot.anchorMin = anchorMin;
            buttonRoot.anchorMax = anchorMax;
            buttonRoot.anchoredPosition = anchoredPosition;
            buttonRoot.sizeDelta = sizeDelta;

            var image = buttonRoot.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.55f, 0.98f, 1f);
            var button = buttonRoot.gameObject.AddComponent<Button>();
            var buttonColors = button.colors;
            buttonColors.normalColor = image.color;
            buttonColors.highlightedColor = new Color(0.25f, 0.65f, 1f, 1f);
            buttonColors.pressedColor = new Color(0.1f, 0.45f, 0.88f, 1f);
            buttonColors.selectedColor = buttonColors.highlightedColor;
            buttonColors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            button.colors = buttonColors;

            var labelText = this.CreateLabel("Label", buttonRoot, label, 34, FontStyles.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.color = Color.white;
            return button;
        }

        private TMP_Text CreateLabel(string name, Transform parent, string text, float fontSize, FontStyles fontStyle, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var labelRoot = this.CreateRectObject(name, parent);
            labelRoot.anchorMin = anchorMin;
            labelRoot.anchorMax = anchorMax;
            labelRoot.anchoredPosition = anchoredPosition;
            labelRoot.sizeDelta = sizeDelta;

            var textComponent = labelRoot.gameObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null)
            {
                textComponent.font = TMP_Settings.defaultFontAsset;
            }

            return textComponent;
        }

        private RectTransform CreateRectObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localScale = Vector3.one;
            return rectTransform;
        }
    }

    [ScreenInfo(nameof(HomeScreenView))]
    public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView>
    {
        private readonly GameStateMachine gameStateMachine;
        private readonly ILevelManager levelManager;
        private readonly List<HomeLevelItemModel> levelModels = new();
        private int selectedLevel;

        public HomeScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            GameStateMachine gameStateMachine,
            ILevelManager levelManager
        ) : base(signalBus, loggerManager)
        {
            this.gameStateMachine = gameStateMachine;
            this.levelManager = levelManager;
        }

        protected override void OnViewReady()
        {
            base.OnViewReady();
            if (this.View.PlayButton != null)
            {
                this.View.PlayButton.onClick.AddListener(this.OnClickPlay);
            }
        }

        public override async UniTask BindData()
        {
            var selectableLevels = this.levelManager.GetSelectableLevels();
            if (selectableLevels.Count == 0 || this.View.LevelGridAdapter == null)
            {
                if (this.View.PlayButton != null)
                {
                    this.View.PlayButton.interactable = false;
                }

                return;
            }

            var currentLevel = await this.levelManager.GetSavedCurrentLevel();
            this.selectedLevel = currentLevel;

            this.levelModels.Clear();
            for (var index = 0; index < selectableLevels.Count; index++)
            {
                var levelNumber = selectableLevels[index];
                this.levelModels.Add(new HomeLevelItemModel
                {
                    LevelNumber = levelNumber,
                    IsSelected = levelNumber == this.selectedLevel,
                    IsCurrentLevel = levelNumber == currentLevel,
                    IsUnlocked = true,
                    OnSelected = this.OnSelectedLevel
                });
            }

            await this.View.LevelGridAdapter.InitItemAdapter(this.levelModels);
            if (this.View.PlayButton != null)
            {
                this.View.PlayButton.interactable = true;
            }

            this.UpdateSelectedLevelText();
        }

        public override void Dispose()
        {
            if (this.View != null && this.View.PlayButton != null)
            {
                this.View.PlayButton.onClick.RemoveListener(this.OnClickPlay);
            }
        }

        private void OnSelectedLevel(int levelNumber)
        {
            if (this.selectedLevel == levelNumber)
            {
                return;
            }

            this.selectedLevel = levelNumber;
            for (var index = 0; index < this.levelModels.Count; index++)
            {
                this.levelModels[index].IsSelected = this.levelModels[index].LevelNumber == this.selectedLevel;
            }

            this.View.LevelGridAdapter?.ForceUpdateFullVisibleItems();
            this.UpdateSelectedLevelText();
        }

        private void OnClickPlay()
        {
            this.PlaySelectedLevel().Forget();
        }

        private async UniTask PlaySelectedLevel()
        {
            if (this.View.PlayButton != null)
            {
                this.View.PlayButton.interactable = false;
            }

            try
            {
                var levelController = await this.levelManager.LoadLevel(this.selectedLevel);
                if (levelController == null)
                {
                    if (this.View.SelectedLevelText != null)
                    {
                        this.View.SelectedLevelText.text = "LOAD FAILED";
                    }

                    return;
                }

                this.gameStateMachine.TransitionTo<GamePlayState>();
            }
            finally
            {
                if (this.View != null && this.View.PlayButton != null)
                {
                    this.View.PlayButton.interactable = true;
                }
            }
        }

        private void UpdateSelectedLevelText()
        {
            if (this.View.SelectedLevelText != null)
            {
                this.View.SelectedLevelText.text = $"LEVEL {this.selectedLevel}";
            }
        }
    }
}
