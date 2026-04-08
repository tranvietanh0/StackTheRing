# Phase 2: Signals

**Effort:** S (0.5 day)
**Dependencies:** Phase 1 (ColorType)
**Blocks:** Phase 4, 6

## Objective

Create signal classes for pub/sub communication (MessagePipe SignalBus).

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Signals/StackTheRingSignals.cs` | CREATE | this phase |

## Implementation

### StackTheRingSignals.cs

All signals in one file. **Signals MUST be class (not struct)** per project standards.

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Signals
{
    using HyperCasualGame.Scripts.StackTheRing.Data;

    /// <summary>
    /// Fired when a ball is collected (removed from RowBall, jumping to bucket).
    /// </summary>
    public class BallCollectedSignal
    {
        public int RowId;
        public int BallIndex;
        public ColorType Color;

        public BallCollectedSignal(int rowId, int ballIndex, ColorType color)
        {
            RowId = rowId;
            BallIndex = ballIndex;
            Color = color;
        }
    }

    /// <summary>
    /// Fired when a bucket reaches its target ball count.
    /// </summary>
    public class BucketCompletedSignal
    {
        public ColorType BucketColor;

        public BucketCompletedSignal(ColorType bucketColor)
        {
            BucketColor = bucketColor;
        }
    }

    /// <summary>
    /// Fired when all buckets are completed (level win).
    /// </summary>
    public class LevelCompleteSignal
    {
        public int LevelIndex;

        public LevelCompleteSignal(int levelIndex)
        {
            LevelIndex = levelIndex;
        }
    }

    /// <summary>
    /// Fired when a RowBall reaches an entry point on the conveyor.
    /// </summary>
    public class RowBallReachEntrySignal
    {
        public UnityEngine.Transform RowBallTransform;
        public int ConveyorId;
        public int EntryNodeIndex;

        public RowBallReachEntrySignal(UnityEngine.Transform rowBallTransform, int conveyorId, int entryNodeIndex = -1)
        {
            RowBallTransform = rowBallTransform;
            ConveyorId = conveyorId;
            EntryNodeIndex = entryNodeIndex;
        }
    }

    /// <summary>
    /// Request to load next level.
    /// </summary>
    public class NextLevelRequestedSignal
    {
        public int NextLevelType;

        public NextLevelRequestedSignal(int nextLevelType)
        {
            NextLevelType = nextLevelType;
        }
    }
}
```

## Signal Usage Reference

| Signal | Publisher | Subscriber |
|--------|-----------|------------|
| `BallCollectedSignal` | Ball.JumpToBucket() | RowBall (remove slot) |
| `BucketCompletedSignal` | Bucket.OnComplete() | StackTheRingController |
| `LevelCompleteSignal` | StackTheRingController | GamePlayState |
| `RowBallReachEntrySignal` | PathFollower | ConveyorController |
| `NextLevelRequestedSignal` | GameWinState | LevelLoader |

## Verification

- [ ] File compiles without errors
- [ ] All signals are `class` (not `struct`)
- [ ] All signals have constructor with parameters
- [ ] No `[Inject]` attributes used

## Notes

- Signals declared in DI scope via `builder.DeclareSignal<T>()`
- Subscribe/Unsubscribe via `signalBus.Subscribe<T>()` / `signalBus.Unsubscribe<T>()`
- Always unsubscribe in Exit()/OnDestroy() to prevent memory leaks
