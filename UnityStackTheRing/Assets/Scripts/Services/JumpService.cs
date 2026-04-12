namespace HyperCasualGame.Scripts.Services
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;
    using CollectAreaComponent = HyperCasualGame.Scripts.CollectArea.CollectArea;

    /// <summary>
    /// Service for handling jump animations (bucket to area, ball to bucket).
    /// Matches Cocos JumpService.ts
    /// </summary>
    public class JumpService
    {
        private static JumpService instance;
        public static JumpService Instance => instance ??= new JumpService();

        /// <summary>
        /// Animate a transform jumping to a target with arc trajectory.
        /// Matches Cocos onJumpingToDestinationWithPromise.
        /// </summary>
        public async UniTask JumpToDestination(
            Transform node,
            Transform targetNode,
            float height,
            float duration,
            Vector3 endRotation,
            float targetYOffset = 0f)
        {
            if (node == null || targetNode == null) return;

            var startPos = node.position;
            var startRotation = node.eulerAngles;

            var dy = startPos.y - targetNode.position.y;
            var threshold = GameConstants.BallConfig.JumpHeight * 4f;
            var multiplier = 2.2f;
            var zOffset = 1.5f;
            var needsWiderPath = dy >= threshold;
            var dynamicHeight = needsWiderPath ? height * multiplier : height;

            var tcs = new UniTaskCompletionSource();

            DOTween.To(
                () => 0f,
                t =>
                {
                    var clampedT = Mathf.Clamp01(t);

                    var targetPos = targetNode.position;
                    var lerpPos = Vector3.Lerp(startPos, targetPos, clampedT);

                    var heightFactor = Mathf.Sin(clampedT * Mathf.PI);
                    var targetY = targetPos.y + targetYOffset;
                    var y = startPos.y + (targetY - startPos.y) * clampedT + dynamicHeight * heightFactor;

                    var z = needsWiderPath
                        ? lerpPos.z + zOffset * heightFactor
                        : lerpPos.z;

                    node.position = new Vector3(lerpPos.x, y, z);

                    var currentRotation = Vector3.Lerp(startRotation, endRotation, clampedT);
                    node.eulerAngles = currentRotation;
                },
                1f,
                duration
            ).OnComplete(() => tcs.TrySetResult());

            await tcs.Task;
        }

        /// <summary>
        /// Fly a bucket to the first empty CollectArea.
        /// Matches Cocos flyBucketToCollectArea.
        /// </summary>
        public async UniTask<CollectAreaComponent> FlyBucketToCollectArea(
            Transform bucketTransform,
            List<CollectAreaComponent> collectAreas,
            JumpConfig config)
        {
            if (bucketTransform == null)
            {
                Debug.LogError("JumpService: bucketTransform is null!");
                return null;
            }

            if (collectAreas == null || collectAreas.Count == 0)
            {
                Debug.LogError("JumpService: No collect areas provided!");
                return null;
            }

            CollectAreaComponent targetArea = null;

            foreach (var area in collectAreas)
            {
                if (area != null && !area.IsOccupied)
                {
                    targetArea = area;
                    break;
                }
            }

            if (targetArea == null)
            {
                Debug.LogWarning("JumpService: No empty CollectArea found!");
                return null;
            }

            await this.JumpToDestination(
                bucketTransform,
                targetArea.transform,
                config.JumpHeight,
                config.JumpDuration,
                config.EndRotation
            );

            return targetArea;
        }
    }

    public struct JumpConfig
    {
        public float JumpHeight;
        public float JumpDuration;
        public Vector3 EndRotation;

        public static JumpConfig DefaultBucket => new()
        {
            JumpHeight = GameConstants.BucketConfig.DefaultJumpHeight,
            JumpDuration = GameConstants.BucketConfig.DefaultJumpDuration,
            EndRotation = new Vector3(
                GameConstants.BucketConfig.DefaultJumpEndRotationX,
                GameConstants.BucketConfig.DefaultJumpEndRotationY,
                GameConstants.BucketConfig.DefaultJumpEndRotationZ
            )
        };

        public static JumpConfig DefaultBall => new()
        {
            JumpHeight = GameConstants.BallConfig.JumpHeight,
            JumpDuration = GameConstants.BallConfig.JumpDuration,
            EndRotation = Vector3.zero
        };
    }
}
