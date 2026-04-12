# Plan: Refactor Gameplay Logic theo Cocos Architecture

**Created:** 2026-04-10
**Status:** Ready for Implementation
**Estimated Effort:** M (3-4 days)

## Overview

Refactor hệ thống Slot/ColorCollector sang Bucket/CollectArea architecture theo Cocos reference, bao gồm:
- Bucket system (thay thế Slot + ColorCollector)
- CollectArea system (mới)
- JumpService (mới)
- Entry-point based attraction (thay thế progress-based)

## Reference

- **Cocos Source:** `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA\assets\scripts\CocosBeadsOutRemasterPLA`
- **Key Files:** `Bucket.ts`, `CollectArea.ts`, `GridBucketManager.ts`, `JumpService.ts`, `MainConveyorController.ts`

## Phases

| Phase | Name | Files | Effort | Dependencies |
|-------|------|-------|--------|--------------|
| 1 | Core Data Structures | 3 | S | None |
| 2 | Bucket System | 2 | M | Phase 1 |
| 3 | CollectArea System | 2 | S | Phase 1 |
| 4 | JumpService | 1 | S | None |
| 5 | Attraction Refactor | 2 | M | Phase 2, 3, 4 |
| 6 | Integration & Cleanup | 4 | M | Phase 5 |

## Risk Assessment

| Risk | L | I | Score | Mitigation |
|------|---|---|-------|------------|
| Breaking existing gameplay | 3 | 5 | 15 | Backup scene, incremental testing |
| Prefab references break | 4 | 3 | 12 | Update prefabs after each phase |
| Signal subscription leaks | 2 | 4 | 8 | Review cleanup in OnDestroy |
| Animation timing issues | 3 | 2 | 6 | Match Cocos timing constants |

## Success Criteria

- [ ] Bucket tap → jump to CollectArea works
- [ ] Ball attraction triggers at entry point
- [ ] Ball jumps to correct Bucket (color match)
- [ ] Bucket completes when full (animation + destroy)
- [ ] Win/Lose conditions work correctly
- [ ] No memory leaks (signal cleanup)

## Commands

```bash
# Execute plan
/t1k:cook plans/20260410-cocos-refactor

# Execute specific phase
/t1k:cook plans/20260410-cocos-refactor/phase-01-core-data.md
```
