# Phase 2: Create Level Prefab Structure

**Effort:** S (< 1 day)
**Dependencies:** Phase 1

## Objective

Tạo Level_01.prefab chứa tất cả gameplay components, extract từ MainScene.

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `Prefabs/Levels/Level_01.prefab` | Create | Level prefab từ scene hierarchy |
| `Scripts/Level/LevelController.cs` | Update | Add SerializeField cho LevelData |

## Prefab Structure

```
Level_01 (root GameObject)
├── LevelController (component on root)
│   ├── [SerializeField] LevelData levelData
│   ├── [SerializeField] ConveyorController conveyorController
│   ├── [SerializeField] BucketColumnManager bucketColumnManager
│   └── [SerializeField] CollectAreaManager collectAreaManager
│
├── Conveyor/ (child)
│   └── ConveyorController
│       ├── SplineComputer
│       ├── RowBallContainer
│       └── EntryNodes
│
├── Buckets/ (child)
│   └── BucketColumnManager
│       └── BucketContainer
│
└── CollectAreas/ (child)
    └── CollectAreaManager
        └── AreaContainer
```

## Implementation Steps

### Step 1: Update LevelController - Add LevelData Reference

```csharp
public class LevelController : MonoBehaviour, IInitializable
{
    [Header("Level Config")]
    [SerializeField] private LevelData levelData;
    
    [Header("References")]
    [SerializeField] private ConveyorController conveyorController;
    [SerializeField] private BucketColumnManager bucketColumnManager;
    [SerializeField] private CollectAreaManager collectAreaManager;
    
    // Remove old prefab references (moved to config or managers)
    // [SerializeField] private Ball ballPrefab; → ConveyorConfig
    // [SerializeField] private RowBall rowBallPrefab; → ConveyorConfig
    // [SerializeField] private Bucket bucketPrefab; → BucketColumnManager
    
    public LevelData LevelData => this.levelData;
}
```

### Step 2: Create Folder Structure

```
Assets/
├── Prefabs/
│   └── Levels/
│       └── Level_01.prefab
└── Resources/
    └── Levels/
        └── LevelData_01.asset (đã có)
```

### Step 3: Create Prefab từ Scene (Unity Editor)

1. Trong Scene hierarchy, tạo empty GameObject "Level_01"
2. Drag LevelController, ConveyorController, BucketColumnManager, CollectAreaManager vào làm children
3. Reorganize hierarchy theo structure ở trên
4. Assign references trong Inspector
5. Drag Level_01 vào Prefabs/Levels/ để tạo prefab
6. Assign LevelData_01 SO vào LevelController.levelData

### Step 4: Move Prefab References to Appropriate Locations

```csharp
// ConveyorController already has:
[SerializeField] private RowBall rowBallPrefab;
[SerializeField] private Ball ballPrefab;

// BucketColumnManager already has:
[SerializeField] private Bucket bucketPrefab;

// CollectAreaManager already has:
[SerializeField] private CollectArea collectAreaPrefab;
```

## Validation

- Prefab tạo thành công, không missing references
- Có thể instantiate prefab trong scene
- LevelData được assign đúng

## Checklist

- [ ] Folder Prefabs/Levels/ created
- [ ] Level_01.prefab created
- [ ] LevelController has levelData field
- [ ] All component references assigned
- [ ] LevelData_01 assigned to prefab
- [ ] No missing references in prefab
