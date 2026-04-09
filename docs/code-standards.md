# Code Standards — Stack The Ring

## Naming Conventions

### General Rules

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase, match folder | `HyperCasualGame.Scripts.StateMachines` |
| Class/Interface | PascalCase | `GameStateMachine`, `IGameState` |
| Interface | Prefix with `I` | `IScreenManager`, `IGameAssets` |
| Method | PascalCase | `TransitionTo()`, `LoadSceneAsync()` |
| Property | PascalCase | `StateMachine`, `NextSceneName` |
| Field (private) | camelCase | `screenManager`, `userDataManager` |
| Field (readonly) | camelCase | `private readonly IGameAssets gameAssets` |
| Constant | PascalCase or UPPER_SNAKE | `MaxRetries`, `DEFAULT_SCENE` |
| Parameter | camelCase | `(SignalBus signalBus)` |
| Local variable | camelCase | `var loadingScreen = ...` |
| Generic type | Single capital or descriptive | `T`, `TView`, `TModel` |

### Async Methods

```csharp
// Async methods MUST use Async suffix
public async UniTask BindDataAsync() { }
public AsyncOperationHandle<T> LoadAssetAsync<T>() { }

// Exception: overriding framework methods
public override async UniTask BindData() { }  // BaseScreenPresenter defines without Async
```

## File Organization

### One Class Per File

```csharp
// CORRECT: GameHomeState.cs
public class GameHomeState : IGameState { }

// REQUIRED: MVP (Model + View + Presenter) in same file
// File name: {Name}ScreenView.cs (e.g., HomeScreenView.cs, LoadingScreenView.cs)
// HomeScreenView.cs
public class HomeScreenModel { }  // Model (optional, if screen needs data)

public class HomeScreenView : BaseView { }  // View

[ScreenInfo(nameof(HomeScreenView))]
public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView> { }  // Presenter
```

### Folder Structure

```
Scripts/
├── Models/           # Data classes
├── Scenes/           # DI scopes, screens by scene
│   ├── Loading/      # LoadingScene components
│   └── Main/         # MainScene components
├── StateMachines/    # State machine implementations
│   └── Game/         # Game FSM
│       ├── Interfaces/
│       └── States/
├── Services/         # Custom services (future)
└── Systems/          # Game systems (future)
```

## Code Patterns

### Dependency Injection

**RULE: Always use constructor injection. Never use `[Inject]` attribute.**

```csharp
// CORRECT: Constructor injection with readonly fields
public class GameHomeState : IGameState
{
    #region Inject

    private readonly IScreenManager screenManager;
    private readonly SignalBus signalBus;

    public GameHomeState(IScreenManager screenManager, SignalBus signalBus)
    {
        this.screenManager = screenManager;
        this.signalBus = signalBus;
    }

    #endregion

    // ... implementation
}

// WRONG: Attribute injection - DO NOT USE
[Inject] private IScreenManager screenManager;  // NEVER - use constructor

// WRONG: Service locator
ServiceLocator.Get<IScreenManager>();            // NEVER
```

### Region Usage

Use `#region Inject` for DI section only:

```csharp
public class MyService
{
    #region Inject

    private readonly IDependency dependency;

    public MyService(IDependency dependency)
    {
        this.dependency = dependency;
    }

    #endregion

    // No other regions - keep code clean
    public void DoWork() { }
}
```

### Async/Await

```csharp
// CORRECT: Use UniTask, not Task
public async UniTask LoadDataAsync()
{
    await userDataManager.LoadUserData();
    await screenManager.OpenScreen<HomeScreenPresenter>();
}

// WRONG: Using System.Threading.Tasks.Task
public async Task LoadDataAsync() { }  // Don't use

// For fire-and-forget (use with caution):
LoadDataAsync().Forget();
```

### State Machine States

```csharp
public class GamePlayState : IGameState, IHaveStateMachine
{
    public IStateMachine StateMachine { get; set; }

    private readonly IScreenManager screenManager;
    private readonly SignalBus signalBus;

    public GamePlayState(IScreenManager screenManager, SignalBus signalBus)
    {
        this.screenManager = screenManager;
        this.signalBus = signalBus;
    }

    public void Enter()
    {
        // Setup when entering state
        signalBus.Subscribe<GameOverSignal>(OnGameOver);
    }

    public void Exit()
    {
        // Cleanup when leaving state
        signalBus.Unsubscribe<GameOverSignal>(OnGameOver);
    }

    private void OnGameOver(GameOverSignal signal)
    {
        StateMachine.TransitionTo<GameResultState>();
    }
}
```

### Screen MVP (Model + View + Presenter in same file)

**File naming:** `{Name}ScreenView.cs` (e.g., `HomeScreenView.cs`, `SettingsScreenView.cs`)

**Two presenter patterns:**
1. `BaseScreenPresenter<TView>` — no model, override `BindData()`
2. `BaseScreenPresenter<TView, TModel>` — with model, override `BindData(TModel)`

```csharp
// HomeScreenView.cs - contains Model, View, and Presenter

// Model (optional - only if screen needs input data)
public class HomeScreenModel
{
    public string PlayerName;
    public int HighScore;
}

// View (MonoBehaviour)
public class HomeScreenView : BaseView
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Button playButton;

    public void SetPlayerName(string name) => playerNameText.text = name;
}

// Presenter (logic)
[ScreenInfo(nameof(HomeScreenView))]
public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView>
{
    #region Inject

    private readonly IGameAssets gameAssets;

    public HomeScreenPresenter(
        SignalBus signalBus,
        ILoggerManager loggerManager,
        IGameAssets gameAssets
    ) : base(signalBus, loggerManager)
    {
        this.gameAssets = gameAssets;
    }

    #endregion

    public override async UniTask BindData()
    {
        // Initialize view data
        View.SetPlayerName("Player 1");
        await UniTask.CompletedTask;
    }

    // Use protected virtual for overridable behavior
    protected virtual string GetWelcomeMessage() => "Welcome!";
}
```

### Popups (special screens)

Use `[PopupInfo]` attribute to mark presenters as popups:

```csharp
// Standard popup (replaces previous screen)
[ScreenInfo(nameof(SettingsPopupView))]
[PopupInfo]
public class SettingsPopupPresenter : BasePopupPresenter<SettingsPopupView> { }

// Overlay popup (stacks on top, doesn't hide previous)
[ScreenInfo(nameof(NotificationPopupView))]
[PopupInfo(IsOverlay = true)]
public class NotificationPopupPresenter : BasePopupPresenter<NotificationPopupView> { }
```

**Popup behavior:**
- Standard popup: previous screen hides when popup opens
- Overlay popup: previous screen remains visible (rendered to `CurrentOverlayRoot`)
- Both use blur background signal (`PopupBlurBgShowedSignal`)

### Signals/Events

```csharp
// Signal definition - ALWAYS use class (not struct)
public class LevelCompletedSignal
{
    public int Level;
    public int Score;
    public float Time;
}

// Declaration in LifetimeScope
builder.DeclareSignal<LevelCompletedSignal>();

// Publishing
signalBus.Fire(new LevelCompletedSignal
{
    Level = currentLevel,
    Score = totalScore,
    Time = elapsedTime
});

// Subscribing
signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);

// ALWAYS unsubscribe in cleanup
signalBus.Unsubscribe<LevelCompletedSignal>(OnLevelCompleted);
```

## Access Modifiers

```csharp
// Default to most restrictive, then widen as needed
private readonly IService service;           // Fields: private
public string Name { get; private set; }     // Properties: public get, private set
protected virtual void OnEnter() { }         // Protected for inheritance
internal void HelperMethod() { }             // Internal for assembly-only
public void DoAction() { }                   // Public for API
```

## Documentation

### When to Document

```csharp
// DON'T: Obvious code
/// <summary>Gets the screen manager.</summary>
public IScreenManager ScreenManager { get; }  // Name is self-explanatory

// DO: Non-obvious behavior
/// <summary>
/// Transitions to the next state. Current state's Exit() is called
/// before the new state's Enter().
/// </summary>
/// <typeparam name="T">Target state type (must implement IGameState)</typeparam>
public void TransitionTo<T>() where T : IGameState { }

// DO: Public API methods
/// <summary>
/// Loads a scene additively via Addressables.
/// </summary>
/// <param name="sceneName">Addressable key for the scene</param>
/// <returns>Handle to track loading progress</returns>
public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string sceneName) { }
```

## Error Handling

```csharp
// Log and handle gracefully
try
{
    await gameAssets.LoadAssetAsync<Sprite>(iconKey);
}
catch (Exception ex)
{
    logger.LogError($"Failed to load icon: {iconKey}", ex);
    // Use fallback or default
}

// For critical failures, throw with context
if (state == null)
{
    throw new InvalidOperationException($"State {typeof(T).Name} not registered");
}
```

## Unity-Specific

### MonoBehaviour Fields

```csharp
public class MyView : BaseView
{
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI scoreText;

    // Don't use public fields for serialization
    // [SerializeField] is explicit and prevents accidental modification
}
```

### Scene References

```csharp
// Use string constants or Addressable keys
protected virtual string NextSceneName => "1.MainScene";

// Or use ScriptableObject for configuration
[CreateAssetMenu]
public class SceneConfig : ScriptableObject
{
    public string LoadingScene = "0.LoadingScene";
    public string MainScene = "1.MainScene";
}
```

## Prohibited Patterns

| Pattern | Reason | Alternative |
|---------|--------|-------------|
| `[Inject]` attribute | Implicit, hard to trace | Constructor injection |
| `FindObjectOfType<T>()` | Slow, brittle | DI injection |
| `GameObject.Find()` | Slow, brittle | Serialize reference |
| Singleton via `static Instance` | Hard to test | DI with Lifetime.Singleton |
| `async void` methods | Unhandled exceptions | `async UniTask` |
| Magic strings/numbers | Unmaintainable | Constants or config |
| Deep inheritance | Rigid | Composition |
| `struct` for signals | Inconsistent behavior | `class` for signals |
| Separate MVP files | Hard to maintain | All MVP in `{Name}ScreenView.cs` |
