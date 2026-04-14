# Ring Landing Effect Implementation Plan

**Created**: 2026-04-13
**Status**: Planning
**Approach**: DOTween Sequence

## Overview

Implement realistic ring landing effect when balls fly into bucket (ring toss pole). Includes:
1. Visible ring stacking on pole
2. Wobble animation (damped oscillation)
3. Sparkle VFX burst on landing

## Current State

```
Ball.JumpToBucket()
  -> JumpService.JumpToDestination() (arc trajectory)
  -> targetBucket.AddBall(ball)
  -> ball.gameObject.SetActive(false)  <- Ring disappears immediately
```

**Problem**: No visual feedback on landing, rings invisible after collection.

## Target State

```
Ball.JumpToBucket()
  -> JumpService.JumpToDestination() (arc trajectory)
  -> RingLandingEffect.PlayLanding()
      -> Wobble animation (DOTween damped sine)
      -> Sparkle VFX burst
      -> Position at stack slot
  -> Bucket manages visible ring stack
```

## Phases

| Phase | Name | Effort | Dependencies |
|-------|------|--------|--------------|
| 1 | Ring Stacking System | S (1d) | None |
| 2 | Wobble Animation | S (1d) | Phase 1 |
| 3 | Sparkle VFX | S (1d) | Phase 1 |
| 4 | Integration & Polish | S (1d) | Phase 2, 3 |

**Total**: ~4 days (can parallelize Phase 2 & 3)

## Files Changed

| File | Change Type |
|------|-------------|
| `Scripts/Ring/Ball.cs` | Modify - add landing effect trigger |
| `Scripts/Bucket/Bucket.cs` | Modify - ring stack management |
| `Scripts/Core/GameConstants.cs` | Modify - add landing effect config |
| `Scripts/Effects/RingLandingEffect.cs` | **New** - wobble + settle logic |
| `Scripts/Effects/SparkleEffect.cs` | **New** - VFX controller |
| `Prefabs/Effects/RingSparkle.prefab` | **New** - Particle System |

## Risk Assessment

| Risk | Likelihood | Impact | Score | Mitigation |
|------|------------|--------|-------|------------|
| Wobble timing feels wrong | 3 | 2 | 6 | Expose all params in config, iterate |
| Performance with many rings | 2 | 3 | 6 | Object pooling for particles |
| Ring clipping through pole | 2 | 2 | 4 | Proper Y offset calculation |
| Stack height overflow | 1 | 2 | 2 | Cap visible rings, fade old ones |

## Success Criteria

- [ ] Rings visible on pole after landing
- [ ] Wobble animation looks natural (3-4 oscillations, damped)
- [ ] Sparkle burst plays on landing
- [ ] No performance regression with 20+ rings
- [ ] Smooth integration with existing jump flow
