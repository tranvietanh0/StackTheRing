# Phase 3: CollectArea System

**Effort:** S (0.5 day)
**Dependencies:** Phase 1
**Blocking:** Phase 5, 6

## Objective

Tạo CollectArea và CollectAreaManager theo Cocos architecture.

## Files to Create

### 1. `Scripts/CollectArea/CollectArea.cs` (~60 LOC)

Simple slot để bucket nhảy vào.

**Properties:**
```csharp
- bool isOccupied
- Bucket occupyingBucket
```

**Methods (match Cocos CollectArea.ts):**
```csharp
- Occupy(Bucket bucket)
- Release()
- Reset()
```

### 2. `Scripts/CollectArea/CollectAreaManager.cs` (~80 LOC)

**Properties:**
```csharp
- CollectArea collectAreaPrefab
- Transform collectAreaContainer
- float areaSpacing = 1.15f // from Cocos COLLECT_AREA.SPACING
```

**Methods (match Cocos CollectAreaManager.ts):**
```csharp
// Query
- GetListCollectArea() → List<CollectArea>
- AreAllCollectAreasOccupied() → bool
- GetFirstEmptyArea() → CollectArea

// Setup
- SpawnAreas(int count)
- ClearContainer()
- Cleanup()
```

## Files to Modify

### `Scripts/Scenes/Main/MainSceneScope.cs`

Register CollectAreaManager:
```csharp
builder.RegisterComponentInHierarchy<CollectAreaManager>();
```

## Prefab Requirements

- Create `CollectArea` prefab với:
  - Visual indicator (transparent platform hoặc highlight)
  - Không cần collider (bucket tự bay vào)

## Verification

- [ ] CollectArea.Occupy() sets isOccupied = true
- [ ] CollectArea.Release() sets isOccupied = false
- [ ] CollectAreaManager.GetFirstEmptyArea() returns correct area
- [ ] CollectAreaManager.AreAllCollectAreasOccupied() returns true when all full

## Cocos Reference

`CollectArea.ts` - very simple, ~65 lines
`CollectAreaManager.ts` - ~50 lines
