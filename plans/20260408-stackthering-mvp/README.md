# StackTheRing MVP Implementation Plan

**Created:** 2026-04-08
**Status:** Ready for Implementation
**Source:** Cocos BeadsOutRemasterPLA conversion

## Overview

Convert Cocos Creator "BeadsOut" game to Unity 6 as "StackTheRing" following existing project architecture (VContainer, UniTask, MessagePipe, DOTween).

## MVP Scope

**IN SCOPE:**
- Core gameplay loop (balls → conveyor → bucket collection)
- Single level type (ellipse conveyor)
- Touch/tap input
- Win condition + basic UI

**OUT OF SCOPE (Phase 2+):**
- Multiple level types
- Tutorial system
- Sound/VFX polish
- Monetization
- Analytics

## Phases

| Phase | Name | Files | Effort |
|-------|------|-------|--------|
| 1 | Data Layer | 3 | S |
| 2 | Signals | 1 | S |
| 3 | Services | 4 | M |
| 4 | Objects | 4 | M |
| 5 | Conveyor | 3 | M |
| 6 | Core Logic | 3 | M |
| 7 | DI Integration | 2 | S |
| 8 | Game States | 2 | S |
| 9 | UI Screens | 2 | S |
| 10 | Scene Setup | - | M |

**Total Effort:** ~2 weeks

## Risk Assessment

| Risk | L | I | Score | Mitigation |
|------|---|---|-------|------------|
| DOTween path complexity | 2 | 3 | 6 | Start simple, upgrade to Splines later |
| Physics raycast perf | 2 | 2 | 4 | Use layer masks, optimize |
| Blueprint CSV parsing | 3 | 2 | 6 | Test with sample data early |
| Object pooling memory | 2 | 3 | 6 | Use existing ObjectPoolManager |

## Quick Start

```bash
# After each phase, run:
/t1k:cook plans/20260408-stackthering-mvp/phase-{N}-*.md
```

## Files Index

- `phase-01-data-layer.md`
- `phase-02-signals.md`
- `phase-03-services.md`
- `phase-04-objects.md`
- `phase-05-conveyor.md`
- `phase-06-core-logic.md`
- `phase-07-di-integration.md`
- `phase-08-game-states.md`
- `phase-09-ui-screens.md`
- `phase-10-scene-setup.md`
