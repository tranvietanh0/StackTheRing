namespace HyperCasualGame.Scripts.Conveyor
{
    using System.Collections.Generic;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Distance-based path follower. Matches Cocos PathFollower class.
    /// Uses world-space distance instead of 0-1 progress.
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [Header("Movement")]
        public float MoveSpeed = 1f;
        public float RotationSpeed = 10f;
        public bool LoopPath = true;
        public bool ReverseDirection = false;

        private ConveyorPath path;
        private bool isMoving;
        private float currentDistance;
        private float totalPathLength;
        private readonly List<float> segmentLengths = new();
        private readonly List<float> cumulativeDistances = new();
        private int lastSegmentIndex;

        // Rotation offset (90 degrees Y)
        private readonly Quaternion rotationOffset = Quaternion.Euler(0, 90, 0);

        // Conveyor ID for signal routing
        private string conveyorId = "";

        // Entry point tracking
        private Transform entryNode;
        private readonly List<Transform> entryNodes = new();
        private readonly HashSet<int> triggeredEntryIndices = new();
        private readonly Dictionary<int, float> entryPathDistances = new();

        public bool IsWaitingAtEntry { get; set; }

        // Static cache for all followers
        private static readonly Dictionary<Transform, PathFollower> followerCache = new();

        private bool siblingsCacheDirty = true;
        private readonly List<PathFollower> cachedSiblingFollowers = new();

        private void OnEnable()
        {
            followerCache[this.transform] = this;
        }

        private void OnDisable()
        {
            followerCache.Remove(this.transform);
            this.MarkSiblingsCacheDirty();
        }

        private void OnDestroy()
        {
            followerCache.Remove(this.transform);
            this.MarkSiblingsCacheDirty();
        }

        public void Initialize(ConveyorPath conveyorPath, int startIndex = 0, string id = "")
        {
            this.path = conveyorPath;
            this.conveyorId = id;
            this.siblingsCacheDirty = true;
            this.entryPathDistances.Clear();
            this.ResetEntryTriggerState();

            this.MarkSiblingsCacheDirty();

            if (this.path != null && this.path.GetSampleCount() > 1)
            {
                this.CalculatePathData();
                this.currentDistance = this.GetDistanceAtIndex(startIndex);
                this.lastSegmentIndex = Mathf.Max(0, startIndex - 1);

                // Initialize position and rotation immediately
                this.UpdatePositionAndRotation(0, true);
            }
        }

        public void SetEntryNode(Transform node)
        {
            this.entryNode = node;
            this.entryNodes.Clear();
            if (node != null)
            {
                this.entryNodes.Add(node);
            }

            this.entryPathDistances.Clear();
            this.ResetEntryTriggerState();
        }

        public void SetEntryNodes(List<Transform> nodes)
        {
            this.entryNodes.Clear();
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    this.entryNodes.Add(node);
                }
            }

            this.entryNode = this.entryNodes.Count > 0 ? this.entryNodes[0] : null;
            this.entryPathDistances.Clear();
            this.ResetEntryTriggerState();

            Debug.Log($"[PathFollower] SetEntryNodes: {this.entryNodes.Count} entry nodes set");
        }

        public void StartMoving()
        {
            this.isMoving = true;
        }

        public void StopMoving()
        {
            this.isMoving = false;
        }

        public bool IsMoving()
        {
            return this.isMoving;
        }

        public float GetCurrentDistance()
        {
            return this.currentDistance;
        }

        public float GetTotalPathLength()
        {
            return this.totalPathLength;
        }

        private void Update()
        {
            if (!this.isMoving || this.path == null)
            {
                return;
            }

            // For non-loop paths, use predictive movement
            if (!this.LoopPath)
            {
                var limitedMovement = this.CalculateLimitedMovement(Time.deltaTime);
                if (limitedMovement <= 0)
                {
                    return;
                }

                if (this.ReverseDirection)
                {
                    this.currentDistance -= limitedMovement;
                    if (this.currentDistance < 0)
                    {
                        this.currentDistance = 0;
                        this.StopMoving();
                    }
                }
                else
                {
                    this.currentDistance += limitedMovement;
                    if (this.currentDistance >= this.totalPathLength)
                    {
                        this.currentDistance = this.totalPathLength;
                        this.StopMoving();
                    }
                }

                this.UpdatePositionAndRotation(Time.deltaTime, false);
                return;
            }

            // For loop paths, use spacing check
            if (this.CheckSpacingAndStop())
            {
                return;
            }

            var movement = this.MoveSpeed * Time.deltaTime;
            if (this.ReverseDirection)
            {
                this.currentDistance -= movement;
                if (this.currentDistance < 0)
                {
                    this.currentDistance += this.totalPathLength;
                }
            }
            else
            {
                this.currentDistance += movement;
                if (this.currentDistance >= this.totalPathLength)
                {
                    this.currentDistance %= this.totalPathLength;
                }
            }

            this.UpdatePositionAndRotation(Time.deltaTime, false);
        }

        private float CalculateLimitedMovement(float dt)
        {
            var desiredSpacing = GameConstants.DistanceThresholds.BallSpacing;
            var maxMovement = this.MoveSpeed * dt;

            var closestAheadDistance = float.MaxValue;

            this.RebuildSiblingCacheIfNeeded();

            foreach (var otherFollower in this.cachedSiblingFollowers)
            {
                if (otherFollower == null || !otherFollower.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (this.IsWaitingAtEntry)
                {
                    return 0;
                }

                var otherDist = otherFollower.GetCurrentDistance();
                var distDiff = this.currentDistance - otherDist;

                // For reverse direction, find balls with SMALLER distance
                var isAhead = this.ReverseDirection ? (distDiff > 0) : (distDiff < 0);

                if (isAhead)
                {
                    var absDiff = Mathf.Abs(distDiff);
                    if (absDiff < closestAheadDistance)
                    {
                        closestAheadDistance = absDiff;
                    }
                }
            }

            if (closestAheadDistance >= float.MaxValue)
            {
                return maxMovement;
            }

            var safeDistance = closestAheadDistance - desiredSpacing;
            if (safeDistance <= 0)
            {
                return 0;
            }

            return Mathf.Min(maxMovement, safeDistance);
        }

        private bool CheckSpacingAndStop()
        {
            if (this.IsWaitingAtEntry)
            {
                return true;
            }

            if (this.transform.parent == null)
            {
                return false;
            }

            this.RebuildSiblingCacheIfNeeded();

            var threshold = GameConstants.DistanceThresholds.BallCollision;

            foreach (var otherFollower in this.cachedSiblingFollowers)
            {
                if (otherFollower == null || !otherFollower.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var otherDist = otherFollower.GetCurrentDistance();
                var distDiff = otherDist - this.currentDistance;

                // Handle wrap-around for looping paths
                if (this.LoopPath && this.totalPathLength > 0)
                {
                    var halfPath = this.totalPathLength / 2;
                    if (distDiff < -halfPath)
                    {
                        distDiff += this.totalPathLength;
                    }
                    else if (distDiff > halfPath)
                    {
                        distDiff -= this.totalPathLength;
                    }
                }

                var isAhead = distDiff > 0;

                if (isAhead)
                {
                    var absDiff = Mathf.Abs(distDiff);

                    if (absDiff < threshold)
                    {
                        if (!otherFollower.IsMoving() || otherFollower.IsWaitingAtEntry)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void RebuildSiblingCacheIfNeeded()
        {
            if (!this.siblingsCacheDirty)
            {
                return;
            }

            this.cachedSiblingFollowers.Clear();

            if (this.transform.parent == null)
            {
                this.siblingsCacheDirty = false;
                return;
            }

            foreach (Transform sibling in this.transform.parent)
            {
                if (sibling == this.transform)
                {
                    continue;
                }

                if (followerCache.TryGetValue(sibling, out var follower) && follower != null)
                {
                    this.cachedSiblingFollowers.Add(follower);
                }
            }

            this.siblingsCacheDirty = false;
        }

        private void MarkSiblingsCacheDirty()
        {
            if (this.transform.parent == null)
            {
                return;
            }

            foreach (Transform sibling in this.transform.parent)
            {
                if (followerCache.TryGetValue(sibling, out var follower) && follower != null)
                {
                    follower.siblingsCacheDirty = true;
                }
            }
        }

        private void UpdatePositionAndRotation(float dt, bool immediate)
        {
            if (this.path == null)
            {
                return;
            }

            var pos = this.GetPositionAtDistance(this.currentDistance, true);
            this.transform.position = pos;

            var lookAhead = this.ReverseDirection ? -0.2f : 0.2f;
            var nextPos = this.GetPositionAtDistance(this.currentDistance + lookAhead, false);

            var dir = nextPos - pos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();

                // Calculate rotation from direction
                var targetRot = Quaternion.LookRotation(dir, Vector3.up);

                // Apply 90 degree offset
                targetRot *= this.rotationOffset;

                if (immediate)
                {
                    this.transform.rotation = targetRot;
                }
                else
                {
                    var lerpFactor = Mathf.Min(1f, this.RotationSpeed * dt);
                    this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRot, lerpFactor);
                }
            }
        }

        private Vector3 GetPositionAtDistance(float targetDist, bool updateCache)
        {
            if (this.path == null)
            {
                return Vector3.zero;
            }

            if (targetDist < 0)
            {
                targetDist += this.totalPathLength;
            }

            if (targetDist > this.totalPathLength)
            {
                targetDist %= this.totalPathLength;
            }

            var count = this.path.GetSampleCount();
            var segmentCount = this.segmentLengths.Count;

            var segmentIdx = this.FindSegmentIndex(targetDist);

            if (updateCache)
            {
                this.lastSegmentIndex = segmentIdx;
            }

            if (segmentIdx >= segmentCount)
            {
                segmentIdx = segmentCount - 1;
            }

            var p1 = this.path.GetSample(segmentIdx);
            var p2 = this.path.GetSample((segmentIdx + 1) % count);
            var segLen = this.segmentLengths[segmentIdx];
            var distAtIndex = this.cumulativeDistances[segmentIdx];

            var ratio = segLen > 0 ? (targetDist - distAtIndex) / segLen : 0;
            return Vector3.Lerp(p1, p2, ratio);
        }

        // Binary search for O(log n) lookup
        private int FindSegmentIndex(float targetDist)
        {
            var cumDist = this.cumulativeDistances;
            var low = 0;
            var high = cumDist.Count - 1;

            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (cumDist[mid] <= targetDist)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return low;
        }

        private void CalculatePathData()
        {
            if (this.path == null)
            {
                return;
            }

            this.totalPathLength = 0;
            this.segmentLengths.Clear();
            this.cumulativeDistances.Clear();
            this.cumulativeDistances.Add(0);

            var count = this.path.GetSampleCount();

            for (var i = 0; i < count - 1; i++)
            {
                var d = Vector3.Distance(this.path.GetSample(i), this.path.GetSample(i + 1));
                this.segmentLengths.Add(d);
                this.totalPathLength += d;
                this.cumulativeDistances.Add(this.totalPathLength);
            }

            // Handle loop closure
            if (this.LoopPath && count > 1)
            {
                var loopDistance = Vector3.Distance(this.path.GetSample(count - 1), this.path.GetSample(0));
                this.segmentLengths.Add(loopDistance);
                this.totalPathLength += loopDistance;
                this.cumulativeDistances.Add(this.totalPathLength);
            }
        }

        private float GetDistanceAtIndex(int index)
        {
            if (index <= 0)
            {
                return 0;
            }

            if (index >= this.cumulativeDistances.Count)
            {
                return this.totalPathLength;
            }

            return this.cumulativeDistances[index];
        }

        private void ResetEntryTriggerState()
        {
            this.triggeredEntryIndices.Clear();
        }

        public float GetDistanceFromEntry()
        {
            if (this.entryNode == null)
            {
                return -1;
            }

            return Vector3.Distance(this.transform.position, this.entryNode.position);
        }

        /// <summary>
        /// Check entry points and fire signals when approaching.
        /// Call this from Update after movement.
        /// </summary>
        public void UpdateEntryPointDetection(System.Action<int> onEntryReached)
        {
            if (this.entryNodes.Count == 0)
            {
                Debug.LogWarning($"[PathFollower] No entry nodes set for follower on {this.gameObject.name}");
                return;
            }

            for (var i = 0; i < this.entryNodes.Count; i++)
            {
                var entryNode = this.entryNodes[i];
                if (entryNode == null)
                {
                    Debug.LogWarning($"[PathFollower] Entry node {i} is NULL!");
                    continue;
                }

                var dist = Vector3.Distance(this.transform.position, entryNode.position);

                // Log distance for first row only to avoid spam (using static counter)
                if (Time.frameCount % 60 == 0 && i == 0)
                {
                    Debug.Log($"[PathFollower] dist to entry: {dist:F2} (threshold: {GameConstants.DistanceThresholds.EntryTrigger})");
                }

                if (this.triggeredEntryIndices.Contains(i))
                {
                    // Reset trigger when far enough away
                    if (dist > GameConstants.DistanceThresholds.FillReset)
                    {
                        this.triggeredEntryIndices.Remove(i);
                    }

                    continue;
                }

                // Check if close enough to trigger
                if (dist < GameConstants.DistanceThresholds.EntryTrigger)
                {
                    Debug.Log($"[PathFollower] TRIGGERED! Row at dist {dist:F2}");
                    this.triggeredEntryIndices.Add(i);
                    onEntryReached?.Invoke(i);
                }
            }
        }

        /// <summary>
        /// Get distance along path to a specific entry node.
        /// </summary>
        public float GetDistanceToEntryAlongPath(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= this.entryNodes.Count)
            {
                return float.MaxValue;
            }

            var entryNode = this.entryNodes[entryIndex];
            if (entryNode == null)
            {
                return float.MaxValue;
            }

            // Get pre-computed entry path distance
            if (this.entryPathDistances.TryGetValue(entryIndex, out var cachedDist))
            {
                // Calculate signed distance from current position to cached entry distance
                var diff = cachedDist - this.currentDistance;

                // Handle wrap-around for looping paths
                if (this.LoopPath && this.totalPathLength > 0)
                {
                    if (diff < -this.totalPathLength / 2)
                    {
                        diff += this.totalPathLength;
                    }
                    else if (diff > this.totalPathLength / 2)
                    {
                        diff -= this.totalPathLength;
                    }
                }

                return Mathf.Abs(diff);
            }

            // Fallback to direct distance if not cached
            return Vector3.Distance(this.transform.position, entryNode.position);
        }

        /// <summary>
        /// Pre-compute entry node path distances. Call after initialization.
        /// </summary>
        public void ComputeEntryPathDistances()
        {
            if (this.path == null)
            {
                return;
            }

            this.entryPathDistances.Clear();

            for (var i = 0; i < this.entryNodes.Count; i++)
            {
                var entryNode = this.entryNodes[i];
                if (entryNode == null)
                {
                    continue;
                }

                // Find closest path position to entry node
                var closestDist = 0f;
                var minDist = float.MaxValue;

                var count = this.path.GetSampleCount();
                var stepSize = this.totalPathLength / count;

                for (var j = 0; j < count; j++)
                {
                    var pathDist = j * stepSize;
                    var pathPos = this.GetPositionAtDistance(pathDist, false);
                    var dist = Vector3.Distance(pathPos, entryNode.position);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestDist = pathDist;
                    }
                }

                this.entryPathDistances[i] = closestDist;
            }
        }
    }
}
