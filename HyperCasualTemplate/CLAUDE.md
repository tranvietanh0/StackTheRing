# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (6000.3.10f1) hyper-casual game template using VContainer for dependency injection, UniTask for async operations, and MessagePipe for pub/sub messaging.

## Build & Development

Open in Unity Hub with Unity 6000.3.10f1. The project uses:
- **Package Manager**: OpenUPM registry for VContainer, UniTask, MessagePipe
- **Addressables**: Asset loading via `IGameAssets.LoadSceneAsync()` / `LoadAssetAsync()`
- **NuGet for Unity**: Manages .NET packages (Newtonsoft.Json, etc.)

## Architecture

### Assembly Structure
```
HyperCasualGame.Scripts          # Main game code (Assets/Scripts/)
├── References: GameFoundationCore.*, UITemplate.Scripts, UniT.*
│
GameFoundationCore.Scripts       # Core framework (Assets/Submodules/GameFoundationCore/)
├── .AssetLibrary                # Addressables wrapper (GameAssets)
├── .BlueprintFlow               # CSV config/data parsing system
├── .DI                          # DI interfaces (IInitializable, ITickable, etc.)
├── .Models                      # Base data models
├── .Signals                     # MessagePipe-based SignalBus
├── .UIModule                    # Screen management, MVP framework
└── .Utilities                   # ObjectPool, SoundManager, UserData services
│
UITemplate.Scripts               # UI template system (Assets/Submodules/UITemplate/)
└── StateMachine                 # State machine base implementation
```

### Dependency Injection (VContainer)

**Scope Hierarchy:**
- `GameLifetimeScope` (root) → registers GameFoundation + UITemplate core services
- `SceneScope` subclasses → per-scene registrations (e.g., `MainSceneScope`)

**Registration pattern:**
```csharp
public class MainSceneScope : SceneScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameStateMachine>(Lifetime.Singleton).AsInterfacesAndSelf();
    }
}
```

**Auto-registered services** (via `RegisterGameFoundation()`):
- `SignalBus`, `IGameAssets`, `IScreenManager`, `ObjectPoolManager`, `AudioService`, `IHandleUserDataServices`

### State Machine Pattern

Game states implement `IGameState` and are auto-discovered:
```csharp
// States register automatically via reflection
builder.WithParameter(container => typeof(IGameState).GetDerivedTypes()
    .Select(type => (IGameState)container.Instantiate(type)).ToList())
```

States can hold reference to their parent machine via `IHaveStateMachine`:
```csharp
public class GameHomeState : IGameState, IHaveStateMachine
{
    public IStateMachine StateMachine { get; set; }
    public void Enter() { }
    public void Exit() { }
}
```

Transition: `stateMachine.TransitionTo<GameHomeState>()`

### Screen/UI System (MVP Pattern)

**View**: MonoBehaviour inheriting `BaseView` (handles animations, lifecycle)
**Presenter**: Handles logic, inherits `BaseScreenPresenter<TView>`

```csharp
public class LoadingScreenView : BaseView { }

[ScreenInfo(nameof(LoadingScreenView))]
public class LoadingScreenPresenter : BaseScreenPresenter<LoadingScreenView>
{
    public override async UniTask BindData() { /* load data */ }
}
```

Open screens via `IScreenManager`:
```csharp
await screenManager.OpenScreen<LoadingScreenPresenter>();
await screenManager.OpenScreen<HomeScreenPresenter, HomeModel>(model);
```

### Signals (Pub/Sub)

Declare signals in LifetimeScope:
```csharp
builder.DeclareSignal<UserDataLoadedSignal>();
```

Usage:
```csharp
signalBus.Subscribe<UserDataLoadedSignal>(OnDataLoaded);
signalBus.Fire(new UserDataLoadedSignal());
signalBus.Unsubscribe<UserDataLoadedSignal>(OnDataLoaded);
```

### Blueprint System

CSV-based config reading with attribute-driven parsing:
```csharp
[BlueprintReader("ConfigName")]
public class MyConfigReader : GenericBlueprintReaderByRow<MyConfigModel> { }
```

## Scene Structure

- `0.LoadingScene` - Entry point, loads user data, transitions to main scene
- `1.MainScene` - Main game scene with GameStateMachine

## Key Conventions

- Async methods use `UniTask`, not `Task`
- All DI-aware classes use constructor injection
- Screen presenters are instantiated via `IScreenManager`, not directly
- Use `IInitializable.Initialize()` for post-injection setup (called after DI container resolves)
- Use `ITickable.Tick()` for update loops (managed by VContainer)
- States auto-register by implementing marker interfaces (`IGameState`)
