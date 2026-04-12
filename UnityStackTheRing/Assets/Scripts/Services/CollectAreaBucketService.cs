namespace HyperCasualGame.Scripts.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using HyperCasualGame.Scripts.Bucket;
    using HyperCasualGame.Scripts.CollectArea;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Service to query buckets in CollectAreas.
    /// Matches Cocos CollectAreaBucketService.ts
    /// </summary>
    public class CollectAreaBucketService
    {
        private CollectAreaManager collectAreaManager;
        private Bucket activeTargetBucket;

        public void SetCollectAreaManager(CollectAreaManager manager)
        {
            this.collectAreaManager = manager;
            this.activeTargetBucket = null;
        }

        public Bucket GetStableTargetBucketForColor(ColorType color)
        {
            if (this.IsBucketValid(this.activeTargetBucket) && this.activeTargetBucket.Data.Color == color)
            {
                return this.activeTargetBucket;
            }

            var nextBucket = this.GetAvailableBucketsByColor(color).FirstOrDefault();
            if (nextBucket != this.activeTargetBucket)
            {
                var previous = this.activeTargetBucket != null ? $"b{this.activeTargetBucket.Data.IndexBucket}:{this.activeTargetBucket.Data.Color}" : "none";
                var next = nextBucket != null ? $"b{nextBucket.Data.IndexBucket}:{nextBucket.Data.Color}" : "none";
                Debug.Log($"[CollectAreaBucketService] ActiveTargetSwitch requestedColor={color} from={previous} to={next}");
            }

            this.activeTargetBucket = nextBucket;
            return this.activeTargetBucket;
        }

        public string GetActiveTargetDebug()
        {
            if (!this.IsBucketValid(this.activeTargetBucket))
            {
                return "none";
            }

            return $"b{this.activeTargetBucket.Data.IndexBucket}:{this.activeTargetBucket.Data.Color}[c={this.activeTargetBucket.CollectedBallCount},in={this.activeTargetBucket.IncomingBallCount},target={this.activeTargetBucket.TargetBallCount},rem={this.activeTargetBucket.GetRemainingSlotCount()}]";
        }

        /// <summary>
        /// Get all target colors from buckets currently in CollectAreas.
        /// </summary>
        public List<ColorType> GetTargetColorsFromBuckets()
        {
            var colors = new List<ColorType>();

            foreach (var bucket in this.GetBucketsInCollectAreas())
            {
                if (bucket != null)
                {
                    colors.Add(bucket.Data.Color);
                }
            }

            return colors;
        }

        /// <summary>
        /// Get target color at specific CollectArea index.
        /// </summary>
        public ColorType? GetTargetColorByCollectAreaIndex(int index)
        {
            var bucket = this.GetBucketByCollectAreaIndex(index);
            return bucket?.Data.Color;
        }

        /// <summary>
        /// Get bucket at specific CollectArea index.
        /// </summary>
        public Bucket GetBucketByCollectAreaIndex(int index)
        {
            if (index < 0 || this.collectAreaManager == null)
            {
                return null;
            }

            var collectAreas = this.collectAreaManager.GetListCollectArea();
            if (index >= collectAreas.Count)
            {
                return null;
            }

            return this.FindBucketInArea(collectAreas[index]);
        }

        /// <summary>
        /// Get first available bucket by color (has remaining slots).
        /// </summary>
        public Bucket GetAvailableBucketByColor(ColorType color)
        {
            var buckets = this.GetAvailableBucketsByColor(color);
            return buckets.Count > 0 ? buckets[0] : null;
        }

        /// <summary>
        /// Get all available buckets by color (have remaining slots).
        /// </summary>
        public List<Bucket> GetAvailableBucketsByColor(ColorType color)
        {
            var result = new List<Bucket>();

            foreach (var bucket in this.GetBucketsInCollectAreas())
            {
                if (bucket == null || bucket.IsBucketCompleted())
                {
                    continue;
                }

                if (bucket.Data.Color == color && bucket.GetRemainingSlotCount() > 0)
                {
                    result.Add(bucket);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all available buckets in CollectAreas (not completed, has slots).
        /// </summary>
        public List<Bucket> GetAvailableBucketsInCollectAreas()
        {
            var result = new List<Bucket>();

            foreach (var bucket in this.GetBucketsInCollectAreas())
            {
                if (bucket != null && !bucket.IsBucketCompleted() && bucket.GetRemainingSlotCount() > 0)
                {
                    result.Add(bucket);
                }
            }

            return result;
        }

        /// <summary>
        /// Check if color is currently targeted by any bucket in CollectAreas.
        /// </summary>
        public bool IsColorTargeted(ColorType color)
        {
            return this.GetTargetColorsFromBuckets().Contains(color);
        }

        /// <summary>
        /// Get total available slot count by color across all buckets.
        /// </summary>
        public int GetAvailableSlotCountByColor(ColorType color)
        {
            var total = 0;

            foreach (var bucket in this.GetAvailableBucketsByColor(color))
            {
                total += bucket.GetRemainingSlotCount();
            }

            return total;
        }

        /// <summary>
        /// Build a balanced bucket assignment plan for multiple balls.
        /// Distributes balls evenly across available buckets.
        /// Matches Cocos CollectAreaBucketService.buildBalancedBucketPlanByColor()
        /// </summary>
        public List<Bucket> BuildBalancedBucketPlanByColor(ColorType color, int ballCount)
        {
            if (ballCount <= 0)
            {
                return new List<Bucket>();
            }

            var buckets = this.GetAvailableBucketsByColor(color);
            if (buckets.Count == 0)
            {
                return new List<Bucket>();
            }

            var plan = new List<Bucket>();
            var plannedIncomingByBucket = new Dictionary<Bucket, int>();

            while (plan.Count < ballCount)
            {
                var nextBucket = this.SelectBestBucketForBalancedPlan(buckets, plannedIncomingByBucket);
                if (nextBucket == null)
                {
                    break;
                }

                plan.Add(nextBucket);

                if (plannedIncomingByBucket.ContainsKey(nextBucket))
                {
                    plannedIncomingByBucket[nextBucket]++;
                }
                else
                {
                    plannedIncomingByBucket[nextBucket] = 1;
                }
            }

            return plan;
        }

        private Bucket SelectBestBucketForBalancedPlan(
            List<Bucket> buckets,
            Dictionary<Bucket, int> plannedIncomingByBucket)
        {
            Bucket bestBucket = null;
            var bestProjectedFillRatio = float.MaxValue;
            var bestRemainingSlotCount = -1;

            foreach (var bucket in buckets)
            {
                if (bucket == null || bucket.IsBucketCompleted())
                {
                    continue;
                }

                plannedIncomingByBucket.TryGetValue(bucket, out var plannedIncoming);
                var remainingSlots = bucket.GetRemainingSlotCount(plannedIncoming);

                if (remainingSlots <= 0)
                {
                    continue;
                }

                var projectedFillRatio = bucket.GetProjectedFillRatio(plannedIncoming);

                var isBetterBucket =
                    projectedFillRatio < bestProjectedFillRatio ||
                    (projectedFillRatio == bestProjectedFillRatio && remainingSlots > bestRemainingSlotCount);

                if (isBetterBucket)
                {
                    bestBucket = bucket;
                    bestProjectedFillRatio = projectedFillRatio;
                    bestRemainingSlotCount = remainingSlots;
                }
            }

            return bestBucket;
        }

        private List<Bucket> GetBucketsInCollectAreas()
        {
            var result = new List<Bucket>();

            if (this.collectAreaManager == null)
            {
                return result;
            }

            foreach (var area in this.collectAreaManager.GetListCollectArea())
            {
                var bucket = this.FindBucketInArea(area);
                if (bucket != null)
                {
                    result.Add(bucket);
                }
            }

            return result;
        }

        private Bucket FindBucketInArea(CollectArea area)
        {
            if (area == null || area.OccupyingBucket == null)
            {
                return null;
            }

            return area.OccupyingBucket.GetComponent<Bucket>();
        }

        private bool IsBucketValid(Bucket bucket)
        {
            return bucket != null && !bucket.IsBucketCompleted() && bucket.GetRemainingSlotCount() > 0;
        }
    }
}
