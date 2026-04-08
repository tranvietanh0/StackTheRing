# Phase 9: UI Screens

**Effort:** S (1 day)
**Dependencies:** Phase 7, Phase 8
**Blocks:** Phase 10

## Objective

Create MVP UI screens: Game HUD and Win popup.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/Scenes/Screen/GameHUDScreenView.cs` | CREATE | this phase |
| `Assets/Scripts/Scenes/Screen/GameWinScreenView.cs` | CREATE | this phase |

## Implementation

### 1. GameHUDScreenView.cs

In-game HUD showing progress. **Model + View + Presenter in ONE file.**

```csharp
namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.StackTheRing.Core;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using TMPro;
    using UniT.Logging;
    using UnityEngine;

    // ===== MODEL =====
    public class GameHUDModel
    {
        public int CompletedBuckets;
        public int TotalBuckets;
    }

    // ===== VIEW =====
    public class GameHUDScreenView : BaseView
    {
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI levelText;

        public void SetProgress(int completed, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{completed}/{total}";
            }
        }

        public void SetLevelText(string text)
        {
            if (levelText != null)
            {
                levelText.text = text;
            }
        }
    }

    // ===== PRESENTER =====
    [ScreenInfo(nameof(GameHUDScreenView))]
    public class GameHUDScreenPresenter : BaseScreenPresenter<GameHUDScreenView>
    {
        #region Inject

        private readonly SignalBus signalBus;
        private readonly StackTheRingController gameController;

        public GameHUDScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager,
            StackTheRingController gameController)
            : base(signalBus, loggerManager)
        {
            this.signalBus = signalBus;
            this.gameController = gameController;
        }

        #endregion

        private int completedBuckets;
        private int totalBuckets;

        public override async UniTask BindData()
        {
            // Subscribe to bucket completion
            signalBus.Subscribe<BucketCompletedSignal>(OnBucketCompleted);

            // Initialize from game controller
            totalBuckets = gameController.GetTotalBuckets();
            completedBuckets = gameController.GetCompletedBuckets();

            UpdateUI();

            await UniTask.CompletedTask;
        }

        protected override void OnClose()
        {
            signalBus.Unsubscribe<BucketCompletedSignal>(OnBucketCompleted);
            base.OnClose();
        }

        private void OnBucketCompleted(BucketCompletedSignal signal)
        {
            completedBuckets = gameController.GetCompletedBuckets();
            UpdateUI();
        }

        private void UpdateUI()
        {
            View.SetProgress(completedBuckets, totalBuckets);
            View.SetLevelText("Level 1"); // MVP: hardcoded
        }
    }
}
```

### 2. GameWinScreenView.cs

Win popup with replay button. **Model + View + Presenter in ONE file.**

```csharp
namespace HyperCasualGame.Scripts.Scenes.Screen
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using TMPro;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.UI;

    // ===== MODEL =====
    public class GameWinModel
    {
        public int LevelIndex;
        public int Score;
    }

    // ===== VIEW =====
    public class GameWinScreenView : BaseView
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button nextButton;

        public Button ReplayButton => replayButton;
        public Button NextButton => nextButton;

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }
    }

    // ===== PRESENTER =====
    [ScreenInfo(nameof(GameWinScreenView))]
    public class GameWinScreenPresenter : BaseScreenPresenter<GameWinScreenView>
    {
        #region Inject

        private readonly SignalBus signalBus;

        public GameWinScreenPresenter(
            SignalBus signalBus,
            ILoggerManager loggerManager)
            : base(signalBus, loggerManager)
        {
            this.signalBus = signalBus;
        }

        #endregion

        public override async UniTask BindData()
        {
            // Setup UI
            View.SetTitle("Level Complete!");
            View.SetMessage("Great job!");

            // Setup button listeners
            if (View.ReplayButton != null)
            {
                View.ReplayButton.onClick.AddListener(OnReplayClicked);
            }

            if (View.NextButton != null)
            {
                View.NextButton.onClick.AddListener(OnNextClicked);
            }

            await UniTask.CompletedTask;
        }

        protected override void OnClose()
        {
            // Remove listeners
            if (View.ReplayButton != null)
            {
                View.ReplayButton.onClick.RemoveListener(OnReplayClicked);
            }

            if (View.NextButton != null)
            {
                View.NextButton.onClick.RemoveListener(OnNextClicked);
            }

            base.OnClose();
        }

        private void OnReplayClicked()
        {
            // Close this screen
            Close();

            // Request next level (which restarts for MVP)
            signalBus.Fire(new NextLevelRequestedSignal(0));
        }

        private void OnNextClicked()
        {
            // Same as replay for MVP
            OnReplayClicked();
        }
    }
}
```

## Prefab Requirements

### GameHUDScreenView Prefab
```
GameHUDScreenView (Canvas)
├── Background (Image, optional)
├── TopBar (RectTransform)
│   ├── LevelText (TMP)
│   └── ProgressText (TMP)
└── (BaseView component)
```

### GameWinScreenView Prefab
```
GameWinScreenView (Canvas)
├── Panel (Image, semi-transparent)
│   ├── TitleText (TMP) - "Level Complete!"
│   ├── MessageText (TMP) - "Great job!"
│   ├── ReplayButton (Button + TMP child)
│   └── NextButton (Button + TMP child)
└── (BaseView component)
```

## Verification

- [ ] Both files compile without errors
- [ ] Model + View + Presenter in same file (per project standards)
- [ ] File names end with `ScreenView.cs`
- [ ] Constructor injection only (no `[Inject]`)
- [ ] `[ScreenInfo]` attribute correctly references View class name
- [ ] Unsubscribe signals in OnClose()

## Notes

- Prefabs must be named exactly as View class (`GameHUDScreenView`, `GameWinScreenView`)
- Prefabs should be in Addressables with matching key
- BaseView provides animation lifecycle (open/close animations)
- Button listeners removed in OnClose() to prevent memory leaks
