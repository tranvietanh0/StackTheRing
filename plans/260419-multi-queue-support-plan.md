# Multi-Queue Support Plan

## Overview
- Extend the current single-queue flow into `N` independent queue lanes, each with its own insert point on the main conveyor.
- Keep `LevelController` as the level runtime owner; add a thin coordinator for lane orchestration instead of pushing multi-lane logic into each existing class.
- Preserve old levels by auto-mapping legacy queue fields into lane `0`.

## Requirements
- Support multiple queue lanes in `LevelData`, each with rings, speed override, enable flag, and stable lane id.
- Allow one `QueueConveyor` + one insert anchor per lane.
- Keep existing main conveyor and bucket logic unchanged as much as possible.
- Existing levels with `HasQueue / QueueRings / QueueSpeed` must still load without manual reauthoring on day one.

## Architecture
- **Data model**: add `QueueLaneData[] QueueLanes` to `LevelData`.
- **Lane config**: each lane stores `LaneId`, `Enabled`, `RingSpawn[] Rings`, `float Speed`, optional `string DisplayName` for editor clarity.
- **Runtime binding**: add a serialized per-lane binding on the level prefab, e.g. `QueueLaneBinding { string LaneId; QueueConveyor QueueConveyor; Transform InsertAnchor; ConveyorFeeder ConveyorFeeder; }`.
- **Coordinator**: add a `MultiQueueCoordinator` owned by `LevelController` that initializes bindings, starts/stops all lanes, exposes `HasPendingRows`, and answers `GetPendingRows()` for gameplay state checks.
- **Feeding rule**: each `ConveyorFeeder` should target its own insert anchor on `ConveyorController` instead of the single global queue insert point.

## Implementation Steps
1. Update `LevelData` with `QueueLaneData[] QueueLanes` and helper properties like `HasAnyQueue`, `TotalQueueRingCount`, `GetActiveQueueLanes()`.
2. Keep legacy fields temporarily; implement normalization so if `QueueLanes` is empty and `HasQueue` is true, runtime treats legacy fields as one generated lane with id `queue-0`.
3. Refactor `QueueConveyor.SetupLevel(...)` to accept lane data instead of reading global queue fields.
4. Refactor `ConveyorFeeder` to work per lane and request insert distance from a lane-specific anchor.
5. Extend `ConveyorController` with an anchor-aware insert query, e.g. `TryGetInsertDistance(Transform anchor, float desiredSpacing, out float insertDistance)`.
6. Add `MultiQueueCoordinator` and let `LevelController` initialize/start/stop/query a list of lane runtimes instead of single `queueConveyor` + `conveyorFeeder` fields.
7. Update `GamePlayState` and `LevelController` to use coordinator aggregate checks for win/lose and pending queue rows.
8. After all prefabs/assets are migrated, remove legacy single-queue fields in a follow-up cleanup change.

## Files to Modify/Create/Delete
- Modify `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- Modify `UnityStackTheRing/Assets/Scripts/StateMachines/Game/States/GamePlayState.cs`
- Create `UnityStackTheRing/Assets/Scripts/Conveyor/MultiQueueCoordinator.cs`
- Create `UnityStackTheRing/Assets/Scripts/Conveyor/QueueLaneBinding.cs` (or keep nested serializable types if preferred)
- Update level prefabs under `UnityStackTheRing/Assets/Prefabs/Levels/`
- Update level assets under `UnityStackTheRing/Assets/Data/Levels/`

## Migration Strategy
- Phase 1: additive migration only; keep legacy queue fields serialized.
- Runtime fallback: if `QueueLanes` is null/empty, build one virtual lane from `HasQueue`, `QueueRings`, `QueueSpeed`.
- Prefab migration: add one lane binding matching old queue objects as `queue-0`, then duplicate bindings for new lanes.
- Asset migration: old levels need no immediate data edits; new levels should author only `QueueLanes`.
- Cleanup: once all shipped levels are resaved with `QueueLanes`, remove legacy fields in a separate low-risk task.

## Prefab Wiring Strategy
- Each queue lane prefab chunk should contain exactly: `QueueConveyor`, `ConveyorFeeder`, row container, spline, transfer exit anchor, and one unique main-conveyor insert anchor reference.
- Store insert anchors on the main conveyor object, one child transform per lane, named by lane id for easy editor matching.
- `LevelController` should serialize lane bindings as an ordered list; validate unique `LaneId`, non-null `QueueConveyor`, `ConveyorFeeder`, and `InsertAnchor` during initialization.
- Do not share one `ConveyorFeeder` across lanes; per-lane feeder keeps logic isolated and avoids conditional branching.

## Testing Strategy
- Unit-ish runtime validation: lane normalization from legacy data, duplicate lane id rejection, aggregate `HasPendingRows` behavior.
- Play mode checks: 0 queues, 1 legacy queue, 2+ queues, mixed speeds, simultaneous ready rows, and empty main conveyor while some lane still has pending rows.
- Prefab validation: broken binding, missing insert anchor, mismatched lane id, and lane count mismatch between `LevelData` and prefab bindings.

## Security Considerations
- No new auth/data exposure surface.
- Add defensive validation to fail fast on malformed lane bindings instead of silently dropping queues.

## Performance Considerations
- Keep one `Update()` per active lane initially; acceptable for small lane counts.
- Avoid repeated LINQ allocations in aggregate pending-row checks inside `GamePlayState`.
- Cap supported lane count in design docs/editor guidance if mobile profiling shows update cost growth.

## Risks & Mitigations
| Risk | Mitigation |
|---|---|
| Lane-to-anchor mismatch causes wrong insert behavior | Validate `LaneId` uniqueness and non-null anchor references in `LevelController.Initialize()` |
| Win/lose logic fires early while another lane still has rows | Centralize pending-row checks inside `MultiQueueCoordinator` and use only that aggregate in `GamePlayState` |
| Migration leaves some prefabs half-converted | Keep legacy fallback for one release and add editor/runtime warnings for legacy-only assets |
| Shared main-conveyor spacing rejects multiple ready lanes unfairly | Feed one row per frame/tick per successful anchor check; log starvation cases during testing |

## TODO Tasks
- [ ] Add multi-lane queue data model to `LevelData`
- [ ] Add legacy-to-lane runtime fallback
- [ ] Make `ConveyorController` anchor-aware for inserts
- [ ] Convert `QueueConveyor` and `ConveyorFeeder` to lane-based setup
- [ ] Add `MultiQueueCoordinator` and wire it through `LevelController`
- [ ] Update `GamePlayState` aggregate queue checks
- [ ] Migrate level prefabs to lane bindings with per-queue insert anchors
- [ ] Run multi-lane regression checks on win/lose/feed behavior
