# Phase 4: Update MainScene & Scope

**Effort:** S (< 1 day)
**Dependencies:** Phase 3

## Objective

- Cleanup MainScene: remove gameplay objects (đã move vào prefab)
- Add levelRoot spawn point
- Update MainSceneScope để không còn reference trực tiếp LevelController

## Files to Modify

| File | Action | Description |
|------|--------|-------------|
| `Scenes/1.MainScene.unity` | Update | Remove objects, add levelRoot |
| `Scripts/Scenes/Main/MainSceneScope.cs` | Update | Remove controller ref, add levelRoot |
| `Scripts/StateMachines/Game/States/GamePlayState.cs` | Update | Get controller từ LevelManager |

## Target Scene Hierarchy

```
1.MainScene
├── MainSceneScope
├── LevelRoot (empty Transform)  ← Spawn point for level prefabs
├── Cameras/
│   └── Main Camera
├── Lighting/
│   └── Directional Light
└── UI/
    └── Canvas (screens)
```

## Implementation Steps

### Step 1: Update MainSceneScope

```csharp
public class MainSceneScope : SceneScope
{
    [SerializeField] private Transform levelRoot;  // NEW: spawn point
    
    // REMOVED: [SerializeField] private GameManager gameManager;
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<VContainerAdapter>(Lifetime.Scoped).AsImplementedInterfaces();
        
        this.RegisterSignals(builder);
        this.RegisterServices(builder);
        this.RegisterStateMachine(builder);
        // REMOVED: this.RegisterComponents(builder);
    }
    
    private void RegisterServices(IContainerBuilder builder)
    {
        // LevelManager với levelRoot và inject callback
        builder.Register<LevelManager>(Lifetime.Singleton)
            .WithParameter("levelRoot", this.levelRoot)
            .WithParameter("injectCallback", new System.Action<LevelController>(controller =>
            {
                // Inject sẽ được gọi khi level load
                // Dependencies từ container
            }))
            .AsImplementedInterfaces();
            
        builder.Register<CollectAreaBucketService>(Lifetime.Scoped);
    }
    
    // REMOVED: RegisterComponents method
}
```

### Step 2: Update MainSceneScope - Full Inject Callback

```csharp
private void RegisterServices(IContainerBuilder builder)
{
    builder.Register<CollectAreaBucketService>(Lifetime.Scoped);
    
    // Register LevelManager with dependencies
    builder.RegisterFactory<Transform, System.Action<LevelController>, LevelManager>(
        (levelRoot, injectCallback) => new LevelManager(
            builder.Container.Resolve<IGameAssets>(),
            builder.Container.Resolve<SignalBus>(),
            builder.Container.Resolve<ILoggerManager>(),
            levelRoot,
            injectCallback
        ),
        Lifetime.Singleton
    ).AsImplementedInterfaces();
    
    // Alternative: Use RegisterBuildCallback
    builder.RegisterBuildCallback(container =>
    {
        var levelManager = container.Resolve<ILevelManager>() as LevelManager;
        levelManager?.SetInjectCallback(controller =>
        {
            controller.Inject(
                container.Resolve<SignalBus>(),
                container.Resolve<ILevelManager>(),
                container.Resolve<GameStateMachine>(),
                container.Resolve<ILoggerManager>(),
                container.Resolve<CollectAreaBucketService>()
            );
        });
    });
}
```

### Step 3: Update GamePlayState

```csharp
public class GamePlayState : IGameState, IHaveStateMachine
{
    private readonly ILevelManager levelManager;
    
    public GamePlayState(ILevelManager levelManager, ...)
    {
        this.levelManager = levelManager;
    }
    
    public void Enter()
    {
        // Get current level controller từ LevelManager
        var controller = this.levelManager.CurrentLevelController;
        if (controller != null)
        {
            // Setup references
            this.SetReferences(
                controller.ConveyorController,
                controller.BucketColumnManager,
                controller.CollectAreaManager,
                // ...
            );
        }
    }
}
```

### Step 4: Update LevelController - Expose References

```csharp
public class LevelController : MonoBehaviour, IInitializable
{
    // Public getters for GamePlayState
    public ConveyorController ConveyorController => this.conveyorController;
    public BucketColumnManager BucketColumnManager => this.bucketColumnManager;
    public CollectAreaManager CollectAreaManager => this.collectAreaManager;
}
```

### Step 5: Update Scene in Unity Editor

1. Delete GameManager, ConveyorController, BucketColumnManager, CollectAreaManager từ scene
2. Create empty GameObject "LevelRoot" 
3. Assign LevelRoot vào MainSceneScope.levelRoot field
4. Save scene

## Validation

- Scene compiles without errors
- MainSceneScope không có missing references
- Play mode: Level loads correctly under LevelRoot
- Gameplay still works

## Checklist

- [ ] MainSceneScope.levelRoot field added
- [ ] MainSceneScope.gameManager field removed
- [ ] RegisterComponents method removed
- [ ] LevelManager receives levelRoot
- [ ] LevelController exposes component getters
- [ ] GamePlayState uses ILevelManager.CurrentLevelController
- [ ] Scene hierarchy cleaned up
- [ ] LevelRoot created in scene
- [ ] No missing references
