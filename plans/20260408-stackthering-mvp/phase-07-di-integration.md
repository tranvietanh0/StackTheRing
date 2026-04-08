# Phase 7: DI Integration

**Effort:** S (1 day)
**Dependencies:** Phase 1-6
**Blocks:** Phase 8, 9

## Objective

Register all StackTheRing services and components with VContainer DI.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Core/StackTheRingSceneScope.cs` | CREATE | this phase |
| `Assets/Scripts/Scenes/Main/MainSceneScope.cs` | MODIFY | this phase |

## Implementation

### 1. StackTheRingSceneScope.cs

Scene-specific DI scope for StackTheRing game.

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Core
{
    using System.Collections.Generic;
    using GameFoundationCore.Scripts.DI.VContainer;
    using HyperCasualGame.Scripts.StackTheRing.Conveyor;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using HyperCasualGame.Scripts.StackTheRing.Services;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;
    using VContainer;

    public class StackTheRingSceneScope : SceneScope
    {
        [Header("Config")]
        [SerializeField] private StackTheRingConfig config;

        [Header("Prefabs")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject rowBallPrefab;
        [SerializeField] private GameObject bucketPrefab;

        [Header("Scene References")]
        [SerializeField] private ConveyorController mainConveyor;
        [SerializeField] private Transform collectAreaContainer;
        [SerializeField] private List<CollectArea> collectAreas;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private Camera mainCamera;

        protected override void Configure(IContainerBuilder builder)
        {
            // Config (instance)
            builder.RegisterInstance(config);

            // Services (Singleton)
            builder.Register<IJumpService, JumpService>(Lifetime.Singleton);
            builder.Register<IColorService, ColorService>(Lifetime.Singleton);

            // Core Logic (Singleton)
            builder.Register<StackTheRingController>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            builder.Register<LevelLoader>(Lifetime.Singleton)
                .WithParameter("ballPrefab", ballPrefab)
                .WithParameter("rowBallPrefab", rowBallPrefab)
                .WithParameter("bucketPrefab", bucketPrefab);

            // Scene Components (from scene)
            builder.RegisterComponent(mainConveyor);
            builder.RegisterComponent(inputHandler);

            // Signals
            builder.DeclareSignal<BallCollectedSignal>();
            builder.DeclareSignal<BucketCompletedSignal>();
            builder.DeclareSignal<LevelCompleteSignal>();
            builder.DeclareSignal<RowBallReachEntrySignal>();
            builder.DeclareSignal<NextLevelRequestedSignal>();
        }

        private void Start()
        {
            // Setup references after DI resolves
            var container = Container;
            var levelLoader = container.Resolve<LevelLoader>();

            levelLoader.SetupReferences(
                ballPrefab,
                rowBallPrefab,
                bucketPrefab,
                mainConveyor,
                collectAreaContainer,
                collectAreas
            );

            // Setup input handler camera
            if (inputHandler != null && mainCamera != null)
            {
                inputHandler.SetCamera(mainCamera);
            }
        }
    }
}
```

### 2. MainSceneScope.cs (MODIFY)

Update existing MainSceneScope to work with StackTheRing.

```csharp
namespace HyperCasualGame.Scripts.Scenes.Main
{
    using System.Linq;
    using GameFoundationCore.Scripts.DI.VContainer;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UniT.Extensions;
    using VContainer;

    public class MainSceneScope : SceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Game State Machine (existing)
            builder.Register<GameStateMachine>(Lifetime.Singleton)
                .WithParameter(container => typeof(IGameState).GetDerivedTypes()
                    .Select(type => (IGameState)container.Instantiate(type)).ToList())
                .AsInterfacesAndSelf();

            // Note: StackTheRing-specific registrations are in StackTheRingSceneScope
            // which should be a child scope or added to this scene
        }
    }
}
```

## Scene Setup Instructions

1. **Add StackTheRingSceneScope to scene:**
   - Create empty GameObject named "StackTheRingScope"
   - Add `StackTheRingSceneScope` component
   - Set as child of MainSceneScope (for scope hierarchy)

2. **Assign references in Inspector:**
   - Config: Create `StackTheRingConfig` ScriptableObject asset
   - Prefabs: Assign Ball, RowBall, Bucket prefabs
   - Scene References: Drag scene objects

3. **Layer setup:**
   - Create "Bucket" layer for raycast filtering
   - Assign layer to bucket prefab colliders

## Verification

- [ ] StackTheRingSceneScope compiles without errors
- [ ] All serialized fields show in Inspector
- [ ] Signals are declared (no runtime errors)
- [ ] StackTheRingController.Initialize() is called on scene load
- [ ] LevelLoader receives correct prefab references

## Notes

- StackTheRingSceneScope can be child of MainSceneScope for hierarchical DI
- Alternative: merge into MainSceneScope if simpler
- Prefabs must be in Addressables or Resources for runtime loading
- Scene references are assigned in Inspector (not loaded dynamically)
