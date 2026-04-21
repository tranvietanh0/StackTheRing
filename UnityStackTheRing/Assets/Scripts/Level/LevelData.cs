namespace HyperCasualGame.Scripts.Level
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HyperCasualGame.Scripts.Core;
    using Sirenix.OdinInspector;
    using Sirenix.Serialization;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Level_00", menuName = "StackTheRing/LevelData")]
    public class LevelData : SerializedScriptableObject
    {
        [Header("Basic Info")]
        public int LevelNumber;

        [Header("Conveyor Settings")]
        [Range(0.5f, 3f)]
        public float ConveyorSpeed = 1f;

        [Header("Stack Settings")]
        [Range(4, 12)]
        public int StackLimit = 8;

        [Header("Ring Configuration")]
        public RingSpawn[] Rings;

        [HideInInspector]
        public ColorType[] AvailableCollectors;

        [Serializable]
        public class HiddenBucketConfig
        {
            [MinValue(0)] public int Column;
            [MinValue(0)] public int Row;
            public bool ShowQuestionMark = true;
        }

        [Header("Bucket Grid Configuration")]
        [InfoBox("BucketGrid matches play mode directly: horizontal = column, vertical = row, access = grid[col, row].")]
        [OdinSerialize]
        [ValidateInput(nameof(IsBucketGridValid), "BucketGrid contains unsupported data.")]
        [TableMatrix(SquareCells = true, ResizableColumns = false, DrawElementMethod = nameof(DrawBucketCell))]
        public BucketCellType[,] BucketGrid;

        [ShowIf(nameof(HasBucketGrid))]
        [ListDrawerSettings(Expanded = true)]
        public HiddenBucketConfig[] HiddenBuckets;

        [ShowInInspector, ReadOnly, PropertyOrder(-18)]
        private bool HasLegacyBucketColumns => this.BucketColumns != null && this.BucketColumns.Length > 0;

        [HideInInspector]
        public BucketColumn[] BucketColumns;
        public float BucketColumnSpacing = 1.2f;
        public float BucketRowSpacing = 1.2f;

        [Header("Queue Conveyor")]
        [ShowIf(nameof(UsesLegacyQueueAuthoring))]
        [Tooltip("Enable queue conveyor that feeds rows into the ring when gaps appear")]
        public bool HasQueue;

        [ShowIf(nameof(UsesLegacyQueueAuthoring))]
        [Tooltip("Rings waiting in the queue (fed into ring when space opens)")]
        public RingSpawn[] QueueRings;

        [ShowIf(nameof(UsesLegacyQueueAuthoring))]
        [Tooltip("Speed multiplier for queue conveyor movement")]
        [Range(0.5f, 3f)]
        public float QueueSpeed = 1f;

        [Tooltip("Multi-queue lanes. New levels should use this instead of singleton queue fields.")]
        public QueueLaneData[] QueueLanes;

        [HideInInspector]
        public bool HasHiddenRings;

        [HideInInspector]
        public int BlockedSlotCount;

        [ShowInInspector, ReadOnly, PropertyOrder(-20)]
        private bool UsesLegacyQueueAuthoring => this.QueueLanes == null || this.QueueLanes.Length == 0;

        [ShowInInspector, ReadOnly, PropertyOrder(-19)]
        private string BucketAuthoringMode => this.HasBucketGrid ? "BucketGrid (Odin)" : "Legacy BucketColumns";

        public int TotalRingCount
        {
            get
            {
                var count = 0;
                if (this.Rings == null) return count;
                foreach (var ring in this.Rings)
                {
                    count += ring.Count;
                }
                return count;
            }
        }

        public int TotalQueueRingCount
        {
            get
            {
                var count = 0;
                foreach (var lane in this.GetActiveQueueLanes())
                {
                    if (lane.QueueRings == null) continue;
                    foreach (var ring in lane.QueueRings)
                    {
                        count += ring.Count;
                    }
                }
                return count;
            }
        }

        public bool HasAnyQueue => this.GetActiveQueueLanes().Length > 0;

        public QueueLaneData[] GetActiveQueueLanes()
        {
            if (this.QueueLanes != null && this.QueueLanes.Length > 0)
            {
                return this.QueueLanes.Where(lane => lane != null && lane.Enabled).ToArray();
            }

            if (!this.HasQueue)
            {
                return Array.Empty<QueueLaneData>();
            }

            return new[]
            {
                new QueueLaneData
                {
                    LaneId = "queue-0",
                    Enabled = true,
                    QueueRings = this.QueueRings,
                    QueueSpeed = this.QueueSpeed,
                    DisplayName = "Legacy Queue"
                }
            };
        }

        public int BucketGridWidth => this.HasBucketGrid ? this.BucketGrid.GetLength(0) : this.BucketColumns?.Length ?? 0;

        public int BucketGridHeight
        {
            get
            {
                if (this.HasBucketGrid)
                {
                    return this.BucketGrid.GetLength(1);
                }

                if (this.BucketColumns == null || this.BucketColumns.Length == 0)
                {
                    return 0;
                }

                return this.BucketColumns.Max(column => column?.BucketColors?.Length ?? 0);
            }
        }

        public bool HasBucketGrid => this.BucketGrid != null && this.BucketGrid.Length > 0;

        public bool TryGetHiddenBucketConfig(int column, int row, out HiddenBucketConfig hiddenBucketConfig)
        {
            hiddenBucketConfig = null;
            if (this.HiddenBuckets == null)
            {
                return false;
            }

            foreach (var item in this.HiddenBuckets)
            {
                if (item == null)
                {
                    continue;
                }

                if (item.Column == column && item.Row == row)
                {
                    hiddenBucketConfig = item;
                    return true;
                }
            }

            return false;
        }

        public bool DoesBucketGridMatchLegacyColumns()
        {
            if (!this.HasBucketGrid || !this.HasLegacyBucketColumns)
            {
                return true;
            }

            var expectedGrid = CreateBucketGridFromLegacyColumns(this.BucketColumns);
            return AreBucketGridsEqual(this.BucketGrid, expectedGrid);
        }

        public IEnumerable<BucketLayoutCell> EnumerateBucketLayout()
        {
            if (this.HasBucketGrid)
            {
                for (var column = 0; column < this.BucketGrid.GetLength(0); column++)
                {
                    for (var row = 0; row < this.BucketGrid.GetLength(1); row++)
                    {
                        var cell = this.BucketGrid[column, row];
                        if (cell == BucketCellType.Empty)
                        {
                            continue;
                        }

                        yield return new BucketLayoutCell(column, row, (ColorType)cell);
                    }
                }

                yield break;
            }

            if (this.BucketColumns == null)
            {
                yield break;
            }

            for (var column = 0; column < this.BucketColumns.Length; column++)
            {
                var bucketColors = this.BucketColumns[column]?.BucketColors;
                if (bucketColors == null)
                {
                    continue;
                }

                for (var row = 0; row < bucketColors.Length; row++)
                {
                    yield return new BucketLayoutCell(column, row, bucketColors[row]);
                }
            }
        }

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium)]
        [PropertyOrder(-10)]
        [GUIColor(0.3f, 0.8f, 0.3f)]
        public void MigrateBucketColumnsToGrid()
        {
            this.BucketGrid = CreateBucketGridFromLegacyColumns(this.BucketColumns);
        }

        [Button(ButtonSizes.Medium)]
        [PropertyOrder(-9)]
        [GUIColor(0.3f, 0.6f, 0.9f)]
        public void SyncLegacyColumnsFromGrid()
        {
            if (!this.HasBucketGrid)
            {
                return;
            }

            this.ValidateBucketGridForLegacySync();

            this.BucketColumns = CreateLegacyColumnsFromBucketGrid(this.BucketGrid);
        }

        private static BucketCellType DrawBucketCell(Rect rect, BucketCellType value)
        {
            return (BucketCellType)Sirenix.Utilities.Editor.SirenixEditorFields.EnumDropdown(rect, value);
        }

        [Button(ButtonSizes.Small)]
        [PropertyOrder(-11)]
        public void ResizeBucketGridToLegacyShape()
        {
            this.BucketGrid = CreateBucketGridFromLegacyColumns(this.BucketColumns);
        }
#endif

        public void ValidateBucketGridForRuntime()
        {
            if (!this.HasBucketGrid)
            {
                return;
            }

            for (var column = 0; column < this.BucketGrid.GetLength(0); column++)
            {
                for (var row = 0; row < this.BucketGrid.GetLength(1); row++)
                {
                    var cell = this.BucketGrid[column, row];
                    if (!Enum.IsDefined(typeof(BucketCellType), cell))
                    {
                        throw new InvalidOperationException($"BucketGrid cell [{column}, {row}] contains unsupported value {(int)cell}.");
                    }
                }
            }

            if (this.HiddenBuckets == null)
            {
                return;
            }

            var uniquePositions = new HashSet<(int Column, int Row)>();
            foreach (var hiddenBucket in this.HiddenBuckets)
            {
                if (hiddenBucket == null)
                {
                    continue;
                }

                var position = (hiddenBucket.Column, hiddenBucket.Row);
                if (!uniquePositions.Add(position))
                {
                    throw new InvalidOperationException($"Hidden bucket config duplicates cell [{hiddenBucket.Column}, {hiddenBucket.Row}].");
                }

                if (hiddenBucket.Column < 0 || hiddenBucket.Column >= this.BucketGrid.GetLength(0)
                    || hiddenBucket.Row < 0 || hiddenBucket.Row >= this.BucketGrid.GetLength(1))
                {
                    throw new InvalidOperationException($"Hidden bucket config [{hiddenBucket.Column}, {hiddenBucket.Row}] is out of BucketGrid range.");
                }

                if (this.BucketGrid[hiddenBucket.Column, hiddenBucket.Row] == BucketCellType.Empty)
                {
                    throw new InvalidOperationException($"Hidden bucket config [{hiddenBucket.Column}, {hiddenBucket.Row}] points to an empty BucketGrid cell.");
                }
            }

            this.ValidateHiddenBucketReachability();
        }

        public void ValidateBucketGridForLegacySync()
        {
            if (!this.HasBucketGrid)
            {
                return;
            }

            for (var column = 0; column < this.BucketGrid.GetLength(0); column++)
            {
                var encounteredFilled = false;
                var encounteredEmptyAfterFilled = false;
                for (var row = 0; row < this.BucketGrid.GetLength(1); row++)
                {
                    var cell = this.BucketGrid[column, row];
                    if (cell == BucketCellType.Empty)
                    {
                        if (encounteredFilled)
                        {
                            encounteredEmptyAfterFilled = true;
                        }

                        continue;
                    }

                    if (!encounteredFilled && row > 0)
                    {
                        throw new InvalidOperationException($"BucketGrid column {column} contains a top gap. Legacy BucketColumns sync requires contiguous filled cells starting at row 0.");
                    }

                    encounteredFilled = true;

                    if (encounteredEmptyAfterFilled)
                    {
                        throw new InvalidOperationException($"BucketGrid column {column} contains unsupported holes for legacy BucketColumns sync.");
                    }
                }
            }
        }

        private bool IsBucketGridValid()
        {
            try
            {
                this.ValidateBucketGridForRuntime();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ValidateHiddenBucketReachability()
        {
            if (this.HiddenBuckets == null || this.HiddenBuckets.Length == 0)
            {
                return;
            }

            var allBucketPositions = new HashSet<(int Column, int Row)>();
            var revealedBucketPositions = new HashSet<(int Column, int Row)>();
            var movedBucketPositions = new HashSet<(int Column, int Row)>();

            for (var column = 0; column < this.BucketGrid.GetLength(0); column++)
            {
                for (var row = 0; row < this.BucketGrid.GetLength(1); row++)
                {
                    if (this.BucketGrid[column, row] == BucketCellType.Empty)
                    {
                        continue;
                    }

                    allBucketPositions.Add((column, row));
                    if (!this.TryGetHiddenBucketConfig(column, row, out _))
                    {
                        revealedBucketPositions.Add((column, row));
                    }
                }
            }

            var madeProgress = true;
            while (madeProgress)
            {
                madeProgress = false;
                for (var column = 0; column < this.BucketGrid.GetLength(0); column++)
                {
                    (int Column, int Row)? eligibleBucket = null;
                    for (var row = 0; row < this.BucketGrid.GetLength(1); row++)
                    {
                        var position = (column, row);
                        if (!allBucketPositions.Contains(position) || movedBucketPositions.Contains(position))
                        {
                            continue;
                        }

                        eligibleBucket = position;
                        break;
                    }

                    if (!eligibleBucket.HasValue || !revealedBucketPositions.Contains(eligibleBucket.Value))
                    {
                        continue;
                    }

                    movedBucketPositions.Add(eligibleBucket.Value);
                    revealedBucketPositions.Add((eligibleBucket.Value.Column, eligibleBucket.Value.Row + 1));
                    revealedBucketPositions.Add((eligibleBucket.Value.Column - 1, eligibleBucket.Value.Row));
                    revealedBucketPositions.Add((eligibleBucket.Value.Column + 1, eligibleBucket.Value.Row));
                    madeProgress = true;
                }
            }

            if (movedBucketPositions.Count == allBucketPositions.Count)
            {
                return;
            }

            var unreachableCount = allBucketPositions.Count - movedBucketPositions.Count;
            throw new InvalidOperationException($"BucketGrid contains {unreachableCount} unreachable buckets. Hidden bucket reveal chain soft-locks this level.");
        }

        private static bool AreBucketGridsEqual(BucketCellType[,] left, BucketCellType[,] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1))
            {
                return false;
            }

            for (var column = 0; column < left.GetLength(0); column++)
            {
                for (var row = 0; row < left.GetLength(1); row++)
                {
                    if (left[column, row] != right[column, row])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static BucketCellType[,] CreateBucketGridFromLegacyColumns(BucketColumn[] bucketColumns)
        {
            var width = bucketColumns?.Length ?? 0;
            var height = bucketColumns?.Max(column => column?.BucketColors?.Length ?? 0) ?? 0;
            var bucketGrid = new BucketCellType[width, height];

            for (var column = 0; column < width; column++)
            {
                for (var row = 0; row < height; row++)
                {
                    bucketGrid[column, row] = BucketCellType.Empty;
                }
            }

            if (bucketColumns == null)
            {
                return bucketGrid;
            }

            for (var column = 0; column < bucketColumns.Length; column++)
            {
                var bucketColors = bucketColumns[column]?.BucketColors;
                if (bucketColors == null)
                {
                    continue;
                }

                for (var row = 0; row < bucketColors.Length; row++)
                {
                    bucketGrid[column, row] = (BucketCellType)bucketColors[row];
                }
            }

            return bucketGrid;
        }

        private static BucketColumn[] CreateLegacyColumnsFromBucketGrid(BucketCellType[,] bucketGrid)
        {
            var width = bucketGrid.GetLength(0);
            var height = bucketGrid.GetLength(1);
            var bucketColumns = new BucketColumn[width];

            for (var column = 0; column < width; column++)
            {
                var colors = new List<ColorType>();
                for (var row = 0; row < height; row++)
                {
                    var cell = bucketGrid[column, row];
                    if (cell == BucketCellType.Empty)
                    {
                        continue;
                    }

                    colors.Add((ColorType)cell);
                }

                bucketColumns[column] = new BucketColumn
                {
                    BucketColors = colors.ToArray()
                };
            }

            return bucketColumns;
        }

        /// <summary>
        /// Total rings across both main conveyor and queue.
        /// </summary>
        public int TotalAllRingCount => this.TotalRingCount + this.TotalQueueRingCount;
    }

    [Serializable]
    public class RingSpawn
    {
        public ColorType Color;
        [Range(1, 100)]
        public int Count;
    }

    /// <summary>
    /// Bucket column configuration (matches Cocos GridColumn)
    /// </summary>
    [Serializable]
    public class BucketColumn
    {
        public ColorType[] BucketColors;
    }

    public enum BucketCellType
    {
        Empty = -1,
        Red = ColorType.Red,
        Yellow = ColorType.Yellow,
        Green = ColorType.Green,
        Blue = ColorType.Blue,
        Purple = ColorType.Purple,
        Orange = ColorType.Orange,
        Cyan = ColorType.Cyan,
        DarkGray = ColorType.DarkGray,
        Pink = ColorType.Pink,
        Brown = ColorType.Brown,
        Lime = ColorType.Lime
    }

    public readonly struct BucketLayoutCell
    {
        public BucketLayoutCell(int column, int row, ColorType color)
        {
            this.Column = column;
            this.Row = row;
            this.Color = color;
        }

        public int Column { get; }
        public int Row { get; }
        public ColorType Color { get; }
    }

    [Serializable]
    public class QueueLaneData
    {
        public string LaneId = "queue-0";
        public string DisplayName = "Queue 0";
        public bool Enabled = true;
        public RingSpawn[] QueueRings;
        [Range(0.5f, 3f)] public float QueueSpeed = 1f;
    }
}
