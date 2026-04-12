# Phase 2: Bucket System

**Effort:** M (1-2 days)
**Dependencies:** Phase 1
**Blocking:** Phase 5, 6

## Objective

Tạo Bucket component và BucketColumnManager theo Cocos architecture.

## Files to Create

### 1. `Scripts/Bucket/Bucket.cs` (~250 LOC)

**Properties:**
```csharp
- BucketConfig data
- bool isInCollectArea
- List<Ball> collectedBalls
- int incomingBalls
- bool isCompleted
- MeshRenderer[] meshRenderers (for color)
- Transform labelTransform (progress %)
- TextMeshPro labelPercent
```

**Methods (match Cocos Bucket.ts):**
```csharp
// Lifecycle
- Initialize(BucketConfig data)
- UpdateState()
- UpdateColor(ColorType color)

// Ball management
- AddBall(Ball ball)
- StartIncomingBall()
- CompleteIncomingBall()
- GetTargetBallCount() → int
- GetCollectedBallCount() → int
- GetIncomingBallCount() → int
- GetRemainingSlotCount(int incoming = 0) → int
- IsBucketCompleted() → bool

// Jump & Animation
- JumpToCollectArea(Transform targetArea) → UniTask
- PlayCollectionAnimation() → UniTask (move up → rotate → scale down)
- TriggerShake()

// Completion
- OnBucketCollectDoneHandler()
- Cleanup()
```

### 2. `Scripts/Bucket/BucketColumnManager.cs` (~200 LOC)

**Properties:**
```csharp
- Bucket bucketPrefab
- Transform bucketContainer
- List<Transform> columnNodes
- List<Bucket> spawnedBuckets
- float columnSpacing = 1.2f
- float rowSpacing = 1.2f
```

**Methods (match Cocos GridBucketManager.ts):**
```csharp
// Setup
- Initialize()
- SpawnBuckets(LevelData levelData)
- CreateDynamicColumns(int count, float spacing)

// Query
- GetEligibleBuckets() → List<Bucket> (first non-placed per column)
- HasBucketsOnGrid() → bool

// Events
- OnBucketTapped(Bucket bucket) // → fire signal, trigger jump

// Cleanup
- Cleanup()
```

## Files to Modify

### `Scripts/Scenes/Main/MainSceneScope.cs`

Register BucketColumnManager:
```csharp
builder.RegisterComponentInHierarchy<BucketColumnManager>();
```

## Prefab Requirements

- Create `Bucket` prefab với:
  - MeshRenderer (với material có thể đổi màu)
  - Label (TextMeshPro) cho progress %
  - Collider cho tap detection

## Verification

- [ ] Bucket.Initialize() sets up state correctly
- [ ] Bucket.AddBall() increments count, triggers shake
- [ ] Bucket completes when collectedBalls >= targetBallCount
- [ ] BucketColumnManager spawns grid correctly
- [ ] Tap eligible bucket fires BucketTappedSignal

## Cocos Reference

Key methods from `Bucket.ts`:
- `initialize()` line 55
- `addBall()` line 268
- `jumpToCollectArea()` line 163
- `playCollectionAnimation()` line 352
