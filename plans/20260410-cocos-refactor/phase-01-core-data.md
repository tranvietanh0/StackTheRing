# Phase 1: Core Data Structures

**Effort:** S (1 day)
**Dependencies:** None
**Blocking:** Phase 2, 3, 5

## Objective

Tạo các data structures và signals cơ bản cần thiết cho Bucket/CollectArea system.

## Files to Create

### 1. `Scripts/Bucket/BucketConfig.cs`

```csharp
namespace HyperCasualGame.Scripts.Bucket
{
    using HyperCasualGame.Scripts.Core;

    [System.Serializable]
    public struct BucketConfig
    {
        public int IndexBucket;
        public int Row;
        public int Column;
        public ColorType Color;
        public int TargetBallCount;
    }
}
```

### 2. `Scripts/Core/BucketConstants.cs`

Thêm constants cho Bucket (copy từ Cocos `GameConfig.BUCKET`):
- `DEFAULT_JUMP_HEIGHT = 0f`
- `DEFAULT_JUMP_DURATION = 0.2f`
- `COLLECTION_MOVE_UP_OFFSET = 3f`
- `COLLECTION_ROTATION_DURATION = 0.5f`
- `COLLECTION_SCALE_DURATION = 0.5f`
- `SHAKE_SCALE_BUMP = 0.03f`
- `SHAKE_DURATION = 0.1f`

### 3. `Scripts/Signals/BucketSignals.cs`

Tạo signals mới:
```csharp
- BucketTappedSignal { Bucket Bucket }
- BucketJumpedToAreaSignal { Bucket Bucket, int AreaIndex }
- BucketCompletedSignal { ColorType Color }
- RowBallReachEntrySignal { RowBall RowBall, int EntryIndex, int ConveyorId }
```

## Files to Modify

### `Scripts/Core/GameConstants.cs`

Thêm `BucketConfig` section với constants từ Cocos.

## Verification

- [ ] All new files compile without errors
- [ ] BucketConfig struct matches Cocos BucketConfig interface
- [ ] Constants match Cocos GameConfig.BUCKET values

## Cocos Reference

```typescript
// GameConfig.ts
BUCKET: {
    DEFAULT_JUMP_HEIGHT: 0,
    DEFAULT_JUMP_DURATION: 0.2,
    COLLECTION_MOVE_UP_OFFSET: 3,
    COLLECTION_ROTATION_DURATION: 0.5,
    COLLECTION_SCALE_DURATION: 0.5,
    SHAKE_SCALE_BUMP: 0.03,
    SHAKE_DURATION: 0.1,
}
```
