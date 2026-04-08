# Phase 8: Game States

**Effort:** S (1 day)
**Dependencies:** Phase 6, Phase 7
**Blocks:** Phase 9

## Objective

Create game states for gameplay flow: Play → Win.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StateMachines/Game/States/GamePlayState.cs` | CREATE | this phase |
| `Assets/Scripts/StateMachines/Game/States/GameWinState.cs` | CREATE | this phase |
| `Assets/Scripts/StateMachines/Game/States/GameHomeState.cs` | MODIFY | this phase |

## Implementation

### 1. GamePlayState.cs

Active gameplay state — manages game session.

```csharp
namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.StackTheRing.Core;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UITemplate.Scripts.Others.StateMachine.Interface;

    public class GamePlayState : IGameState, IHaveStateMachine
    {
        public IStateMachine StateMachine { get; set; }

        #region Inject

        private readonly IScreenManager screenManager;
        private readonly SignalBus signalBus;
        private readonly StackTheRingController gameController;

        public GamePlayState(
            IScreenManager screenManager,
            SignalBus signalBus,
            StackTheRingController gameController)
        {
            this.screenManager = screenManager;
            this.signalBus = signalBus;
            this.gameController = gameController;
        }

        #endregion

        public void Enter()
        {
            // Subscribe to level complete
            signalBus.Subscribe<LevelCompleteSignal>(OnLevelComplete);

            // Start level
            gameController.StartLevel(levelIndex: 0);

            // Show HUD
            OpenHUD().Forget();
        }

        public void Exit()
        {
            signalBus.Unsubscribe<LevelCompleteSignal>(OnLevelComplete);
        }

        private async UniTaskVoid OpenHUD()
        {
            // TODO: Open GameHUDScreenPresenter when created
            // await screenManager.OpenScreen<GameHUDScreenPresenter>();
            await UniTask.CompletedTask;
        }

        private void OnLevelComplete(LevelCompleteSignal signal)
        {
            StateMachine.TransitionTo<GameWinState>();
        }
    }
}
```

### 2. GameWinState.cs

Level complete state — shows win screen, handles replay.

```csharp
namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.StackTheRing.Core;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UITemplate.Scripts.Others.StateMachine.Interface;

    public class GameWinState : IGameState, IHaveStateMachine
    {
        public IStateMachine StateMachine { get; set; }

        #region Inject

        private readonly IScreenManager screenManager;
        private readonly SignalBus signalBus;
        private readonly StackTheRingController gameController;

        public GameWinState(
            IScreenManager screenManager,
            SignalBus signalBus,
            StackTheRingController gameController)
        {
            this.screenManager = screenManager;
            this.signalBus = signalBus;
            this.gameController = gameController;
        }

        #endregion

        public void Enter()
        {
            // Subscribe to next level request
            signalBus.Subscribe<NextLevelRequestedSignal>(OnNextLevelRequested);

            // Show win screen
            ShowWinScreen().Forget();
        }

        public void Exit()
        {
            signalBus.Unsubscribe<NextLevelRequestedSignal>(OnNextLevelRequested);
        }

        private async UniTaskVoid ShowWinScreen()
        {
            // TODO: Open GameWinScreenPresenter when created
            // await screenManager.OpenScreen<GameWinScreenPresenter>();
            await UniTask.CompletedTask;
        }

        private void OnNextLevelRequested(NextLevelRequestedSignal signal)
        {
            // Reset and restart
            gameController.ResetLevel();
            StateMachine.TransitionTo<GamePlayState>();
        }

        public void RequestReplay()
        {
            signalBus.Fire(new NextLevelRequestedSignal(0));
        }
    }
}
```

### 3. GameHomeState.cs (MODIFY)

Update to transition to GamePlayState.

```csharp
namespace HyperCasualGame.Scripts.StateMachines.Game.States
{
    using GameFoundationCore.Scripts.UIModule.ScreenFlow.Manager;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UITemplate.Scripts.Others.StateMachine.Interface;

    public class GameHomeState : IGameState, IHaveStateMachine
    {
        public IStateMachine StateMachine { get; set; }

        #region Inject

        private readonly IScreenManager screenManager;

        public GameHomeState(IScreenManager screenManager)
        {
            this.screenManager = screenManager;
        }

        #endregion

        public void Enter()
        {
            // For MVP, immediately transition to play state
            // In future: show home screen with Play button
            StateMachine.TransitionTo<GamePlayState>();
        }

        public void Exit()
        {
        }
    }
}
```

## State Flow Diagram

```
GameStateMachine.Initialize()
        │
        ▼
   GameHomeState
        │ (auto-transition for MVP)
        ▼
   GamePlayState ◄──────────┐
        │                   │
        │ LevelCompleteSignal
        ▼                   │
   GameWinState             │
        │                   │
        │ NextLevelRequestedSignal
        └───────────────────┘
```

## Verification

- [ ] All state files compile without errors
- [ ] States implement `IGameState` and `IHaveStateMachine`
- [ ] Constructor injection only (no `[Inject]` attributes)
- [ ] Subscribe in Enter(), Unsubscribe in Exit()
- [ ] State transitions work correctly

## Notes

- GameHomeState auto-transitions to GamePlayState for MVP simplicity
- Add Home screen with Play button in future iteration
- GameWinState.RequestReplay() can be called from Win screen button
- States are auto-discovered via reflection (existing pattern)
