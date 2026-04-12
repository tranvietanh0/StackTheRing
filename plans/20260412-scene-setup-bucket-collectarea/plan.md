# Implementation Plan: Scene Setup for Bucket/CollectArea System

**Created:** 2026-04-12
**Status:** Ready for Implementation
**Estimated Effort:** S (~1 day)

## Overview

Setup Unity scene with new Bucket/CollectArea system to replace old Slot/Collector system. Code is already complete - this plan covers scene configuration, prefab creation, and cleanup.

## Prerequisites

- [x] Code complete: Bucket, BucketColumnManager, CollectArea, CollectAreaManager
- [x] Services complete: CollectAreaBucketService, JumpService
- [x] Integration complete: GameManager, GamePlayState, ConveyorController
- [x] MCP connected to Unity Editor

---

## Phase 1: Create Prefabs

**Effort:** S (30 min)
**Owner:** dots-implementer or unity-developer

### Task 1.1: Create Bucket Prefab

Create `Assets/Prefabs/Bucket.prefab` with:
- Root GameObject with `Bucket.cs` component
- Child "Visual" with Cylinder mesh (scale: 0.8, 0.5, 0.8)
- Child "Label_Percent" with TextMeshPro (positioned above)
- Child "Cover" with Cylinder mesh (disabled by default)
- MeshRenderers array assigned in Bucket component

### Task 1.2: Create CollectArea Prefab

Create `Assets/Prefabs/CollectArea.prefab` with:
- Root GameObject with `CollectArea.cs` component
- Child "Visual" with Plane/Quad mesh (scale: 1.2, 1, 1.2)
- Semi-transparent material for visual indicator

---

## Phase 2: Scene Setup

**Effort:** S (20 min)
**Owner:** dots-environment or unity-developer
**Blocked by:** Phase 1

### Task 2.1: Add BucketColumnManager to Scene

- Create empty GameObject "BucketColumnManager"
- Add `BucketColumnManager.cs` component
- Create child "BucketContainer" transform
- Position at (0, 0, 5) for bucket grid area

### Task 2.2: Add CollectAreaManager to Scene

- Create empty GameObject "CollectAreaManager"
- Add `CollectAreaManager.cs` component
- Create child "AreaContainer" transform
- Position at (0, 0, -3) for collect area row
- Assign CollectArea prefab reference

### Task 2.3: Update GameManager References

- Assign `bucketColumnManager` reference
- Assign `collectAreaManager` reference
- Assign `bucketPrefab` reference
- Verify `conveyorController`, `ballPrefab`, `rowBallPrefab` still assigned

---

## Phase 3: Cleanup Old Systems

**Effort:** S (15 min)
**Owner:** dots-implementer
**Blocked by:** Phase 2

### Task 3.1: Delete Old GameObjects from Scene

Remove from MainScene:
- SlotManager
- CollectorPanel
- AttractionController

### Task 3.2: Delete Old Code Files

Remove files:
- `Assets/Scripts/Slot/Slot.cs`
- `Assets/Scripts/Slot/SlotManager.cs`
- `Assets/Scripts/Slot/ColorCollector.cs`
- `Assets/Scripts/Slot/CollectorPanel.cs`
- `Assets/Scripts/Attraction/AttractionController.cs`
- `Assets/Scripts/Attraction/AttractionConfig.cs`

Also delete corresponding .meta files.

### Task 3.3: Update Assembly References

- Remove deleted files from any asmdef references
- Check MainSceneScope for old signal declarations (keep for now, mark deprecated)

---

## Phase 4: Level Configuration

**Effort:** S (10 min)
**Owner:** dots-implementer
**Blocked by:** Phase 2

### Task 4.1: Update Level_01 BucketColumns

Configure in `Assets/Data/Levels/Level_01.asset`:
```
BucketColumns:
  [0]: BucketColors = [Red, Blue]
  [1]: BucketColors = [Yellow, Green]
  [2]: BucketColors = [Red, Yellow]
  
BucketColumnSpacing: 1.2
BucketRowSpacing: 1.2
```

### Task 4.2: Verify Rings Match Buckets

Ensure Rings config matches bucket capacity:
- Red: 10 balls (2 buckets)
- Blue: 5 balls (1 bucket)
- Yellow: 10 balls (2 buckets)
- Green: 5 balls (1 bucket)

---

## Phase 5: Validation

**Effort:** S (15 min)
**Owner:** dots-validator
**Blocked by:** Phase 3, Phase 4

### Task 5.1: Verify Compilation

- Check Unity console for errors
- Ensure all scripts compile

### Task 5.2: Play Mode Test

- Enter Play mode
- Verify buckets spawn in grid
- Verify collect areas spawn
- Verify conveyor starts with balls
- Tap bucket → jumps to collect area
- Balls auto-collect when row reaches entry point
- Bucket completes → animation plays → bucket destroyed
- All buckets complete → Win state

---

## Risk Assessment

| Risk | Likelihood | Impact | Score | Mitigation |
|------|------------|--------|-------|------------|
| Prefab references lost after delete | 2 | 3 | 6 | Save scene after each phase |
| Entry point detection fails | 2 | 4 | 8 | Verify ConveyorController.entryNodes assigned |
| Bucket tap not working | 3 | 3 | 9 | Need input handler (Phase 2 scope) |

## Timeline

| Phase | Effort | Notes |
|-------|--------|-------|
| Phase 1: Prefabs | S (30m) | MCP prefab creation |
| Phase 2: Scene Setup | S (20m) | MCP scene modification |
| Phase 3: Cleanup | S (15m) | File deletion + scene cleanup |
| Phase 4: Level Config | S (10m) | ScriptableObject modification |
| Phase 5: Validation | S (15m) | Play mode testing |
| **Total** | **~1.5h** | Critical path: 1→2→3/4→5 |

---

## Handoff

After plan approval, run:
```
/t1k:cook plans/20260412-scene-setup-bucket-collectarea
```
