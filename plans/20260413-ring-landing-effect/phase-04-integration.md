# Phase 4: Integration & Polish

**Effort**: S (1 day)
**Dependencies**: Phase 1, Phase 2, Phase 3
**Owner**: implementer

## Objective

Integrate all components, polish timing, and ensure smooth flow.

## Integration Points

### 4.1 Update Ball.JumpToBucket() - Complete Flow

```csharp
public async UniTask JumpToBucket(Bucket targetBucket, bool incomingAlreadyReserved = false)
{
    // ... existing validation code ...

    this.isCollected = true;
    
    if (!incomingAlreadyReserved)
    {
        targetBucket.StartIncomingBall();
        hasReservation = true;
    }

    // Fire collected signal (existing)
    this.signalBus?.Fire(new BallCollectedSignal { ... });

    // Detach from parent for jump
    this.transform.SetParent(null);

    // Jump animation (existing)
    await JumpService.Instance.JumpToDestination(...);

    // === NEW: Landing Effect Sequence ===
    
    // 1. Get stack position
    var stackPos = targetBucket.GetNextStackPosition();
    
    // 2. Reparent to bucket for local space
    this.transform.SetParent(targetBucket.StackRoot ?? targetBucket.transform);
    
    // 3. Play sparkle VFX (fire and forget)
    SparkleEffectPool.Instance?.PlayAt(this.transform.position, this.BallColor).Forget();
    
    // 4. Play wobble animation (await)
    var landingEffect = this.GetComponent<RingLandingEffect>() 
        ?? this.gameObject.AddComponent<RingLandingEffect>();
    await landingEffect.PlayLandingEffect(stackPos);
    
    // 5. Register with bucket
    targetBucket.AddBallToStack(this);
    
    // === END NEW ===

    targetBucket.CompleteIncomingBall();
    reservationCompleted = true;
    
    // REMOVED: this.gameObject.SetActive(false);
}
```

### 4.2 Update Bucket.cs - Expose StackRoot

```csharp
// Add serialized field
[SerializeField] private Transform stackRoot;

// Add public property
public Transform StackRoot => this.stackRoot;

// Update DestroyAllBalls() for cleanup
private void DestroyAllBalls()
{
    // Destroy tracked balls
    foreach (var ball in this.collectedBalls)
    {
        if (ball != null)
        {
            Destroy(ball.gameObject);
        }
    }
    this.collectedBalls.Clear();

    // Also destroy stacked ring visuals
    foreach (var ringTransform in this.stackedRings)
    {
        if (ringTransform != null)
        {
            Destroy(ringTransform.gameObject);
        }
    }
    this.stackedRings.Clear();
}
```

### 4.3 Timing Coordination

```
Timeline:
0ms    - Jump starts
200ms  - Jump completes (JumpDuration)
200ms  - Sparkle burst triggers
200ms  - Wobble starts
700ms  - Wobble completes (WobbleDuration)
700ms  - Ring registered in stack
```

### 4.4 Config Tuning Values (Recommended Defaults)

```csharp
// After testing, these values feel good:
public static class WobbleConfig
{
    public const float InitialTiltAngle = 12f;    // Not too dramatic
    public const float DampingFactor = 0.55f;     // Natural decay
    public const int OscillationCount = 4;        // Enough to feel real
    public const float WobbleDuration = 0.45f;    // Quick but visible
    public const float BounceHeight = 0.03f;      // Subtle bounce
}

public static class SparkleConfig
{
    public const int ParticleCount = 20;          // Not too busy
    public const float ParticleLifetime = 0.35f;  // Quick sparkle
    public const float BurstSpeed = 1.8f;         // Moderate spread
    public const float SparkleScale = 0.015f;     // Small particles
}

public static class RingStackConfig
{
    public const float RingHeight = 0.06f;        // Tight stacking
    public const float BaseStackY = 0.08f;        // Just above pole base
    public const int MaxVisibleRings = 10;        // Before fading
    public const float RingScaleOnStack = 0.7f;   // Slightly smaller
}
```

### 4.5 Edge Cases

| Case | Handling |
|------|----------|
| Rapid consecutive landings | Each landing gets own wobble, sparkles overlap (OK) |
| Bucket completes mid-wobble | Wobble continues, then bucket completion triggers |
| Ring destroyed mid-wobble | DOTween sequence killed in OnDestroy |
| No SparkleEffectPool in scene | Null check skips VFX (graceful) |

### 4.6 Scene Setup Checklist

- [ ] Add StackRoot child to Bucket prefab
- [ ] Create Effects folder in Prefabs
- [ ] Create RingSparkle.prefab with ParticleSystem
- [ ] Add EffectsPool GameObject to MainScene
- [ ] Add SparkleEffectPool component, assign prefab

## File Ownership

| File | Lines | Exclusive |
|------|-------|-----------|
| `Scripts/Ring/Ball.cs` | 66-135 | Final integration |
| `Scripts/Bucket/Bucket.cs` | Multiple sections | Final integration |
| `Prefabs/Bucket.prefab` | StackRoot child | Yes |
| `Scenes/1.MainScene.unity` | EffectsPool object | Yes |

## Polish Checklist

- [ ] Test with single ring landing
- [ ] Test with rapid 5+ ring landings
- [ ] Test bucket completion with stacked rings
- [ ] Test on different ring colors
- [ ] Check no console errors
- [ ] Profile for performance (aim <1ms per landing)
- [ ] Verify DOTween sequences properly killed on destroy

## Verification

- [ ] Complete flow: jump -> sparkle -> wobble -> stack
- [ ] Timing feels natural, not too slow/fast
- [ ] Visual polish: colors pop, wobble realistic
- [ ] No regression in existing bucket/ball behavior
- [ ] Memory stable (no leaks over many plays)
