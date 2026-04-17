# Phase 02 - Rewrite sub conveyor runtime

- Date: 2026-04-17
- Priority: High
- Status: In Progress

## Overview

- Remove hybrid transfer logic.
- Rebuild queue runtime around ready-at-entry model.

## Implementation steps

1. Simplify `PathFollower` sibling cache invalidation for reparent flow.
2. Rewrite `QueueConveyor` to:
   - reverse non-loop path
   - detect ready rows at entry
   - maintain `readyToFill`
   - pop front row cleanly
3. Simplify `ConveyorFeeder` into thin pull loop.
4. Simplify `ConveyorController` into clear-check + insert-from-sub.
5. Remove obsolete queue signals/logic if unused.

## Todo list

- [ ] Add path follower cache invalidation hooks
- [ ] Rewrite queue runtime
- [ ] Rewrite feeder
- [ ] Rewrite main insert helpers
- [ ] Remove dead queue transfer signal logic

## Success criteria

- No reservation/handoff async state remains.
- Queue front waits at entry.
- Main pops front row as soon as fill point clear.

## Risk assessment

- Reparent side effects on spacing cache.
- Wrong entry distance semantics for reverse path.

## Security considerations

- None.

## Next steps

- Rewire prefab.
