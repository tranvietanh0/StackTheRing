namespace HyperCasualGame.Scripts.Effects
{
    using System;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    /// <summary>
    /// Handles wobble animation when ring lands on bucket pole.
    /// Simulates realistic ring toss physics with damped oscillation.
    /// </summary>
    public class RingLandingEffect : MonoBehaviour
    {
        private Sequence wobbleSequence;

        /// <summary>
        /// Play the landing effect: wobble + settle animation.
        /// </summary>
        /// <param name="targetLocalPos">Final local position on stack</param>
        /// <param name="onComplete">Callback when animation completes</param>
        public async UniTask PlayLandingEffect(Vector3 targetLocalPos, Action onComplete = null)
        {
            this.wobbleSequence?.Kill();

            // Random initial tilt direction (perpendicular to Y axis)
            var randomAngle = UnityEngine.Random.Range(0f, 360f);
            var tiltDirection = new Vector3(
                Mathf.Cos(randomAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(randomAngle * Mathf.Deg2Rad)
            ).normalized;

            var stepDuration = GameConstants.WobbleConfig.WobbleDuration / (GameConstants.WobbleConfig.OscillationCount + 1);

            // Build wobble sequence
            this.wobbleSequence = DOTween.Sequence();

            // Initial position with bounce offset
            var startY = targetLocalPos.y + GameConstants.WobbleConfig.BounceHeight;
            this.transform.localPosition = new Vector3(targetLocalPos.x, startY, targetLocalPos.z);

            // Add Y bounce (settles to target)
            this.wobbleSequence.Join(
                this.transform.DOLocalMoveY(targetLocalPos.y, GameConstants.WobbleConfig.WobbleDuration * 0.4f)
                    .SetEase(Ease.OutBounce)
            );

            // Build wobble oscillations
            var currentAmplitude = GameConstants.WobbleConfig.InitialTiltAngle;

            for (var i = 0; i < GameConstants.WobbleConfig.OscillationCount; i++)
            {
                // Alternate direction each swing
                var angle = currentAmplitude * (i % 2 == 0 ? 1f : -1f);
                var targetRotation = Quaternion.AngleAxis(angle, tiltDirection);

                this.wobbleSequence.Append(
                    this.transform.DOLocalRotateQuaternion(targetRotation, stepDuration)
                        .SetEase(Ease.InOutSine)
                );

                // Dampen amplitude for next swing
                currentAmplitude *= GameConstants.WobbleConfig.DampingFactor;
            }

            // Final settle to rest (identity rotation)
            this.wobbleSequence.Append(
                this.transform.DOLocalRotateQuaternion(Quaternion.identity, stepDuration * 0.5f)
                    .SetEase(Ease.OutSine)
            );

            // Wait for completion
            var tcs = new UniTaskCompletionSource();
            this.wobbleSequence.OnComplete(() =>
            {
                // Ensure final state is clean
                this.transform.localRotation = Quaternion.identity;
                this.transform.localPosition = targetLocalPos;

                onComplete?.Invoke();
                tcs.TrySetResult();
            });

            await tcs.Task;
        }

        /// <summary>
        /// Stop any ongoing wobble animation.
        /// </summary>
        public void StopEffect()
        {
            this.wobbleSequence?.Kill();
            this.wobbleSequence = null;
        }

        private void OnDestroy()
        {
            this.StopEffect();
        }
    }
}
