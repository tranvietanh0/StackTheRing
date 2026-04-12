# Phase 6: Integration & Cleanup

**Effort:** M (1 day)
**Dependencies:** Phase 5
**Blocking:** None (final phase)

## Objective

Wire up all components, update GameManager, cleanup old code, test end-to-end.

## Files to Modify

### 1. `Scripts/Core/GameManager.cs`

Replace old references with new components:

```csharp
// OLD
[SerializeField] private SlotManager slotManager;
[SerializeField] private CollectorPanel collectorPanel;

// NEW
[SerializeField] private BucketColumnManager bucketColumnManager;
[SerializeField] private CollectAreaManager collectAreaManager;

// Inject
private CollectAreaBucketService collectAreaBucketService;
private JumpService jumpService;

// InitializeSystems()
private void InitializeSystems()
{
    this.conveyorController.Initialize(this.signalBus, this.loggerManager);
    this.bucketColumnManager.Initialize();
    this.collectAreaManager.SpawnAreas(4); // or from level config
    
    // Wire up services
    this.collectAreaBucketService.SetManagers(
        this.collectAreaManager,
        this.bucketColumnManager
    );
    
    // Subscribe to bucket tap
    this.signalBus.Subscribe<BucketTappedSignal>(this.OnBucketTapped);
}

private async void OnBucketTapped(BucketTappedSignal signal)
{
    var emptyArea = this.collectAreaManager.GetFirstEmptyArea();
    if (emptyArea == null) return;
    
    await signal.Bucket.JumpToCollectArea(emptyArea.transform);
    emptyArea.Occupy(signal.Bucket);
}
```

### 2. `Scripts/StateMachines/Game/States/GamePlayState.cs`

Update win/lose conditions:

```csharp
// OLD
private void CheckLoseCondition()
{
    if (!slotManager.AllSlotsOccupied()) return;
    // ...check canCollectAny from slots
}

// NEW
private void CheckLoseCondition()
{
    if (!collectAreaManager.AreAllCollectAreasOccupied()) return;
    
    // Check if any ball on conveyor matches any bucket color
    var targetColors = collectAreaBucketService.GetTargetColorsFromBuckets();
    var canCollectAny = false;
    
    foreach (var rowBall in conveyor.ActiveRowBalls)
    {
        foreach (var ball in rowBall.GetActiveBalls())
        {
            if (targetColors.Contains(ball.BallColor) &&
                collectAreaBucketService.GetAvailableSlotCountByColor(ball.BallColor) > 0)
            {
                canCollectAny = true;
                break;
            }
        }
        if (canCollectAny) break;
    }
    
    if (!canCollectAny)
    {
        // LOSE
        levelManager.FailLevel();
        StateMachine.TransitionTo<GameLoseState>();
    }
}

// Win condition: subscribe to BucketCompletedSignal
// Track total buckets vs completed buckets
```

### 3. `Scripts/Signals/GameSignals.cs`

Ensure all new signals are declared in LifetimeScope.

### 4. `Scripts/Scenes/Main/MainSceneScope.cs`

Register all new components:
```csharp
// Services
builder.Register<CollectAreaBucketService>(Lifetime.Scoped);

// Components in hierarchy
builder.RegisterComponentInHierarchy<BucketColumnManager>();
builder.RegisterComponentInHierarchy<CollectAreaManager>();
```

## Files to Delete

| File | Reason |
|------|--------|
| `Scripts/Slot/Slot.cs` | Replaced by Bucket |
| `Scripts/Slot/SlotManager.cs` | Replaced by BucketColumnManager + CollectAreaManager |
| `Scripts/Slot/ColorCollector.cs` | Logic moved to Bucket tap |
| `Scripts/Slot/CollectorPanel.cs` | Not needed |
| `Scripts/Attraction/AttractionController.cs` | Logic in ConveyorController |
| `Scripts/Attraction/AttractionConfig.cs` | Constants in GameConstants |

## Scene Updates

1. Remove old GameObjects:
   - SlotManager
   - CollectorPanel
   - AttractionController
   - Individual Slots

2. Add new GameObjects:
   - BucketColumnManager (with BucketContainer child)
   - CollectAreaManager (with AreaContainer child)

3. Update prefab references in GameManager

## Verification Checklist

### Functional Tests
- [ ] Game starts, buckets spawn in grid
- [ ] Tap bucket → jumps to CollectArea
- [ ] Ball reaches entry point → signal fires
- [ ] Ball jumps to correct bucket (color match)
- [ ] Multiple balls jump with delay
- [ ] Bucket fills → completion animation → destroy
- [ ] All buckets complete → WIN
- [ ] No possible moves → LOSE
- [ ] Retry level works
- [ ] Next level works

### Memory/Cleanup Tests
- [ ] No signal subscription leaks
- [ ] Buckets destroyed properly
- [ ] Balls destroyed properly
- [ ] Scene reload works without errors

### Edge Cases
- [ ] Tap bucket when no CollectArea empty → nothing happens
- [ ] Ball color not in any bucket → not collected
- [ ] Multiple balls same color → distributed correctly
- [ ] Rapid tapping → no duplicate jumps

## Final Cleanup

1. Remove any unused `using` statements
2. Delete empty Slot/Attraction folders
3. Update documentation in `docs/`
4. Update `CLAUDE.md` if needed
