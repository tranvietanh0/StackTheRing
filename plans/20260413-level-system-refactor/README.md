# Plan: Level System Refactor

**Created:** 2026-04-13
**Status:** Ready for Implementation
**Estimated Effort:** M (2-3 days)

## Overview

Refactor Level System để hỗ trợ:
- Mỗi level = 1 prefab riêng + 1 LevelData SO riêng
- GameManager → LevelController (đổi tên)
- Load level prefab qua Addressables
- MainScene chỉ chứa levelRoot spawn point

## Current State

```
1.MainScene
├── GameManager (MonoBehaviour)
│   ├── [Ref] ConveyorController
│   ├── [Ref] BucketColumnManager
│   └── [Ref] CollectAreaManager
├── ConveyorController
├── BucketColumnManager
└── CollectAreaManager
```

## Target State

```
1.MainScene
└── LevelRoot (empty Transform, spawn point)

Addressables/
├── Level_01.prefab
│   ├── LevelController (root)
│   │   └── [Ref] LevelData_01
│   ├── ConveyorController
│   ├── BucketColumnManager
│   └── CollectAreaManager
├── Level_02.prefab
│   └── ...
└── LevelData_01.asset (embedded in prefab or separate)
```

## Phases

| Phase | Name | Files | Effort | Dependencies |
|-------|------|-------|--------|--------------|
| 1 | Rename GameManager → LevelController | 4 | S | None |
| 2 | Create Level Prefab Structure | 2 | S | Phase 1 |
| 3 | Update LevelManager (Load Prefab) | 2 | M | Phase 2 |
| 4 | Update MainScene & Scope | 3 | S | Phase 3 |
| 5 | Addressables Setup | 2 | S | Phase 4 |

## Risk Assessment

| Risk | L | I | Score | Mitigation |
|------|---|---|-------|------------|
| Prefab references break | 4 | 4 | 16 | Backup scene, test từng phase |
| DI injection fails for LevelController | 3 | 4 | 12 | Manual inject via callback |
| Addressables load fails | 2 | 3 | 6 | Fallback to Resources |
| Scene hierarchy changes break gameplay | 3 | 3 | 9 | Test gameplay after mỗi phase |

## Success Criteria

- [ ] GameManager renamed to LevelController (no compile errors)
- [ ] Level_01.prefab created với đầy đủ components
- [ ] LevelManager load prefab từ Addressables thành công
- [ ] LevelController được inject dependencies đúng
- [ ] Gameplay hoạt động như cũ (conveyor, bucket, collect)
- [ ] Có thể tạo Level_02.prefab từ Level_01 template

## Commands

```bash
# Execute full plan
/t1k:cook plans/20260413-level-system-refactor

# Execute specific phase
/t1k:cook plans/20260413-level-system-refactor/phase-01-rename.md
```
