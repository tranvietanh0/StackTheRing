# Codebase Summary — Stack The Ring

## Project Overview

**Stack The Ring** is a Unity 6 (6000.3.10f1) hyper-casual mobile game built on a modular framework architecture. The project uses dependency injection, async operations, and event-driven communication patterns.

## Tech Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Engine | Unity | 6000.3.10f1 |
| DI Container | VContainer | 1.16.9 |
| Async | UniTask | 2.5.10 |
| Pub/Sub | MessagePipe | 1.8.1 |
| Asset Loading | Addressables | 2.9.0 |
| JSON | Newtonsoft.Json | 3.2.2 |
| Tweening | DOTween Pro | - |

## Directory Structure

```
StackTheRing/
├── UnityStackTheRing/           # Unity project root
│   ├── Assets/
│   │   ├── Scripts/             # Game-specific code
│   │   │   ├── Models/          # Data models
│   │   │   ├── Scenes/          # DI scopes & screens
│   │   │   └── StateMachines/   # Game state management
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
├── GameFoundationCore.Scripts       # Core services (DI, signals, assets)
├── GameFoundationCore.UIModule      # Screen management, MVP
├── GameFoundationCore.DI            # DI interfaces
├── GameFoundationCore.Signals       # SignalBus
├── GameFoundationCore.AssetLibrary  # Addressables wrapper
├── GameFoundationCore.Models        # Base data models
├── UITemplate.Scripts               # StateMachine, UserData
├── UniT.Logging                     # Logging abstractions
└── UniT.Extensions                  # Utility helpers
```

## Source Files

| Location | Files | Purpose |
|----------|-------|---------|
| `Assets/Scripts/` | 9 | Game-specific logic |
| `Assets/Submodules/GameFoundationCore/` | ~200+ | Core framework |
| `Assets/Submodules/UITemplate/` | ~50+ | UI system |
| `Assets/Scenes/` | 2 | Loading + Main |

## Key Entry Points

1. **`0.LoadingScene`** — App entry, loads user data, transitions to main
2. **`GameLifetimeScope`** — Root DI container setup
3. **`LoadingScreenPresenter`** — Initial screen, loads data then scene
4. **`MainSceneScope`** — Per-scene DI registrations
5. **`GameStateMachine`** — Game flow orchestrator

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
                └── GameStateMachine.Initialize()
                    └── TransitionTo<GameHomeState>()
```

## Lines of Code (Game-Specific)

| File | Lines | Description |
|------|-------|-------------|
| GameLifetimeScope.cs | 18 | Root DI setup |
| GameStateMachine.cs | 31 | State machine orchestrator |
| MainSceneScope.cs | 18 | Scene DI setup |
| LoadingScreenView.cs | 51 | Loading screen MVP |
| GameHomeState.cs | 27 | Home state logic |
| UserLocalData.cs | 12 | Local save data model |
| IGameState.cs | 14 | State interface |
| IHaveStateMachine.cs | ~10 | StateMachine accessor |

**Total game-specific code: ~180 lines** (template ready for game logic)
