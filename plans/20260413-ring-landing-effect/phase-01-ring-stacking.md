# Phase 1: Ring Stacking System

**Effort**: S (1 day)
**Dependencies**: None
**Owner**: implementer

## Objective

Make collected rings visible on the bucket pole instead of hiding them.

## Current Behavior

```csharp
// Ball.cs:125
this.gameObject.SetActive(false);  // Ring disappears
```

## Target Behavior

- Bucket manages list of visible ring GameObjects
- Each ring positioned at stack height: `baseY + stackIndex * ringHeight`
- Ring re-parented to bucket's visualRoot
- Old rings fade/shrink when stack exceeds max visible

## Implementation Steps

### 1.1 Add Stacking Config to GameConstants

```csharp
// GameConstants.cs
public static class RingStackConfig
{
    public const float RingHeight = 0.08f;        // Height per ring in stack
    public const float BaseStackY = 0.1f;         // Starting Y position
    public const int MaxVisibleRings = 8;         // Max rings before fading old
    public const float RingScaleOnStack = 0.8f;   // Scale when stacked (relative)
}
```

### 1.2 Update Bucket.cs - Stack Management

```csharp
// New field
[SerializeField] private Transform stackRoot;  // Child transform for ring stack
private readonly List<Transform> stackedRings = new();

// New method
public Vector3 GetNextStackPosition()
{
    var stackIndex = this.stackedRings.Count;
    var y = GameConstants.RingStackConfig.BaseStackY 
          + stackIndex * GameConstants.RingStackConfig.RingHeight;
    return new Vector3(0, y, 0);
}

// Modify AddBall() - don't deactivate, stack instead
public void AddBallToStack(Ball ball)
{
    var ringTransform = ball.transform;
    
    // Reparent to stack
    ringTransform.SetParent(this.stackRoot ?? this.transform);
    ringTransform.localPosition = this.GetNextStackPosition();
    ringTransform.localRotation = Quaternion.identity;
    ringTransform.localScale = Vector3.one * GameConstants.RingStackConfig.RingScaleOnStack;
    
    this.stackedRings.Add(ringTransform);
    this.FadeOldRingsIfNeeded();
}

private void FadeOldRingsIfNeeded()
{
    if (this.stackedRings.Count <= GameConstants.RingStackConfig.MaxVisibleRings)
        return;
    
    // Fade/shrink oldest ring
    var oldest = this.stackedRings[0];
    oldest.DOScale(0f, 0.3f).OnComplete(() => oldest.gameObject.SetActive(false));
}
```

### 1.3 Update Ball.cs - Don't Self-Deactivate

```csharp
// Ball.cs:JumpToBucket() - Replace line 125
// OLD: this.gameObject.SetActive(false);
// NEW: (removed - bucket handles visibility now)
```

### 1.4 Add stackRoot to Bucket Prefab

- Create empty child "StackRoot" at position (0, 0, 0)
- Assign to Bucket.stackRoot field

## File Ownership

| File | Lines | Exclusive |
|------|-------|-----------|
| `Scripts/Core/GameConstants.cs` | 119-126 | Yes |
| `Scripts/Bucket/Bucket.cs` | 180-220 (approx) | Yes |
| `Scripts/Ring/Ball.cs` | 125 | Yes |
| `Prefabs/Bucket.prefab` | - | Yes |

## Verification

- [ ] Ring stays visible after jumping to bucket
- [ ] Multiple rings stack vertically
- [ ] Old rings fade when exceeding max
- [ ] No errors in console
- [ ] Existing bucket completion flow still works
