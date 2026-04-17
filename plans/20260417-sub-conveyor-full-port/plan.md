# Sub Conveyor Full Port

- Status: In Progress
- Goal: rewrite Unity sub conveyor flow to match Cocos behavior, remove obsolete hybrid queue logic, and rewire `Level_02` prefab.

## Phases

1. [Phase 01 - Research and architecture](phase-01-research-and-architecture.md) - Done
2. [Phase 02 - Rewrite sub conveyor runtime](phase-02-rewrite-sub-conveyor-runtime.md) - In Progress
3. [Phase 03 - Rewire Level_02 prefab](phase-03-rewire-level-02-prefab.md) - Planned
4. [Phase 04 - Validate, test, review](phase-04-validate-test-review.md) - Planned

## Scope

- `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/PathFollower.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs`
- `UnityStackTheRing/Assets/Scripts/Signals/GameSignals.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/Main/MainSceneScope.cs`
- `UnityStackTheRing/Assets/Resources/Levels/Level_02.prefab`
- `UnityStackTheRing/Assets/Prefabs/Levels/Level_02.prefab`

## Success

- Sub conveyor behaves like Cocos: wait at entry, main pops front row, next rows slide/follow up.
- No reservation/handoff hybrid logic left.
- `Level_02` wired to explicit sub entry / main fill anchors.
- Unity compile passes, tests pass, code review has 0 critical.
