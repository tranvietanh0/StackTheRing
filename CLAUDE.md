# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Stack The Ring** — Unity 6 (6000.3.10f1) hyper-casual mobile game using:
- **VContainer** (1.16.9) — Dependency injection
- **UniTask** (2.5.10) — Async/await
- **MessagePipe** (1.8.1) — Pub/sub messaging
- **Addressables** (2.9.0) — Asset management
- **DOTween Pro** — Animations

## Project Structure

```
StackTheRing/
├── UnityStackTheRing/           # Unity project (open in Unity Hub)
│   ├── Assets/Scripts/          # Game code (HyperCasualGame.Scripts)
│   └── Assets/Submodules/       # Core frameworks (git submodules)
├── docs/                        # Project documentation
└── CLAUDE.md                    # This file
```

## Build & Development

1. Open `UnityStackTheRing/` in Unity Hub with Unity 6000.3.10f1
2. Git submodules should auto-initialize via Setup.bat/Setup.sh
3. Package Manager uses OpenUPM registry (auto-configured)

## Key Architectural Patterns

### Dependency Injection (VContainer)

```csharp
// Root scope (entire app lifetime)
public class GameLifetimeScope : LifetimeScope { }

// Scene scope (per-scene services)
public class MainSceneScope : SceneScope { }

// Constructor injection (REQUIRED - no [Inject] attribute)
public class MyService(IScreenManager screenManager, SignalBus signalBus) { }
```

### State Machine (GameStateMachine)

States auto-discovered via reflection by implementing `IGameState`:

```csharp
public class GamePlayState : IGameState, IHaveStateMachine
{
    public IStateMachine StateMachine { get; set; }
    public void Enter() { /* setup */ }
    public void Exit() { /* cleanup */ }
}
```

### MVP Screen System

**All MVP in ONE file:** `{Name}ScreenView.cs`

```csharp
// HomeScreenView.cs - Model + View + Presenter together

// Model (optional)
public class HomeScreenModel { public int HighScore; }

// View (MonoBehaviour)
public class HomeScreenView : BaseView { }

// Presenter (constructor injection only - no [Inject] attribute)
[ScreenInfo(nameof(HomeScreenView))]
public class HomeScreenPresenter : BaseScreenPresenter<HomeScreenView>
{
    public HomeScreenPresenter(SignalBus signalBus, ILoggerManager logger)
        : base(signalBus, logger) { }

    public override async UniTask BindData() { }
}

// Open screen
await screenManager.OpenScreen<HomeScreenPresenter>();
```

### Pub/Sub (SignalBus)

**Signals must be `class` (not struct):**

```csharp
// Define signal as class
public class LevelCompletedSignal
{
    public int Level;
    public int Score;
}

// Declare in LifetimeScope
builder.DeclareSignal<LevelCompletedSignal>();

// Usage
signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
signalBus.Fire(new LevelCompletedSignal { Level = 1 });
signalBus.Unsubscribe<LevelCompletedSignal>(OnLevelCompleted);
```

## Code Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespace | PascalCase | `HyperCasualGame.Scripts.StateMachines` |
| Class | PascalCase | `GameStateMachine` |
| Interface | I-prefix | `IGameState` |
| Private field | camelCase | `private readonly IScreenManager screenManager;` |
| Async method | Use UniTask | `public async UniTask LoadAsync()` |

## Common Commands

```csharp
// State transition
stateMachine.TransitionTo<GamePlayState>();

// Load scene
await gameAssets.LoadSceneAsync("1.MainScene");

// Open screen with data
await screenManager.OpenScreen<LevelSelectPresenter, LevelSelectModel>(model);
```

## File Locations

| What | Where |
|------|-------|
| Game code | `UnityStackTheRing/Assets/Scripts/` |
| Scenes | `UnityStackTheRing/Assets/Scenes/` |
| Core framework | `UnityStackTheRing/Assets/Submodules/GameFoundationCore/` |
| UI framework | `UnityStackTheRing/Assets/Submodules/UITemplate/` |
| Documentation | `docs/` |

## Documentation

- `docs/codebase-summary.md` — Project overview
- `docs/system-architecture.md` — Architecture deep-dive
- `docs/code-standards.md` — Coding conventions
- `docs/project-changelog.md` — Version history
- `docs/development-roadmap.md` — Future plans

## Prohibited Patterns

- `[Inject]` attribute — Use constructor injection only
- `struct` for signals — Use `class` for signals
- Separate MVP files — All MVP in `{Name}ScreenView.cs`
- `FindObjectOfType<T>()` — Use DI
- `GameObject.Find()` — Serialize reference
- Static singletons — Use VContainer `Lifetime.Singleton`
- `async void` — Use `async UniTask`
- `Task` — Use `UniTask`
