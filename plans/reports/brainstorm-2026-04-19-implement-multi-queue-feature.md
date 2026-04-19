# Brainstorm - implement multi queue feature

## Problem

- User wants true multiple waiting queues.
- Current architecture is hard singleton.

## Current blockers

- `LevelData` only stores one queue config.
- `LevelController` only owns one `QueueConveyor` and one `ConveyorFeeder`.
- `GamePlayState` start/stop/win/lose only checks one queue.
- `ConveyorFeeder` only feeds from one queue.
- `ConveyorController` only has one queue insert anchor.

## Brutal truth

- This is not a small feature.
- It is a refactor across data model, runtime ownership, state checks, and prefab conventions.
- If implemented as patches on top of singleton fields, it will become jam/race hell.

## Options

### A. Fake multi-queue
- 1 real queue, others visual only
- Fast
- Not real gameplay feature

### B. True multi-queue, shared insert point
- Needs arbitration coordinator
- Works
- Highest jam/race risk

### C. True multi-queue, one insert point per queue
- Recommended
- Cleaner ownership
- Less contention
- Easier to debug and tune

## Recommendation

- Implement **true multi-queue with one insert point per queue**.
- Add a thin coordinator.
- Keep queue lanes independent.

## Architecture

### Data model

Replace singleton queue fields with:

```text
QueueLaneData[] QueueLanes
- QueueRings
- QueueSpeed
- LaneId
- Optional priority
```

### Prefab/runtime binding

Add `QueueLaneBinding[]` on level prefab/runtime:

```text
QueueLaneBinding
- QueueConveyor queue
- ConveyorFeeder feeder
- Transform insertAnchor
```

### Ownership

- `QueueConveyor` owns one lane only.
- `ConveyorFeeder` should become lane-local or be replaced by one `MultiQueueCoordinator`.
- `ConveyorController` should expose `TryGetSubInsertDistance(Transform insertAnchor, float spacing, out float distance)`.

### State / checks

- `GamePlayState` must iterate all queues for win/lose.
- `LevelController` must initialize all lanes.

## Implementation strategy

1. Add `QueueLaneData[]` with migration fallback from old single queue fields.
2. Add `QueueLaneBinding[]` in `LevelController`.
3. Refactor `ConveyorController` to support per-call insert anchor.
4. Add `MultiQueueCoordinator` to arbitrate feed order.
5. Update `GamePlayState` to aggregate all queues.
6. Migrate `Level_03+` first. Keep old levels on compatibility path.

## Risks

- Highest risk: shared insert contention if trying to keep old feeder model.
- Second risk: forget to update lose/win checks, causing false win/lose.
- Third risk: prefab authoring complexity if anchors are not explicit.

## Success criteria

- 2+ queues active in one level.
- Each queue waits/feed independently.
- No queue jam due to cross-lane contention.
- Win/lose checks remain correct.

## Next steps

1. Decide migration scope: new levels only vs retrofit old queue levels.
2. Implement per-queue insert anchor support first.
3. Then build coordinator.

## Unresolved questions

- Do you need deterministic priority between queues, or round-robin is enough?
- Should multiple queues be allowed to feed in same frame if they have separate insert anchors?
