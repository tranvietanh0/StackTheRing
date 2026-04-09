# System Architecture — Stack The Ring

## Architectural Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Unity Application Layer                      │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │ GameStates   │  │ Screens      │  │ Game Systems         │  │
│  │ (FSM)        │  │ (MVP)        │  │ (Future)             │  │
│  └──────┬───────┘  └──────┬───────┘  └──────────────────────┘  │
├─────────┼─────────────────┼─────────────────────────────────────┤
│         │     Service Layer (DI Managed)                        │
│  ┌──────┴─────────────────┴───────────────────────────────────┐ │
│  │  SignalBus │ ScreenManager │ GameAssets │ UserDataManager  │ │
│  │  AudioService │ ObjectPoolManager │ LoggerManager          │ │
│  └────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                  VContainer (Dependency Injection)              │
│  ┌─────────────────┐  ┌─────────────────┐                      │
│  │ GameLifetime    │──│ SceneScope      │                      │
│  │ Scope (Root)    │  │ (Per-Scene)     │                      │
│  └─────────────────┘  └─────────────────┘                      │
├─────────────────────────────────────────────────────────────────┤
│                    Unity Engine & Packages                      │
│  Addressables │ InputSystem │ Timeline │ DOTween               │
└─────────────────────────────────────────────────────────────────┘
```

## Core Patterns

### 1. Dependency Injection (VContainer)

**Scope Hierarchy:**
```
GameLifetimeScope (Root, DontDestroyOnLoad)
├── Core Services (Singleton across app)
│   ├── SignalBus
│   ├── IGameAssets
│   ├── IScreenManager
│   ├── ObjectPoolManager
│   ├── AudioService
│   └── IHandleUserDataServices
│
└── SceneScope (Per-Scene, child of root)
    └── Scene-specific services
        └── GameStateMachine (in MainScene)
```

**Registration Example:**
```csharp
// Root scope (GameLifetimeScope.cs)
builder.RegisterGameFoundation(this.transform);
builder.RegisterUITemplate();

// Scene scope (MainSceneScope.cs)
builder.Register<GameStateMachine>(Lifetime.Singleton)
    .WithParameter(container => /* auto-discover states */)
    .AsInterfacesAndSelf();
```

### 2. State Machine (FSM)

**Pattern:** Finite State Machine with auto-discovery

```
GameStateMachine (Controller)
├── List<IGameState> states (injected via reflection)
├── TransitionTo<T>() — switches current state
│
└── IGameState implementations
    ├── GameHomeState
    ├── GamePlayState (future)
    └── GameResultState (future)
```

**State Lifecycle:**
```
Enter() → [active] → Exit() → [next state].Enter()
```

**Bi-directional Reference:**
States implementing `IHaveStateMachine` receive reference back to their parent machine for self-transitions.

### 3. MVP Screen Pattern

**Components (all in ONE file: `{Name}ScreenView.cs`):**
- **Model**: Data class (optional, passed to presenter)
- **View**: `MonoBehaviour` inheriting `BaseView`
- **Presenter**: Logic class inheriting `BaseScreenPresenter<TView>`

```
IScreenManager (ScreenManager)
├── OpenScreen<TPresenter>()           — load/cache, BindData(), Open()
├── OpenScreen<TPresenter, TModel>()   — with model data
├── GetScreen<TPresenter>()            — get or lazy-load
├── CloseCurrentScreen()               — close top screen
├── CloseAllScreen()                   — close all
├── CleanUpAllScreen()                 — dispose all
│
TPresenter : BaseScreenPresenter<TView>
├── BindData()            — abstract, override to load data
├── OpenViewAsync()       — calls BindData() then View.Open()
├── CloseViewAsync()      — View.Close() + fires ScreenCloseSignal
├── HideView()            — hide without destroy
├── View                  — reference to MonoBehaviour
├── SignalBus             — protected, for communication
├── Logger                — protected, for logging
├── IsClosePrevious       — if true, closes previous screen on open
└── ScreenStatus          — Opened, Closed, Hide, Destroyed
```

**Screen Lifecycle:**
```
GetScreen<T>() → VContainer.Instantiate(T) → LoadAsset(View prefab) → SetView()
                                                                        ↓
OpenViewAsync() → BindData() → ScreenShowSignal → View.Open()
                                                        ↓
                                              [Active Screen]
                                                        ↓
CloseViewAsync() → View.Close() → ScreenCloseSignal → SetParent(HiddenRoot) → Dispose()
```

**Screen Caching:** ScreenManager caches loaded presenters in `typeToLoadedScreenPresenter`. Second `OpenScreen<T>()` call reuses cached instance.

**File Structure Example (`HomeScreenView.cs`):**
```csharp
// Model (optional)
public class HomeScreenModel { public int HighScore; }

// View
public class HomeScreenView : BaseView { }

// Presenter
[ScreenInfo(nameof(HomeScreenView))]
public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView> { }
```

### 4. Pub/Sub Messaging (MessagePipe + SignalBus)

**SignalBus** wraps MessagePipe's `IPublisher<T>` / `ISubscriber<T>` with automatic subscription tracking and cleanup. Implements `ILateDisposable` for container disposal.

**Signal Definition (always use `class`, not `struct`):**
```csharp
public class UserDataLoadedSignal
{
    public UserLocalData Data;
}
```

**Signal Declaration (in LifetimeScope):**
```csharp
builder.DeclareSignal<UserDataLoadedSignal>();
```

**Usage Pattern:**
```csharp
// Subscribe (with or without signal parameter)
signalBus.Subscribe<UserDataLoadedSignal>(OnDataLoaded);
signalBus.Subscribe<UserDataLoadedSignal>(() => Debug.Log("Loaded"));

// Try-variants (don't throw if already subscribed/unsubscribed)
signalBus.TrySubscribe<UserDataLoadedSignal>(OnDataLoaded);
signalBus.TryUnsubscribe<UserDataLoadedSignal>(OnDataLoaded);

// Publish
signalBus.Fire(new UserDataLoadedSignal { Data = userData });
signalBus.Fire<UserDataLoadedSignal>();  // default(T)

// Cleanup (REQUIRED in Exit() or Dispose())
signalBus.Unsubscribe<UserDataLoadedSignal>(OnDataLoaded);
```

**Internal Signals (framework-defined):**
- `ScreenShowSignal` — fired when screen opens
- `ScreenCloseSignal` — fired when screen closes
- `ScreenSelfDestroyedSignal` — fired when view destroyed
- `StartLoadingNewSceneSignal` — triggers screen cleanup
- `UserDataLoadedSignal` — fired after user data load

### 5. Asset Loading (Addressables)

**Interface:** `IGameAssets` (implemented by `GameAssets`)

**Features:**
- **Caching:** Assets cached in `loadedAssets` dictionary, prevents duplicate loads
- **Auto-unload:** Assets track their scene, auto-release when scene unloads
- **Load tracking:** `loadingAssets` prevents concurrent loads of same asset

```csharp
// Scene loading
AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode mode = Single);
AsyncOperationHandle<SceneInstance> UnloadSceneAsync(object key);

// Asset loading (with auto-unload option)
AsyncOperationHandle<T> LoadAssetAsync<T>(object key, bool isAutoUnload = true);
AsyncOperationHandle<T> LoadAssetAsync<T>(AssetReference assetRef, bool isAutoUnload = true);

// Preloading multiple assets
List<AsyncOperationHandle<T>> PreloadAsync<T>(string targetScene, params object[] keys);
UniTask<List<AsyncOperationHandle<T>>> LoadAssetsByLabelAsync<T>(string label);

// Instantiation
UniTask<GameObject> InstantiateAsync(object key, Vector3 pos, Quaternion rot, Transform parent);
bool DestroyGameObject(GameObject go);  // releases Addressable instance

// Manual release
void ReleaseAsset(object key);
void UnloadUnusedAssets(string sceneName);  // release all scene's auto-unload assets
```

## Data Flow

### App Startup Flow

```
1. Unity loads 0.LoadingScene
2. GameLifetimeScope.Configure() runs
   └── Registers core services
3. LoadingSceneScope.Configure() runs
4. LoadingScreenPresenter auto-opens
5. LoadingScreenPresenter.BindData():
   ├── await userDataManager.LoadUserData()
   └── await gameAssets.LoadSceneAsync("1.MainScene")
6. Unity loads 1.MainScene
7. MainSceneScope.Configure() runs
   └── Registers GameStateMachine
8. VContainer calls IInitializable.Initialize() on GameStateMachine
9. GameStateMachine.Initialize():
   └── TransitionTo<GameHomeState>()
```

### State Transition Flow

```
Current State                    Next State
┌──────────────────┐            ┌──────────────────┐
│ GameHomeState    │            │ GamePlayState    │
│                  │            │                  │
│ User taps Play   │──────────► │ Enter()          │
│ Exit()           │            │ - Setup game     │
└──────────────────┘            └──────────────────┘
```

## DI Lifecycle Interfaces

| Interface | When Called | Purpose |
|-----------|-------------|---------|
| `IInitializable` | After container built | Post-injection setup |
| `IPostInitializable` | After all IInitializable | Late initialization |
| `IStartable` | MonoBehaviour.Start timing | Unity lifecycle hook |
| `ITickable` | Every Update frame | Game loop logic |
| `ILateTickable` | Every LateUpdate frame | Post-update logic |
| `IFixedTickable` | Every FixedUpdate frame | Physics timing |
| `IDisposable` | Container disposed | Cleanup |

## Framework Internals Reference

| Component | File Location | Key Methods |
|-----------|---------------|-------------|
| DI Registration | `GameFoundationCore/Scripts/GameFoundationVContainer.cs` | `RegisterGameFoundation()` |
| SignalBus | `GameFoundationCore/Scripts/Signals/SignalBus.cs` | `Fire()`, `Subscribe()`, `Unsubscribe()` |
| ScreenManager | `GameFoundationCore/Scripts/UIModule/ScreenFlow/Manager/ScreenManager.cs` | `OpenScreen()`, `GetScreen()`, `CloseCurrentScreen()` |
| GameAssets | `GameFoundationCore/Scripts/AssetLibrary/GameAssets.cs` | `LoadSceneAsync()`, `LoadAssetAsync()` |
| BasePresenter | `GameFoundationCore/Scripts/UIModule/ScreenFlow/BaseScreen/Presenter/BaseScreenPresenter.cs` | `BindData()`, `OpenViewAsync()` |
| StateMachine | `UITemplate/Scripts/Others/StateMachine/Controller/StateMachine.cs` | `TransitionTo<T>()` |

## Extension Points

### Adding a New Game State

1. Create class implementing `IGameState`:
```csharp
public class GamePlayState : IGameState, IHaveStateMachine
{
    public IStateMachine StateMachine { get; set; }
    public void Enter() { /* ... */ }
    public void Exit() { /* ... */ }
}
```

2. Auto-discovered via reflection — no manual registration needed.

### Adding a New Screen

1. Create single file `{Name}ScreenView.cs` with Model + View + Presenter:
```csharp
// HomeScreenView.cs

// Model (optional)
public class HomeScreenModel
{
    public string PlayerName;
}

// View
public class HomeScreenView : BaseView { }

// Presenter (constructor injection only - no [Inject] attribute)
[ScreenInfo(nameof(HomeScreenView))]
public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView>
{
    private readonly IGameAssets gameAssets;

    public HomeScreenPresenter(
        SignalBus signalBus,
        ILoggerManager loggerManager,
        IGameAssets gameAssets
    ) : base(signalBus, loggerManager)
    {
        this.gameAssets = gameAssets;
    }

    public override async UniTask BindData() { /* ... */ }
}
```

2. Create prefab named `HomeScreenView` in Addressables.

3. Open via: `screenManager.OpenScreen<HomeScreenPresenter>()`

### Adding a New Signal

1. Define signal as **class** (not struct):
```csharp
public class GameStartedSignal
{
    public int Level;
    public GameMode Mode;
}
```

2. Declare in LifetimeScope:
```csharp
builder.DeclareSignal<GameStartedSignal>();
```

3. Use via `SignalBus`.
