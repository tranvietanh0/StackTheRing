# Codebase Summary — Stack The Ring

## Project Overview

**Stack The Ring** is a Unity 6 (6000.3.10f1) hyper-casual mobile game featuring a conveyor-based ball sorting mechanic. Balls move along a spline-based conveyor, and players tap color collectors to attract matching balls into stacking slots. Clear stacks when full, avoid deadlock conditions.

## Tech Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Engine | Unity | 6000.3.10f1 |
| DI Container | VContainer | 1.16.9 |
| Async | UniTask | 2.5.10 |
| Pub/Sub | MessagePipe | 1.8.1 |
| Asset Loading | Addressables | 2.9.0 |
| Spline | Dreamteck Splines | - |
| Tweening | DOTween Pro | - |
| JSON | Newtonsoft.Json | 3.2.2 |

## Directory Structure

```
StackTheRing/
├── UnityStackTheRing/           # Unity project root
│   ├── Assets/
│   │   ├── Scripts/             # Game-specific code (31 files)
│   │   │   ├── Attraction/      # Ball attraction system
│   │   │   ├── Conveyor/        # Spline-based conveyor belt
│   │   │   ├── Core/            # Constants, enums, GameManager
│   │   │   ├── Editor/          # Unity Editor tools
│   │   │   ├── Level/           # LevelData, LevelManager
│   │   │   ├── Models/          # Data models (UserLocalData)
│   │   │   ├── Ring/            # Ball & RowBall components
│   │   │   ├── Scenes/          # DI scopes & screens
│   │   │   ├── Signals/         # Game event signals
│   │   │   ├── Slot/            # Stacking slots & collectors
│   │   │   └── StateMachines/   # Game state management
│   │   ├── Data/                # ScriptableObject configs
│   │   ├── Prefabs/             # Ball, RowBall prefabs
│   │   ├── Scenes/              # Unity scene files
│   │   ├── Plugins/             # DOTween, etc.
│   │   └── Submodules/          # Git submodules (core frameworks)
│   │       ├── GameFoundationCore/  # Core framework
│   │       ├── UITemplate/          # UI MVP framework
│   │       ├── Extensions/          # Utility extensions
│   │       └── Logging/             # Logging system
│   ├── Packages/
│   └── ProjectSettings/
└── docs/                        # Documentation
```

## Assembly Structure

```
HyperCasualGame.Scripts              # Main game assembly
├── VContainer                       # DI container
├── GameFoundationCore.Scripts       # Core services (DI, signals, assets)
├── GameFoundationCore.UIModule      # Screen management, MVP
├── GameFoundationCore.DI            # DI interfaces (IInitializable, ITickable, etc.)
├── GameFoundationCore.Signals       # SignalBus (MessagePipe wrapper)
├── GameFoundationCore.AssetLibrary  # Addressables wrapper (GameAssets)
├── GameFoundationCore.Models        # Base data models
├── UITemplate.Scripts               # StateMachine, UserData
├── UniT.Logging                     # Logging abstractions
├── UniT.Extensions                  # Utility helpers
├── UniTask                          # Async/await framework
└── Unity.Addressables               # Asset loading
```

## Source Files

| Location | Files | Purpose |
|----------|-------|---------|
| `Assets/Scripts/` | 31 | Game-specific logic |
| `Assets/Scripts/Core/` | 4 | ColorType, RingState, GameConstants, GameManager |
| `Assets/Scripts/Conveyor/` | 4 | Spline conveyor system |
| `Assets/Scripts/Ring/` | 2 | Ball, RowBall components |
| `Assets/Scripts/Slot/` | 4 | Stacking slots & collectors |
| `Assets/Scripts/Attraction/` | 2 | Ball attraction mechanics |
| `Assets/Scripts/Level/` | 2 | Level data & management |
| `Assets/Scripts/Signals/` | 1 | 15 game signals |
| `Assets/Scripts/StateMachines/` | 6 | FSM states |
| `Assets/Submodules/GameFoundationCore/` | ~200+ | Core framework |
| `Assets/Submodules/UITemplate/` | ~50+ | UI system |
| `Assets/Scenes/` | 2 | Loading + Main |

## Key Entry Points

1. **`0.LoadingScene`** — App entry, loads user data, transitions to main
2. **`GameLifetimeScope`** — Root DI container, calls `RegisterGameFoundation()` + `RegisterUITemplate()`
3. **`LoadingScreenPresenter`** — Initial screen, loads `UserLocalData` then `1.MainScene`
4. **`MainSceneScope`** — Per-scene DI, registers `GameStateMachine`, `LevelManager`, `GameManager`
5. **`GameManager`** — Central orchestrator, initializes all game systems
6. **`GameStateMachine`** — Game flow: `GameHomeState` → `GamePlayState` → `GameWinState`/`GameLoseState`

## Services Registered (via RegisterGameFoundation)

| Service | Lifetime | Interface | Purpose |
|---------|----------|-----------|---------|
| SignalBus | Scoped | - | Pub/sub messaging (MessagePipe wrapper) |
| GameAssets | Singleton | IGameAssets | Addressables wrapper with caching |
| ScreenManager | Scoped | IScreenManager | MVP screen management |
| ObjectPoolManager | Singleton | - | Object pooling |
| AudioService | Singleton | IAudioService | Sound management |
| HandleLocalUserDataServices | Singleton | IHandleUserDataServices | User data persistence |
| LoggerManager | - | ILoggerManager | Logging |

## Dependencies (Git Submodules)

| Submodule | Repository | Purpose |
|-----------|-----------|---------|
| GameFoundationCore | tranvietanh0/GameFoundationCore | Core framework services |
| UITemplate | tranvietanh0/UITemplate | UI MVP pattern, StateMachine |
| Extensions | tranvietanh0/Unity.Extensions | Utility extensions |
| Logging | tranvietanh0/Unity.Logging | Logging abstractions |

## External Packages (via OpenUPM)

- `jp.hadashikick.vcontainer` — DI container
- `com.cysharp.unitask` — Async/await for Unity
- `com.cysharp.messagepipe` — High-performance pub/sub
- `com.unity.addressables` — Async asset loading

## Scene Flow

```
0.LoadingScene
    └── LoadingScreenPresenter.BindData()
        ├── userDataManager.LoadUserData()
        └── gameAssets.LoadSceneAsync("1.MainScene")
            └── 1.MainScene
                └── GameManager.Initialize()
                    ├── InitializeSystems()
                    │   ├── conveyorController.Initialize()
                    │   ├── slotManager.Initialize()
                    │   ├── collectorPanel.Initialize()
                    │   └── attractionController.Initialize()
                    └── StartGame()
                        ├── levelManager.LoadLevel(1)
                        ├── SetupLevel(levelData)
                        └── stateMachine.TransitionTo<GamePlayState>()
```

## Game Loop

```
GamePlayState.Enter()
    ├── Subscribe signals (AllRingsCleared, RowBallCompletedLoop, BallAttracted)
    ├── conveyor.StartConveyor()
    └── attractionController.SetEnabled(true)

Tick() [every frame]
    └── CheckLoseCondition()
        └── If no possible moves → TransitionTo<GameLoseState>()

Win Condition: AllRingsClearedSignal → TransitionTo<GameWinState>()
Lose Condition: AllSlotsOccupied + NoPossibleMoves → TransitionTo<GameLoseState>()
```

## Lines of Code (Game-Specific)

| File | Lines | Description |
|------|-------|-------------|
| **Core** | | |
| GameManager.cs | 157 | Central game orchestrator |
| GameConstants.cs | 90 | Game constants & color configs |
| ColorType.cs | 11 | Color enum (Red, Yellow, Green, Blue) |
| **Conveyor** | | |
| ConveyorController.cs | 371 | Spline-based conveyor management |
| ConveyorPath.cs | ~50 | Path data wrapper |
| PathFollower.cs | ~100 | Spline following component |
| **Ring** | | |
| Ball.cs | 92 | Individual ball component |
| RowBall.cs | ~150 | Row of 5 balls container |
| **Slot** | | |
| SlotManager.cs | 222 | Manages 4 stacking slots |
| Slot.cs | ~120 | Individual slot logic |
| ColorCollector.cs | ~80 | Tap-to-place collector |
| CollectorPanel.cs | ~100 | Collector UI panel |
| **Attraction** | | |
| AttractionController.cs | 170 | Ball-to-slot attraction |
| **Level** | | |
| LevelManager.cs | 140 | Level loading & progress |
| LevelData.cs | 53 | ScriptableObject level config |
| **States** | | |
| GamePlayState.cs | 193 | Main gameplay loop |
| GameWinState.cs | ~30 | Win condition handler |
| GameLoseState.cs | ~30 | Lose condition handler |
| **Signals** | | |
| GameSignals.cs | 89 | 15 game event signals |

**Total game-specific code: ~2,200+ lines**

## Framework Quick Reference

| Task | Code Pattern |
|------|--------------|
| Register service | `builder.Register<MyService>(Lifetime.Singleton)` |
| Inject dependency | Constructor parameter (no `[Inject]` attribute) |
| Open screen | `await screenManager.OpenScreen<MyPresenter>()` |
| Fire signal | `signalBus.Fire(new MySignal { ... })` |
| Subscribe signal | `signalBus.Subscribe<MySignal>(OnMySignal)` |
| Load level | `await levelManager.LoadLevel(levelNumber)` |
| Change state | `stateMachine.TransitionTo<GamePlayState>()` |
| Start conveyor | `conveyorController.StartConveyor()` |
| Place collector | `slotManager.TryPlaceCollector(ColorType.Red)` |
| Check win | `signalBus.Fire(new AllRingsClearedSignal())` |
