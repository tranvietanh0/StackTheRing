# Phase 5: Attraction Logic Refactor

**Effort:** M (1-2 days)
**Dependencies:** Phase 2, 3, 4
**Blocking:** Phase 6

## Objective

Chuyển từ progress-based attraction sang entry-point based theo Cocos.

## Current vs New

| Aspect | Current (Unity) | New (Cocos-style) |
|--------|-----------------|-------------------|
| Trigger | AttractionController.Update() checks progress | RowBallReachEntrySignal at entry point |
| Target | SlotManager.GetSlotForColor() | GetAvailableBucketByColor() |
| Logic | IsInAttractionZone(progress) | Distance to entry node < threshold |

## Files to Create

### 1. `Scripts/Services/CollectAreaBucketService.cs` (~150 LOC)

Service để query buckets trong CollectAreas.

**Methods (match Cocos CollectAreaBucketService.ts):**
```csharp
- GetTargetColorsFromBuckets() → List<ColorType>
- GetAvailableBucketByColor(ColorType) → Bucket
- GetAvailableBucketsByColor(ColorType) → List<Bucket>
- GetAvailableSlotCountByColor(ColorType) → int
- IsColorTargeted(ColorType) → bool
- BuildBalancedBucketPlanByColor(ColorType, int ballCount) → List<Bucket>
```

## Files to Modify

### 1. `Scripts/Conveyor/ConveyorController.cs`

Add entry point detection:

```csharp
// New fields
[SerializeField] private List<Transform> entryNodes;
private HashSet<RowBall> processedAtEntry = new();

// In Update() or separate method
private void CheckEntryPoints()
{
    foreach (var rowBall in activeRowBalls)
    {
        foreach (var (entryNode, index) in entryNodes.WithIndex())
        {
            if (IsNearEntryPoint(rowBall, entryNode))
            {
                if (!processedAtEntry.Contains(rowBall))
                {
                    processedAtEntry.Add(rowBall);
                    OnRowBallReachedEntry(rowBall, index);
                }
            }
            else
            {
                processedAtEntry.Remove(rowBall);
            }
        }
    }
}

private async void OnRowBallReachedEntry(RowBall rowBall, int entryIndex)
{
    // Match Cocos MainConveyorController.onRowBallReachedEntry()
    
    // 1. Get target colors from buckets in CollectAreas
    var targetColors = collectAreaBucketService.GetTargetColorsFromBuckets();
    
    // 2. Filter balls matching target colors
    var ballsToCollect = rowBall.GetActiveBalls()
        .Where(b => targetColors.Contains(b.BallColor))
        .ToList();
    
    // 3. Check has enough slots
    if (!HasEnoughSlots(ballsToCollect)) return;
    
    // 4. Build bucket assignments
    var assignments = BuildBucketAssignments(ballsToCollect);
    
    // 5. Start incoming on each bucket
    foreach (var (ball, bucket) in assignments)
    {
        bucket.StartIncomingBall();
    }
    
    // 6. Jump balls with delay
    var jumpTasks = new List<UniTask>();
    for (int i = 0; i < assignments.Count; i++)
    {
        var (ball, bucket) = assignments[i];
        var delay = i * GameConstants.RowBallConfig.BallJumpDelay;
        
        jumpTasks.Add(JumpBallToBucketWithDelay(ball, bucket, delay));
    }
    
    await UniTask.WhenAll(jumpTasks);
}
```

### 2. `Scripts/Ring/Ball.cs`

Update `JumpToBucket()` method:

```csharp
public async UniTask JumpToBucket(Bucket targetBucket, bool incomingAlreadyReserved = false)
{
    // Match Cocos Ball.jumpToBucket()
    
    if (isCollected || targetBucket == null || targetBucket.IsBucketCompleted())
        return;
    
    // Check available slots
    var incomingToCheck = incomingAlreadyReserved ? 0 : targetBucket.GetIncomingBallCount();
    var availableSlots = targetBucket.GetRemainingSlotCount(incomingToCheck);
    if (availableSlots <= 0) return;
    
    isCollected = true;
    
    if (!incomingAlreadyReserved)
        targetBucket.StartIncomingBall();
    
    // Fire signal
    signalBus.Fire(new BallCollectedSignal { ... });
    
    // Jump animation
    await JumpService.Instance.JumpToDestination(
        transform,
        targetBucket.transform,
        GameConstants.BallConfig.JumpHeight,
        GameConstants.BallConfig.JumpDuration,
        Vector3.zero
    );
    
    // Add to bucket
    targetBucket.AddBall(this);
    targetBucket.CompleteIncomingBall();
}
```

## Files to Delete (after integration working)

- `Scripts/Attraction/AttractionController.cs` — logic moved to ConveyorController
- `Scripts/Attraction/AttractionConfig.cs` — constants moved to GameConstants

## Verification

- [ ] Entry point detection works (distance-based)
- [ ] RowBallReachEntrySignal fires at correct position
- [ ] Ball jumps to correct bucket (color match)
- [ ] Multiple balls jump with staggered delay
- [ ] Bucket incoming count tracks correctly
- [ ] No double-collection of same ball

## Cocos Reference

`MainConveyorController.ts`:
- `onRowBallReachedEntry()` line 119
- `hasEnoughSlotsForEntry()` line 261
- `resolveTargetColorsForEntry()` line 232

`Ball.ts`:
- `jumpToBucket()` line 29
