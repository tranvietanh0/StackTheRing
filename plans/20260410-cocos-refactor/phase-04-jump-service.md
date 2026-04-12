# Phase 4: JumpService

**Effort:** S (0.5 day)
**Dependencies:** None (can run parallel with Phase 2, 3)
**Blocking:** Phase 5

## Objective

Tạo JumpService singleton để xử lý jump animation (bucket bay, ball bay).

## Files to Create

### 1. `Scripts/Services/JumpService.cs` (~120 LOC)

**Singleton pattern giống Cocos JumpService.ts**

**Methods:**
```csharp
/// <summary>
/// Animate node jumping to destination with arc
/// </summary>
public async UniTask JumpToDestination(
    Transform node,
    Transform targetNode,
    float height,
    float duration,
    Vector3 endRotation,
    float targetYOffset = 0f
)

/// <summary>
/// Fly bucket to first empty CollectArea
/// </summary>
public async UniTask<CollectArea> FlyBucketToCollectArea(
    Bucket bucket,
    List<CollectArea> collectAreas,
    JumpConfig config
)
```

**JumpConfig struct:**
```csharp
public struct JumpConfig
{
    public float JumpHeight;
    public float JumpDuration;
    public Vector3 EndRotation;
}
```

**Animation Logic (match Cocos):**
```csharp
// From JumpService.ts line 52-112
1. Calculate start/end positions
2. Check dy threshold for wider path
3. Tween with:
   - X/Z: linear interpolation
   - Y: sin curve for arc (heightFactor = sin(t * PI))
   - Z offset if dy >= threshold (avoid collision)
   - Rotation: interpolate to endRotation
```

## Files to Modify

### `Scripts/Scenes/GameLifetimeScope.cs`

Register JumpService as singleton:
```csharp
builder.Register<JumpService>(Lifetime.Singleton);
```

## Verification

- [ ] JumpToDestination creates smooth arc animation
- [ ] Height factor uses sin curve correctly
- [ ] FlyBucketToCollectArea finds empty area and jumps
- [ ] Animation duration matches config

## Cocos Reference

`JumpService.ts`:
- `onJumpingToDestinationWithPromise()` line 52
- `flyBucketToCollectArea()` line 122
- Vec3Pool for GC optimization (optional for Unity)
