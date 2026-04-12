# System Architecture — Stack The Ring

## Architectural Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     Unity Application Layer                      │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │ GameStates   │  │ Screens      │  │ GameManager          │  │
│  │ (FSM)        │  │ (MVP)        │  │ (Orchestrator)       │  │
│  └──────┬───────┘  └──────┬───────┘  └──────────┬───────────┘  │
├─────────┼─────────────────┼─────────────────────┼───────────────┤
│         │          Game Systems Layer           │               │
│  ┌──────┴───────────────────────────────────────┴─────────────┐ │
│  │  ConveyorController │ SlotManager │ AttractionController   │ │
│  │  LevelManager │ CollectorPanel │ PathFollower              │ │
│  └────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                Service Layer (DI Managed)                       │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  SignalBus │ ScreenManager │ GameAssets │ UserDataManager  │ │
│  │  AudioService │ ObjectPoolManager │ LoggerManager          │ │
│  └────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│                  VContainer (Dependency Injection)              │
│  ┌─────────────────┐  ┌─────────────────┐                      │
│  │ GameLifetime    │──│ MainSceneScope  │                      │
│  │ Scope (Root)    │  │ (Per-Scene)     │                      │
│  └─────────────────┘  └─────────────────┘                      │
├─────────────────────────────────────────────────────────────────┤
│                    Unity Engine & Packages                      │
│  Addressables │ Dreamteck Splines │ DOTween │ InputSystem      │
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
    ├── GameHomeState      — Initial menu state
    ├── GamePlayState      — Active gameplay (ITickable)
    ├── GameWinState       — Level completed
    └── GameLoseState      — Game over
```

**State Lifecycle:**
```
Enter() → [active, Tick() if ITickable] → Exit() → [next state].Enter()
```

**Bi-directional Reference:**
States implementing `IHaveStateMachine` receive reference back to their parent machine for self-transitions.

### 3. Game Systems

**ConveyorController** — Manages spline-based ball conveyor
```
ConveyorController
├── SplineComputer spline (Dreamteck)
├── ConveyorPath (cached path samples)
├── List<RowBall> activeRowBalls
├── SetupLevel(LevelData) — spawn rows from level config
├── StartConveyor() / StopConveyor()
└── Events: OnRowBallCompletedLoop, OnAllBallsCleared
```

**SlotManager** — Manages 4 stacking slots
```
SlotManager
├── Slot[] slots (4 slots)
├── TryPlaceCollector(ColorType) — assign color to empty slot
├── GetSlotForColor(color) — find slot accepting color
├── CanCollectColor(color) — check if attraction possible
└── Events: OnCollectorPlaced, OnSlotCleared, OnBallStackedInSlot
```

**AttractionController** — Pulls matching balls to slots
```
AttractionController
├── CheckAttraction() — every Update when enabled
├── FindMatchingSlot(Ball) — color-based matching
├── IsInAttractionZone(PathFollower, Slot) — progress-based check
└── AttractBall() — DOTween curved path animation
```

**LevelManager** — Level loading & progression
```
ILevelManager
├── CurrentLevel, HighestUnlockedLevel
├── LoadLevel(int) — Resources or Addressables
├── CompleteLevel() — unlock next, fire LevelWinSignal
├── FailLevel() — fire LevelLoseSignal
└── SaveProgress() — PlayerPrefs persistence
```

### 4. MVP Screen Pattern

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

### 5. Pub/Sub Messaging (MessagePipe + SignalBus)

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

**Framework Signals:**
- `ScreenShowSignal` — fired when screen opens
- `ScreenCloseSignal` — fired when screen closes
- `ScreenSelfDestroyedSignal` — fired when view destroyed
- `StartLoadingNewSceneSignal` — triggers screen cleanup
- `UserDataLoadedSignal` — fired after user data load

**Game Signals (GameSignals.cs):**
| Signal | Fired When | Data |
|--------|-----------|------|
| `CollectorTappedSignal` | Player taps collector | Color |
| `CollectorPlacedSignal` | Collector placed in slot | SlotIndex, Color |
| `BallCollectedSignal` | Ball removed from row | RowId, BallIndex, Color |
| `BallAttractedSignal` | Ball starts attraction | Ball, SlotIndex |
| `BallStackedSignal` | Ball lands in slot | Ball, SlotIndex, CurrentStackCount |
| `StackClearedSignal` | Stack full & cleared | SlotIndex, Color, BallsCleared |
| `RowBallCompletedLoopSignal` | RowBall completes loop | RowBall, LoopCount |
| `AllRingsClearedSignal` | All balls collected | - |
| `LevelStartSignal` | Level loaded | LevelNumber |
| `LevelWinSignal` | Level completed | LevelNumber, Score |
| `LevelLoseSignal` | Game over | LevelNumber |

### 6. Asset Loading (Addressables)

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
   └── Registers GameStateMachine, LevelManager, GameManager
8. VContainer calls IInitializable.Initialize():
   ├── GameManager.Initialize()
   │   ├── InitializeSystems() — wire up controllers
   │   └── StartGame() — load level 1, transition to play
   └── GameStateMachine.Initialize()
```

### Game Loop Flow

```
GamePlayState.Enter()
    ├── Subscribe to signals
    ├── conveyor.StartConveyor()
    └── attractionController.SetEnabled(true)

[Every Frame - Tick()]
    ├── AttractionController.Update()
    │   └── For each RowBall on conveyor
    │       └── For each Ball in row
    │           └── If matching slot in attraction zone → AttractBall()
    └── GamePlayState.CheckLoseCondition()
        └── If AllSlotsOccupied && NoPossibleMoves → GameLoseState

[On Attraction Complete]
    └── slot.AddBall(ball)
        └── If stack full → ClearStack() → StackClearedSignal

[On All Balls Cleared]
    └── AllRingsClearedSignal → levelManager.CompleteLevel() → GameWinState
```

### State Transition Flow

```
┌──────────────┐     LoadLevel()      ┌──────────────┐
│ GameHomeState│────────────────────► │ GamePlayState│
└──────────────┘                      └──────┬───────┘
                                             │
                  AllRingsClearedSignal      │ NoPossibleMoves
                           │                 │
                           ▼                 ▼
                   ┌──────────────┐  ┌──────────────┐
                   │ GameWinState │  │ GameLoseState│
                   └──────────────┘  └──────────────┘
                           │                 │
                           └────────┬────────┘
                                    │ Retry/Next
                                    ▼
                            ┌──────────────┐
                            │ GamePlayState│
                            └──────────────┘
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
public class GamePauseState : IGameState, IHaveStateMachine
{
    public GameStateMachine StateMachine { get; set; }
    public void Enter() { /* pause conveyor, show pause UI */ }
    public void Exit() { /* resume conveyor */ }
}
```

2. Auto-discovered via reflection — no manual registration needed.

3. For frame-by-frame updates, also implement `ITickable`:
```csharp
public class GamePlayState : IGameState, IHaveStateMachine, ITickable
{
    public void Tick() { /* called every Update frame */ }
}
```

### Adding a New Level

1. Create ScriptableObject via Assets → Create → StackTheRing → LevelData
2. Configure:
   - `LevelNumber` — Sequential level ID
   - `ConveyorSpeed` — 0.5 to 3.0
   - `StackLimit` — 4 to 12 balls per stack
   - `Rings[]` — Color + Count pairs
   - `AvailableCollectors[]` — Colors players can use

3. Place in `Resources/Levels/Level_XX` or Addressables with key `Level_XX`

### Adding a New Color

1. Add to `ColorType` enum:
```csharp
public enum ColorType { Red = 0, Yellow = 1, Green = 2, Blue = 3, Purple = 4 }
```

2. Add color mapping in `GameConstants.GetColor()`:
```csharp
ColorType.Purple => new Color(0.6f, 0.2f, 0.8f),
```

3. Create matching Ball material and Collector prefab variant

### Adding a New Signal

1. Define signal as **class** in `GameSignals.cs`:
```csharp
public class ComboAchievedSignal
{
    public int ComboCount;
    public int BonusScore;
}
```

2. Declare in LifetimeScope:
```csharp
builder.DeclareSignal<ComboAchievedSignal>();
```

3. Fire: `signalBus.Fire(new ComboAchievedSignal { ComboCount = 3 });`
