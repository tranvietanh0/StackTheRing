# Phase 3: Update LevelManager (Load Prefab)

**Effort:** M (1-2 days)
**Dependencies:** Phase 2

## Objective

Update LevelManager để:
1. Load level prefab thay vì chỉ load LevelData SO
2. Instantiate prefab và inject dependencies
3. Cleanup level cũ khi load level mới

## Files to Modify

| File | Action | Description |
|------|--------|-------------|
| `Scripts/Level/LevelManager.cs` | Update | Load prefab, manage lifecycle |
| `Scripts/Level/ILevelManager.cs` | Update | Interface changes |

## Interface Changes

```csharp
public interface ILevelManager
{
    int CurrentLevel { get; }
    int HighestUnlockedLevel { get; }
    LevelData CurrentLevelData { get; }
    LevelController CurrentLevelController { get; }  // NEW
    
    UniTask<LevelController> LoadLevel(int levelNumber);  // Changed return type
    void UnloadCurrentLevel();  // NEW
    void CompleteLevel();
    void FailLevel();
    void SaveProgress();
}
```

## Implementation Steps

### Step 1: Add Level Prefab Loading

```csharp
public class LevelManager : ILevelManager
{
    private const string LevelPrefabPrefix = "Level_";
    
    private readonly IGameAssets gameAssets;
    private readonly Transform levelRoot;
    private LevelController currentLevelController;
    
    // Inject callback for DI
    private readonly System.Action<LevelController> injectCallback;
    
    public LevelController CurrentLevelController => this.currentLevelController;
    
    public LevelManager(
        IGameAssets gameAssets, 
        SignalBus signalBus, 
        ILoggerManager loggerManager,
        Transform levelRoot,
        System.Action<LevelController> injectCallback)
    {
        this.gameAssets = gameAssets;
        this.levelRoot = levelRoot;
        this.injectCallback = injectCallback;
        // ...
    }
}
```

### Step 2: Implement LoadLevel with Prefab

```csharp
public async UniTask<LevelController> LoadLevel(int levelNumber)
{
    // Cleanup previous level
    this.UnloadCurrentLevel();
    
    this.CurrentLevel = levelNumber;
    var prefabKey = $"{LevelPrefabPrefix}{levelNumber:D2}";
    
    this.logger.Info($"Loading level prefab: {prefabKey}");
    
    try
    {
        // Load prefab từ Addressables
        var handle = this.gameAssets.LoadAssetAsync<GameObject>(prefabKey);
        await handle.Task;
        
        if (handle.Result == null)
        {
            this.logger.Error($"Failed to load level prefab: {prefabKey}");
            return null;
        }
        
        // Instantiate
        var levelGO = Object.Instantiate(handle.Result, this.levelRoot);
        levelGO.name = $"Level_{levelNumber:D2}";
        
        // Get controller
        this.currentLevelController = levelGO.GetComponent<LevelController>();
        if (this.currentLevelController == null)
        {
            this.logger.Error("Level prefab missing LevelController component!");
            Object.Destroy(levelGO);
            return null;
        }
        
        // Inject dependencies
        this.injectCallback?.Invoke(this.currentLevelController);
        
        // Cache LevelData
        this.CurrentLevelData = this.currentLevelController.LevelData;
        
        // Fire signal
        this.signalBus.Fire(new LevelStartSignal { LevelNumber = levelNumber });
        
        this.logger.Info($"Level {levelNumber} loaded successfully");
        return this.currentLevelController;
    }
    catch (System.Exception ex)
    {
        this.logger.Error($"Failed to load level: {ex.Message}");
        return null;
    }
}
```

### Step 3: Implement UnloadCurrentLevel

```csharp
public void UnloadCurrentLevel()
{
    if (this.currentLevelController != null)
    {
        Object.Destroy(this.currentLevelController.gameObject);
        this.currentLevelController = null;
        this.CurrentLevelData = null;
        
        this.logger.Info("Previous level unloaded");
    }
}
```

### Step 4: Update MainSceneScope để Pass Dependencies

```csharp
// Trong MainSceneScope.RegisterServices()
builder.Register<LevelManager>(Lifetime.Singleton)
    .WithParameter("levelRoot", this.levelRoot)
    .WithParameter("injectCallback", (System.Action<LevelController>)(controller =>
    {
        controller.Inject(
            container.Resolve<SignalBus>(),
            container.Resolve<ILevelManager>(),
            container.Resolve<GameStateMachine>(),
            container.Resolve<ILoggerManager>(),
            container.Resolve<CollectAreaBucketService>()
        );
    }))
    .AsImplementedInterfaces();
```

## Validation

```csharp
// Test loading
var controller = await levelManager.LoadLevel(1);
Debug.Assert(controller != null);
Debug.Assert(controller.LevelData != null);

// Test unload + reload
levelManager.UnloadCurrentLevel();
controller = await levelManager.LoadLevel(2);
```

## Checklist

- [ ] ILevelManager interface updated
- [ ] LevelManager loads prefab from Addressables
- [ ] LevelManager instantiates under levelRoot
- [ ] LevelController gets injected correctly
- [ ] UnloadCurrentLevel cleans up properly
- [ ] No memory leaks (prefab destroyed on unload)
- [ ] Retry/NextLevel still works
