namespace HyperCasualGame.Scripts.Bucket
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.CollectArea;
    using HyperCasualGame.Scripts.Conveyor;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Ring;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using UnityEngine;

    /// <summary>
    /// Manages bucket columns/grid. Matches Cocos GridBucketManager.ts
    /// </summary>
    public class BucketColumnManager : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Bucket bucketPrefab;
        [SerializeField] private Bucket hiddenBucketPrefab;
        [SerializeField] private Transform bucketContainer;
        [SerializeField] private float columnSpacing = 1.2f;
        [SerializeField] private float rowSpacing = 1.2f;

        #endregion

        #region Private Fields

        private readonly List<Transform> dynamicColumnNodes = new();
        private readonly List<Bucket> spawnedBuckets = new();
        private readonly List<Bucket> lockedBuckets = new();
        private readonly Dictionary<Vector2Int, Bucket> bucketsByGridPosition = new();
        private SignalBus signalBus;
        private CollectAreaManager collectAreaManager;
        private int collectedUnlockBallCount;

        #endregion

        #region Properties

        public IReadOnlyList<Bucket> SpawnedBuckets => this.spawnedBuckets;

        #endregion

        #region Public Methods

        public void Initialize(SignalBus signalBus, CollectAreaManager collectAreaManager)
        {
            this.signalBus = signalBus;
            this.collectAreaManager = collectAreaManager;

            // Setup input controller for bucket taps
            var inputController = this.GetComponent<BucketInputController>();
            if (inputController == null)
            {
                inputController = this.gameObject.AddComponent<BucketInputController>();
            }
            inputController.Initialize(signalBus);
        }

        /// <summary>
        /// Spawn buckets based on level data.
        /// Matches Cocos GridBucketManager.spawnBuckets()
        /// </summary>
        public void SpawnBuckets(LevelData levelData, int ballsPerRow, MultiQueueCoordinator multiQueueCoordinator = null)
        {
            this.Cleanup();

            levelData?.ValidateBucketGridForRuntime();

            if (this.bucketPrefab == null)
            {
                Debug.LogError("[BucketColumnManager] bucketPrefab is null!");
                return;
            }

            var layoutCells = levelData.EnumerateBucketLayout().ToArray();
            if (layoutCells.Length == 0)
            {
                Debug.LogWarning("[BucketColumnManager] No bucket grid layout in level data!");
                return;
            }

            var width = levelData.BucketGridWidth;
            this.collectedUnlockBallCount = 0;
            var totalBallCountByColor = this.CalculateTotalBallCountByColor(levelData, ballsPerRow, multiQueueCoordinator);
            var bucketCountByColor = this.CalculateBucketCountByColor(layoutCells);
            var assignedBucketCountByColor = new Dictionary<ColorType, int>();

            var spacing = levelData.BucketColumnSpacing > 0 ? levelData.BucketColumnSpacing : this.columnSpacing;
            var rowSpace = levelData.BucketRowSpacing > 0 ? levelData.BucketRowSpacing : this.rowSpacing;

            this.CreateDynamicColumns(width, spacing);

            var globalIndex = 0;

            foreach (var layoutCell in layoutCells.OrderBy(cell => cell.Column).ThenBy(cell => cell.Row))
            {
                var columnNode = this.dynamicColumnNodes[layoutCell.Column];
                if (columnNode == null) continue;

                levelData.TryGetHiddenBucketConfig(layoutCell.Column, layoutCell.Row, out var hiddenBucketConfig);
                levelData.TryGetLockedBucketConfig(layoutCell.Column, layoutCell.Row, out var lockedBucketConfig);

                var zOffset = -layoutCell.Row * rowSpace;
                var prefab = hiddenBucketConfig != null && this.hiddenBucketPrefab != null
                    ? this.hiddenBucketPrefab
                    : this.bucketPrefab;
                var bucketObj = Instantiate(prefab, columnNode);
                bucketObj.transform.localPosition = new Vector3(0f, 0f, zOffset);

                var bucketConfig = new BucketConfig
                {
                    IndexBucket = globalIndex++,
                    Row = layoutCell.Row,
                    Column = layoutCell.Column,
                    Color = layoutCell.Color,
                    IsHidden = hiddenBucketConfig != null,
                    ShowQuestionMark = hiddenBucketConfig?.ShowQuestionMark ?? false,
                    IsLocked = lockedBucketConfig != null,
                    RequiredBallsToUnlock = lockedBucketConfig?.RequiredBallsToUnlock ?? 0,
                    TargetBallCount = this.ResolveTargetBallCountForBucket(
                        layoutCell.Color,
                        totalBallCountByColor,
                        bucketCountByColor,
                        assignedBucketCountByColor
                    )
                };

                bucketObj.Initialize(bucketConfig, this.signalBus);
                bucketObj.RefreshLockedProgress(this.collectedUnlockBallCount);
                bucketObj.OnBallAdded += this.OnBucketBallAdded;
                bucketObj.name = $"Bucket_{layoutCell.Column}_{layoutCell.Row}_{layoutCell.Color}";
                this.spawnedBuckets.Add(bucketObj);
                if (bucketObj.IsLocked)
                {
                    this.lockedBuckets.Add(bucketObj);
                }
                this.bucketsByGridPosition[new Vector2Int(layoutCell.Column, layoutCell.Row)] = bucketObj;
            }

            Debug.Log($"[BucketColumnManager] Spawned {this.spawnedBuckets.Count} buckets in {width} columns");
        }

        /// <summary>
        /// Get all buckets eligible to jump to collect area.
        /// Returns first non-placed bucket from each column.
        /// Matches Cocos GridBucketManager.getEligibleBuckets()
        /// </summary>
        public List<Bucket> GetEligibleBuckets()
        {
            var eligibleBuckets = new List<Bucket>();

            foreach (var columnNode in this.dynamicColumnNodes)
            {
                if (columnNode == null) continue;

                foreach (Transform child in columnNode)
                {
                    var bucket = child.GetComponent<Bucket>();
                    if (bucket != null && !bucket.IsInCollectArea)
                    {
                        if (!bucket.IsHidden && !bucket.IsLocked)
                        {
                            eligibleBuckets.Add(bucket);
                        }

                        break;
                    }
                }
            }

            return eligibleBuckets;
        }

        /// <summary>
        /// Check if there are any buckets remaining on the grid (not in collect area).
        /// Matches Cocos GridBucketManager.hasBucketsOnGrid()
        /// </summary>
        public bool HasBucketsOnGrid()
        {
            foreach (var bucket in this.spawnedBuckets)
            {
                if (bucket != null && !bucket.IsInCollectArea)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handle bucket tap - jump to first empty collect area.
        /// </summary>
        public async UniTask OnBucketTapped(Bucket bucket)
        {
            if (bucket == null || bucket.IsInCollectArea || bucket.IsLocked)
            {
                return;
            }

            var eligibleBuckets = this.GetEligibleBuckets();
            if (!eligibleBuckets.Contains(bucket))
            {
                Debug.LogWarning($"[BucketColumnManager] Bucket {bucket.name} is not eligible to jump");
                return;
            }

            var targetArea = this.collectAreaManager?.GetFirstEmptyArea();
            if (targetArea == null)
            {
                Debug.LogWarning("[BucketColumnManager] No empty CollectArea available");
                return;
            }

            targetArea.Occupy(bucket.transform);

            // Note: BucketTappedSignal is already fired by BucketInputController
            // DO NOT fire it again here to avoid loop

            await bucket.JumpToCollectArea(targetArea.transform);
            this.RevealNeighborHiddenBuckets(bucket);

            this.signalBus?.Fire(new BucketJumpedToAreaSignal
            {
                BucketIndex = bucket.Data.IndexBucket,
                AreaIndex = targetArea.AreaIndex,
                Color = bucket.Data.Color
            });
        }

        /// <summary>
        /// Auto-place eligible buckets into empty CollectAreas.
        /// Called at game start to fill CollectAreas automatically.
        /// </summary>
        public async UniTask AutoPlaceEligibleBuckets()
        {
            var eligibleBuckets = this.GetEligibleBuckets();
            Debug.Log($"[BucketColumnManager] AutoPlaceEligibleBuckets: {eligibleBuckets.Count} eligible buckets");

            foreach (var bucket in eligibleBuckets)
            {
                var targetArea = this.collectAreaManager?.GetFirstEmptyArea();
                if (targetArea == null)
                {
                    Debug.Log("[BucketColumnManager] No more empty CollectAreas for auto-placement");
                    break;
                }

                targetArea.Occupy(bucket.transform);
                await bucket.JumpToCollectArea(targetArea.transform);
                this.RevealNeighborHiddenBuckets(bucket);

                this.signalBus?.Fire(new BucketJumpedToAreaSignal
                {
                    BucketIndex = bucket.Data.IndexBucket,
                    AreaIndex = targetArea.AreaIndex,
                    Color = bucket.Data.Color
                });

                Debug.Log($"[BucketColumnManager] Auto-placed {bucket.name} to CollectArea {targetArea.AreaIndex}");
            }
        }

        /// <summary>
        /// Cleanup all buckets and columns.
        /// Matches Cocos GridBucketManager.cleanup()
        /// </summary>
        public void Cleanup()
        {
            foreach (var bucket in this.spawnedBuckets)
            {
                if (bucket != null)
                {
                    bucket.OnBallAdded -= this.OnBucketBallAdded;
                    Destroy(bucket.gameObject);
                }
            }

            this.spawnedBuckets.Clear();
            this.lockedBuckets.Clear();
            this.bucketsByGridPosition.Clear();

            foreach (var columnNode in this.dynamicColumnNodes)
            {
                if (columnNode != null)
                {
                    Destroy(columnNode.gameObject);
                }
            }

            this.dynamicColumnNodes.Clear();
            this.collectedUnlockBallCount = 0;
        }

        #endregion

        #region Private Methods

        private void OnBucketBallAdded(Bucket bucket, Ball _)
        {
            if (bucket == null || !bucket.IsInCollectArea || this.lockedBuckets.Count == 0)
            {
                return;
            }

            this.collectedUnlockBallCount++;
            this.RefreshLockedBuckets();
        }

        private void RefreshLockedBuckets()
        {
            for (var index = this.lockedBuckets.Count - 1; index >= 0; index--)
            {
                var lockedBucket = this.lockedBuckets[index];
                if (lockedBucket == null)
                {
                    this.lockedBuckets.RemoveAt(index);
                    continue;
                }

                lockedBucket.RefreshLockedProgress(this.collectedUnlockBallCount);
                if (!lockedBucket.IsLocked)
                {
                    this.lockedBuckets.RemoveAt(index);
                }
            }
        }

        private void CreateDynamicColumns(int numberOfColumns, float spacing)
        {
            var parentNode = this.bucketContainer != null ? this.bucketContainer : this.transform;
            var totalWidth = (numberOfColumns - 1) * spacing;
            var startX = -totalWidth / 2f;

            for (var i = 0; i < numberOfColumns; i++)
            {
                var columnNode = new GameObject($"BucketColumn_{i}");
                columnNode.transform.SetParent(parentNode, false);
                var xPos = startX + i * spacing;
                columnNode.transform.localPosition = new Vector3(xPos, 0f, 0f);

                this.dynamicColumnNodes.Add(columnNode.transform);
            }
        }

        private int ResolveTargetBallCountForBucket(
            ColorType bucketColor,
            Dictionary<ColorType, int> totalBallCountByColor,
            Dictionary<ColorType, int> bucketCountByColor,
            Dictionary<ColorType, int> assignedBucketCountByColor)
        {
            if (!totalBallCountByColor.TryGetValue(bucketColor, out var totalBallCount))
            {
                return GameConstants.RowBallConfig.MaxBalls;
            }

            var totalBucketCount = bucketCountByColor.TryGetValue(bucketColor, out var count)
                ? Mathf.Max(1, count)
                : 1;

            assignedBucketCountByColor.TryGetValue(bucketColor, out var assignedBucketCount);
            assignedBucketCountByColor[bucketColor] = assignedBucketCount + 1;

            var baseBallCountPerBucket = totalBallCount / totalBucketCount;
            var extraBallCount = totalBallCount % totalBucketCount;

            return baseBallCountPerBucket + (assignedBucketCount < extraBallCount ? 1 : 0);
        }

        private void RevealNeighborHiddenBuckets(Bucket sourceBucket)
        {
            this.RevealHiddenBucketAt(sourceBucket.Data.Column, sourceBucket.Data.Row + 1);
            this.RevealHiddenBucketAt(sourceBucket.Data.Column - 1, sourceBucket.Data.Row);
            this.RevealHiddenBucketAt(sourceBucket.Data.Column + 1, sourceBucket.Data.Row);
        }

        private void RevealHiddenBucketAt(int column, int row)
        {
            if (column < 0 || row < 0)
            {
                return;
            }

            if (!this.bucketsByGridPosition.TryGetValue(new Vector2Int(column, row), out var bucket) || bucket == null || !bucket.IsHidden)
            {
                return;
            }

            bucket.Reveal();
        }

        private Dictionary<ColorType, int> CalculateTotalBallCountByColor(LevelData levelData, int ballsPerRow, MultiQueueCoordinator multiQueueCoordinator)
        {
            var result = new Dictionary<ColorType, int>();
            var normalizedBallsPerRow = Mathf.Max(1, ballsPerRow);

            // Count main ring balls
            if (levelData.Rings != null)
            {
                foreach (var ring in levelData.Rings)
                {
                    var totalBallCount = ring.Count * normalizedBallsPerRow;
                    if (result.ContainsKey(ring.Color))
                    {
                        result[ring.Color] += totalBallCount;
                    }
                    else
                    {
                        result[ring.Color] = totalBallCount;
                    }
                }
            }

            if (multiQueueCoordinator != null)
            {
                foreach (var rowBall in multiQueueCoordinator.PendingRows)
                {
                    foreach (var ball in rowBall.GetActiveBalls())
                    {
                        if (result.ContainsKey(ball.BallColor))
                        {
                            result[ball.BallColor] += 1;
                        }
                        else
                        {
                            result[ball.BallColor] = 1;
                        }
                    }
                }
            }
            else
            {
                foreach (var lane in levelData.GetActiveQueueLanes())
                {
                    if (!lane.Enabled || lane.QueueRings == null)
                    {
                        continue;
                    }

                    foreach (var ring in lane.QueueRings)
                    {
                        var totalBallCount = ring.Count * normalizedBallsPerRow;
                        if (result.ContainsKey(ring.Color))
                        {
                            result[ring.Color] += totalBallCount;
                        }
                        else
                        {
                            result[ring.Color] = totalBallCount;
                        }
                    }
                }
            }

            return result;
        }

        private Dictionary<ColorType, int> CalculateBucketCountByColor(IEnumerable<BucketLayoutCell> layoutCells)
        {
            var result = new Dictionary<ColorType, int>();

            foreach (var layoutCell in layoutCells)
            {
                if (result.ContainsKey(layoutCell.Color))
                {
                    result[layoutCell.Color]++;
                }
                else
                {
                    result[layoutCell.Color] = 1;
                }
            }

            return result;
        }

        #endregion
    }
}
