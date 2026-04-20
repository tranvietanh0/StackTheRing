# Research Report: LevelData bucket config -> Odin-friendly schema

Conducted: 2026-04-20

## Executive Summary

Best fit here: keep runtime model simple, author with a fixed-size 2D bucket grid on `LevelData`, and let Odin render it with `TableMatrix`. For this project, bucket layout is semantically a rectangular grid already: runtime spawns by column count + per-row traversal, and spacing is already `column`/`row` driven in `BucketColumnManager`.

Do not keep `BucketColumn[]` as primary authoring model. It is runtime-shaped, not designer-shaped. Also do not over-engineer with nested polymorphic cell configs. Use one enum cell type plus a tiny compatibility/migration layer from legacy columns.

## Repo Findings

- Current authoring model is `LevelData.BucketColumns : BucketColumn[]`, each column holds `ColorType[] BucketColors` in `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs:29` and `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs:127`.
- Runtime treats it as a true grid already: outer loop = column, inner loop = row in `UnityStackTheRing/Assets/Scripts/Bucket/BucketColumnManager.cs:92`.
- Current level assets serialize buckets as per-column arrays, eg `Level_01.asset` and `Level_09.asset`, which is compact but poor for visual editing in Inspector.
- Likely legacy fields around queue already exist: `HasQueue`, `QueueRings`, `QueueSpeed` are fallback compatibility fields, while new path is `QueueLanes` in `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs:44` and `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs:84`.

## External Findings

- Unity default serializer does not support multidimensional arrays, jagged arrays, or nested containers directly. It only supports arrays / `List<T>` of serializable element types. Source: Unity serialization rules, `https://docs.unity3d.com/Manual/script-serialization-rules.html`.
- Odin `TableMatrix` is explicitly meant to draw two-dimensional arrays (`T[,]`). Source: Sirenix TableMatrix docs, `https://odininspector.com/attributes/table-matrix-attribute`.
- Odin serialization must be opted into explicitly, typically by inheriting `SerializedScriptableObject` or implementing `ISerializationCallbackReceiver`. Source: Sirenix serializer docs, `https://odininspector.com/tutorials/serialize-anything/implementing-the-odin-serializer`.

## Recommendation

### Preferred schema

Use `LevelData : SerializedScriptableObject` and make bucket authoring use one Odin-serialized 2D grid as source of truth.

Suggested shape:

```csharp
public enum BucketCell
{
    Empty = -1,
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    // ... map to existing ColorType values or wrap them
}

public sealed class LevelData : SerializedScriptableObject
{
    [OdinSerialize]
    [TableMatrix(SquareCells = true, ResizableColumns = false, Transpose = true)]
    public BucketCell[,] BucketGrid;

    [SerializeField]
    private float bucketColumnSpacing = 1.2f;

    [SerializeField]
    private float bucketRowSpacing = 1.2f;
}
```

Practical nuance: if `ColorType` already has no `None/Empty`, add one sentinel entry or use a dedicated `BucketCell` enum instead of stuffing empties into `ColorType`. Safer, clearer, less validator pain.

### Why this is best

- Matches mental model of level designers: edit cells, not nested arrays.
- Matches runtime model closely enough: runtime can iterate `[col, row]` with trivial conversion.
- Stable schema: one source of truth, no duplicated derived lists.
- Low ceremony: no custom node graph, no ScriptableObject-per-column, no polymorphism.

## Jagged vs TableMatrix / true 2D

Recommendation: use real 2D + `TableMatrix`, not jagged rows.

Why:

- `BucketColumns` already implies fixed grid semantics, not free-form row lengths.
- Jagged rows remain structurally awkward in Inspector, even with Odin helpers.
- Jagged shape encourages inconsistent widths, which runtime does not seem to need.
- `TableMatrix` gives immediate visual authoring value; that is the main goal of this refactor.

Only choose jagged if game design truly needs ragged columns of different heights as first-class data. Current code does not justify that.

## Migration Strategy

Safe path from legacy `BucketColumns`:

1. Add new `BucketGrid` alongside legacy `BucketColumns`; mark legacy as `[HideInInspector]` + `[FormerlySerializedAs]` only if renaming fields.
2. Build one editor-only migration action on `LevelData`:
   - width = `BucketColumns.Length`
   - height = `max(column.BucketColors.Length)`
   - fill missing cells with `Empty`
   - map `BucketColumns[col].BucketColors[row] -> BucketGrid[col, row]`
3. Keep runtime reading old data first via compatibility shim or convert grid -> runtime enumerable in one place; do not duplicate conversion logic all over.
4. Batch-migrate all `Level_*.asset`, save, diff YAML, then validate bucket counts and playable output.
5. After all assets migrated and verified, remove `BucketColumns` and any dead legacy fields.

Best compatibility shim shape:

```csharp
IEnumerable<(int col, int row, ColorType color)> EnumerateBuckets()
```

Then both old and new schema can feed same runtime loop during transition.

## Legacy fields to drop

Based on current file, strongest legacy-drop candidates are queue fallback fields once all assets use `QueueLanes`:

- `HasQueue`
- `QueueRings`
- `QueueSpeed`

For bucket config itself, drop `BucketColumns` after migration. Keep spacing fields unless layout is now fully scene-driven.

## Main Risks

- Odin dependency risk: `T[,]` only works safely if `LevelData` uses Odin serialization path (`SerializedScriptableObject` or equivalent). Unity alone will not serialize it.
- Empty-cell semantics risk: if no explicit `Empty` exists, designers cannot represent gaps cleanly.
- Axis confusion risk: current runtime is column-major; inspector may visually imply row-major. Need one explicit convention and labels.
- YAML churn risk: migration touches all level assets; do in one focused change.
- Partial migration risk: supporting both `BucketColumns` and `BucketGrid` too long creates split-brain data.

## Final Recommendation

Ship the smallest durable model:

- `LevelData` becomes `SerializedScriptableObject`
- `BucketGrid : BucketCell[,]` as single source of truth
- Odin `TableMatrix` for authoring
- one editor migration button from `BucketColumns`
- remove `BucketColumns` and old queue fallback fields after asset migration verified

This is most Odin-friendly, most authoring-friendly, and still minimal.

## References

- Repo: `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs`
- Repo: `UnityStackTheRing/Assets/Scripts/Bucket/BucketColumnManager.cs`
- Repo asset sample: `UnityStackTheRing/Assets/Resources/Levels/Level_01.asset`
- Repo asset sample: `UnityStackTheRing/Assets/Resources/Levels/Level_09.asset`
- Unity serialization rules: `https://docs.unity3d.com/Manual/script-serialization-rules.html`
- Odin TableMatrix: `https://odininspector.com/attributes/table-matrix-attribute`
- Odin serializer integration: `https://odininspector.com/tutorials/serialize-anything/implementing-the-odin-serializer`
