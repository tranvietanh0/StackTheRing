# Phase 1: Data Layer

**Effort:** S (1 day)
**Dependencies:** None
**Blocks:** Phase 2, 3, 4

## Objective

Create data structures and configuration for StackTheRing game.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Data/ColorType.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Data/StackTheRingConfig.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Data/BucketConfig.cs` | CREATE | this phase |

## Implementation

### 1. ColorType.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Data
{
    public enum ColorType
    {
        Red,
        Blue,
        Green,
        Yellow,
        Orange,
        Purple,
        Pink,
        Cyan,
        Brown,
        Mint,
        Silver,
        DarkOrange
    }
}
```

### 2. StackTheRingConfig.cs

ScriptableObject for game constants (editable in Unity Inspector).

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Data
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "StackTheRingConfig", menuName = "StackTheRing/Config")]
    public class StackTheRingConfig : ScriptableObject
    {
        [Header("Ball Settings")]
        public int BallsPerRow = 5;
        public float BallJumpHeight = 1f;
        public float BallJumpDuration = 0.2f;
        public float BallJumpDelay = 0.05f;

        [Header("Bucket Settings")]
        public int DefaultTargetBallCount = 100;
        public float BucketJumpHeight = 0f;
        public float BucketJumpDuration = 0.2f;

        [Header("Conveyor Settings")]
        public float ConveyorSpeed = 1f;
        public float FillPointThreshold = 0.3f;
        public float EntryTriggerThreshold = 0.3f;

        [Header("Collect Area")]
        public float CollectAreaSpacing = 1.15f;
    }
}
```

### 3. BucketConfig.cs

Data class for bucket configuration (used by Blueprint loader).

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Data
{
    using System;

    [Serializable]
    public class BucketConfig
    {
        public int Index;
        public int Row;
        public int Column;
        public ColorType Color;
        public int TargetBallCount;

        public BucketConfig(int index, int row, int column, ColorType color, int targetBallCount)
        {
            Index = index;
            Row = row;
            Column = column;
            Color = color;
            TargetBallCount = targetBallCount;
        }
    }

    [Serializable]
    public class RowConfig
    {
        public int RowId;
        public ColorType[] BallColors;

        public RowConfig(int rowId, ColorType[] ballColors)
        {
            RowId = rowId;
            BallColors = ballColors;
        }
    }

    [Serializable]
    public class LevelData
    {
        public int LevelId;
        public BucketConfig[] Buckets;
        public RowConfig[] InitialRows;
        public int ConveyorType;
    }
}
```

## Verification

- [ ] All 3 files compile without errors
- [ ] StackTheRingConfig can be created via Unity menu
- [ ] ColorType enum has 12 colors matching Cocos source

## Notes

- ColorType uses PascalCase (Unity convention) vs snake_case in Cocos
- StackTheRingConfig is ScriptableObject for Inspector editing
- BucketConfig/RowConfig/LevelData are serializable for Blueprint loading
