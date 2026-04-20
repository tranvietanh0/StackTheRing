# LevelData Odin Schema Refactor Plan

## Overview
- Refactor `LevelData` to an Odin-friendly authoring model where bucket layout is edited as a 2D grid and runtime behavior stays playable throughout the transition.
- Make the bucket grid the single durable source of truth, while keeping one short-lived compatibility path from legacy `BucketColumns` during migration.
- Remove legacy bucket/queue fields only after assets and prefabs are migrated and verified.

## Requirements
- Keep current runtime flow working during refactor; no broken level loading or bucket spawning.
- Replace `BucketColumns` as the primary authoring model with an Odin-friendly 2D schema.
- Preserve bucket spacing behavior and current column-major runtime semantics.
- Support safe migration of existing level assets, including empty-cell handling.
- Remove unused legacy pieces only when runtime and assets no longer depend on them.

## Architecture
- **LevelData authoring**: change `LevelData` from `ScriptableObject` to Odin-backed `SerializedScriptableObject` and expose `BucketGrid` as a 2D matrix for editing.
- **Cell model**: add a dedicated bucket cell enum/value type with an explicit `Empty` sentinel instead of overloading `ColorType`.
- **Runtime adapter**: centralize bucket iteration behind one helper on `LevelData`, e.g. `EnumerateBucketCells()` or `GetBucketLayout()`, so `BucketColumnManager` stops knowing about legacy/new storage details.
- **Migration boundary**: keep legacy `BucketColumns` hidden and read-only during transition; migrate assets into `BucketGrid`, then delete legacy fields in a final cleanup pass.
- **Queue cleanup**: keep `QueueLanes` as the queue source of truth; remove `HasQueue`, `QueueRings`, and `QueueSpeed` only after confirming all authored levels no longer rely on legacy fallback.
- **Assembly impact**: update `HyperCasualGame.Scripts.asmdef` if direct `Sirenix.OdinInspector` / `Sirenix.Serialization` references are required by the main gameplay assembly.

## Phases

### Phase 1 - Additive schema and compatibility
1. Update `LevelData` to support Odin serialization and introduce `BucketGrid` plus helper APIs for width, height, and bucket enumeration.
2. Keep `BucketColumns` serialized but hidden; mark it as temporary migration-only data.
3. Add editor-only validation/migration utilities on `LevelData` to convert `BucketColumns` into `BucketGrid` with deterministic axis rules: `grid[col, row]`.
4. Refactor `BucketColumnManager` to consume the new helper API instead of directly reading `BucketColumns`.
5. Keep queue compatibility unchanged in this phase; do not remove queue legacy fields yet.

### Phase 2 - Asset migration and runtime verification
1. Batch-migrate all `Level_*.asset` instances under both `UnityStackTheRing/Assets/Data/Levels/` and `UnityStackTheRing/Assets/Resources/Levels/`.
2. Re-save any level prefabs that embed or reference updated `LevelData` so inspector/runtime metadata stays in sync.
3. Validate per-level parity: bucket count, bucket color counts, column count, row count, and empty-cell placement.
4. Run play-mode smoke checks on at least one no-queue level and one queued level to ensure target-ball count distribution and collect-area behavior remain unchanged.

### Phase 3 - Legacy cleanup
1. Remove `BucketColumns` and the migration shim only after all assets are converted and verified.
2. Audit queue assets for `QueueLanes` adoption; if no remaining levels use legacy queue fallback, remove `HasQueue`, `QueueRings`, and `QueueSpeed` from `LevelData` and simplify `GetActiveQueueLanes()`.
3. Remove dead comments/usings/docs that still describe bucket config as column arrays or queue config as singleton legacy fields.

## Migration Order
1. Add `BucketGrid` + Odin serialization support.
2. Add one centralized compatibility reader so runtime can read new data first and legacy data only as fallback.
3. Refactor `BucketColumnManager` to consume the centralized reader.
4. Migrate all level assets and verify parity.
5. Remove `BucketColumns`.
6. Remove legacy queue fallback fields only if asset audit proves `QueueLanes` is fully adopted.

## Files to Modify/Create/Delete
- Modify `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Bucket/BucketColumnManager.cs`
- Modify `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs` for logging/queue wording if it still reports legacy queue state
- Modify `UnityStackTheRing/Assets/Scripts/HyperCasualGame.Scripts.asmdef` if Odin references are needed
- Potentially create `UnityStackTheRing/Assets/Scripts/Level/Editor/LevelDataEditorUtilities.cs` if migration/validation code should stay editor-only instead of living inside `LevelData`
- Update level assets in `UnityStackTheRing/Assets/Data/Levels/`
- Update level assets in `UnityStackTheRing/Assets/Resources/Levels/`
- Update affected level prefabs in `UnityStackTheRing/Assets/Prefabs/Levels/` if they serialize stale references or embedded data
- Update docs: `docs/codebase-summary.md`, `docs/system-architecture.md`, and any level-authoring note chosen for Odin workflow

## Testing Strategy
- **Schema tests**: verify `BucketGrid` width/height, empty-cell handling, and deterministic conversion from `BucketColumns`.
- **Runtime parity**: verify identical spawned bucket count, per-color bucket totals, per-bucket target count distribution, and eligible-bucket ordering.
- **Regression checks**: verify queue levels still compute total ball counts correctly while bucket refactor is in place.
- **Editor checks**: verify Odin matrix displays correct axis orientation and migrated assets stay stable after save/reopen.

## Security Considerations
- No auth or external-data impact.
- Keep migration/editor tools local-only and deterministic; avoid silent destructive cleanup before parity checks pass.

## Performance Considerations
- Prefer one precomputed enumeration path from `LevelData` to avoid repeated conversion/allocation in `BucketColumnManager`.
- Avoid long-lived dual-schema support; remove fallback quickly after migration to prevent extra branching and maintenance cost.

## Risks & Mitigations
| Risk | Likelihood | Impact | Mitigation |
|---|---:|---:|---|
| Odin types fail to compile in main assembly | 3 | 4 | Confirm asmdef references up front and use existing Odin setup from submodules as reference |
| Grid axes are flipped in authoring vs runtime | 4 | 4 | Lock convention to `grid[col, row]`, label it in code/docs, and test one known level layout before batch migration |
| Partial migration leaves split-brain data | 3 | 5 | Keep one short migration window, batch-convert all level assets, then remove legacy fields promptly |
| Queue cleanup removes still-used fallback fields | 2 | 4 | Treat queue field removal as separate final gate after asset audit, not part of initial bucket refactor |

## Timeline
| Phase | Effort | Notes |
|---|---|---|
| Phase 1: Additive schema and compatibility | M (3d) | Main coding risk is Odin serialization + adapter shape |
| Phase 2: Asset migration and runtime verification | S (1d) | Depends on stable migration tool and quick parity checks |
| Phase 3: Legacy cleanup | S (1d) | Only proceed after asset audit and smoke verification |
| Total | M (about 1 week) | Critical path: Phase 1 -> Phase 2 -> Phase 3 |

## Docs Impact
- Update architecture docs to describe bucket layout as an Odin-authored 2D grid, not `BucketColumns[]`.
- Update onboarding/codebase summary to note `LevelData` now uses Odin serialization and queue legacy fields are transitional or removed.
- Add a short authoring note for designers: axis convention, `Empty` cell semantics, and when to use migration tooling.

## TODO Tasks
- [ ] Convert `LevelData` to Odin-friendly serialized schema with `BucketGrid`
- [ ] Add centralized bucket layout adapter and migration utility
- [ ] Refactor `BucketColumnManager` to use adapter instead of `BucketColumns`
- [ ] Batch-migrate all level assets and verify parity
- [ ] Remove `BucketColumns` after successful migration
- [ ] Audit and remove legacy queue fallback fields if `QueueLanes` fully replaces them
- [ ] Update docs for new level authoring workflow
