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
IScreenManager
├── OpenScreen<TPresenter>()
├── OpenScreen<TPresenter, TModel>(model)
├── CloseScreen<TPresenter>()
│
TPresenter : BaseScreenPresenter<TView>
├── BindData() — async initialization
├── View — reference to MonoBehaviour
└── SignalBus — for communication
```

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
// Subscribe
signalBus.Subscribe<UserDataLoadedSignal>(OnDataLoaded);

// Publish
signalBus.Fire(new UserDataLoadedSignal { Data = userData });

// Cleanup
signalBus.Unsubscribe<UserDataLoadedSignal>(OnDataLoaded);
```

### 5. Asset Loading (Addressables)

**Interface:** `IGameAssets`

```csharp
// Scene loading
AsyncOperationHandle<SceneInstance> LoadSceneAsync(string sceneName);

// Asset loading
AsyncOperationHandle<T> LoadAssetAsync<T>(string key);
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
