# Phase 2: Wobble Landing Animation

**Effort**: S (1 day)
**Dependencies**: Phase 1 (Ring Stacking)
**Owner**: implementer

## Objective

Add realistic wobble animation when ring lands on pole - damped oscillation that settles.

## Physics Reference (Ring Toss)

Real ring toss wobble:
1. Initial tilt (random direction based on approach angle)
2. Oscillation around pole axis
3. Amplitude decreases exponentially (damping)
4. Settles to rest after 3-4 swings
5. Total duration: ~0.5-0.8s

## Implementation

### 2.1 Add Wobble Config to GameConstants

```csharp
// GameConstants.cs
public static class WobbleConfig
{
    public const float InitialTiltAngle = 15f;    // Max initial tilt (degrees)
    public const float DampingFactor = 0.6f;      // Each swing reduces by this factor
    public const int OscillationCount = 4;        // Number of wobbles
    public const float WobbleDuration = 0.5f;     // Total wobble time
    public const float BounceHeight = 0.05f;      // Small Y bounce on landing
}
```

### 2.2 Create RingLandingEffect.cs

```csharp
namespace HyperCasualGame.Scripts.Effects
{
    using System;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    public class RingLandingEffect : MonoBehaviour
    {
        private Sequence wobbleSequence;

        public async UniTask PlayLandingEffect(Vector3 targetLocalPos, Action onComplete = null)
        {
            this.wobbleSequence?.Kill();
            
            // Random initial tilt direction
            var tiltAxis = UnityEngine.Random.insideUnitCircle.normalized;
            var tiltDirection = new Vector3(tiltAxis.x, 0, tiltAxis.y);
            
            var config = GameConstants.WobbleConfig;
            var stepDuration = config.WobbleDuration / config.OscillationCount;
            
            // Build wobble sequence
            this.wobbleSequence = DOTween.Sequence();
            
            // Initial tilt
            var currentAmplitude = config.InitialTiltAngle;
            
            for (int i = 0; i < config.OscillationCount; i++)
            {
                var angle = currentAmplitude * (i % 2 == 0 ? 1 : -1);
                var targetRotation = Quaternion.AngleAxis(angle, tiltDirection) * Quaternion.identity;
                
                this.wobbleSequence.Append(
                    this.transform.DOLocalRotateQuaternion(targetRotation, stepDuration)
                        .SetEase(Ease.InOutSine)
                );
                
                currentAmplitude *= config.DampingFactor;
            }
            
            // Settle to rest
            this.wobbleSequence.Append(
                this.transform.DOLocalRotateQuaternion(Quaternion.identity, stepDuration * 0.5f)
                    .SetEase(Ease.OutSine)
            );
            
            // Add Y bounce in parallel
            var bounceSeq = DOTween.Sequence();
            var startY = targetLocalPos.y + config.BounceHeight;
            this.transform.localPosition = new Vector3(targetLocalPos.x, startY, targetLocalPos.z);
            
            bounceSeq.Append(
                this.transform.DOLocalMoveY(targetLocalPos.y, config.WobbleDuration * 0.3f)
                    .SetEase(Ease.OutBounce)
            );
            
            this.wobbleSequence.Join(bounceSeq);
            
            // Wait for completion
            var tcs = new UniTaskCompletionSource();
            this.wobbleSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                tcs.TrySetResult();
            });
            
            await tcs.Task;
        }

        private void OnDestroy()
        {
            this.wobbleSequence?.Kill();
        }
    }
}
```

### 2.3 Attach Component to Ball Prefab

- Add `RingLandingEffect` component to Ball.prefab
- Or create dynamically when needed (lighter approach)

### 2.4 Integrate with Ball.JumpToBucket()

```csharp
// Ball.cs - After jump completes, before AddBall

// Get or add landing effect
var landingEffect = this.GetComponent<RingLandingEffect>() 
    ?? this.gameObject.AddComponent<RingLandingEffect>();

// Get stack position from bucket
var stackPos = targetBucket.GetNextStackPosition();

// Reparent early for local space calculations
this.transform.SetParent(targetBucket.StackRoot ?? targetBucket.transform);

// Play wobble
await landingEffect.PlayLandingEffect(stackPos);

// Now add to bucket's tracking
targetBucket.AddBallToStack(this);
```

## File Ownership

| File | Lines | Exclusive |
|------|-------|-----------|
| `Scripts/Core/GameConstants.cs` | 127-134 | Yes |
| `Scripts/Effects/RingLandingEffect.cs` | All (New) | Yes |
| `Scripts/Ring/Ball.cs` | 110-125 | Shared with Phase 1 |

## Wobble Tuning Guide

| Parameter | Feel |
|-----------|------|
| `InitialTiltAngle` ↑ | More dramatic initial tilt |
| `DampingFactor` ↑ | Slower settling (more wobbles) |
| `OscillationCount` ↑ | More precise oscillations |
| `WobbleDuration` ↑ | Slower overall animation |
| `BounceHeight` ↑ | More noticeable landing impact |

## Verification

- [ ] Ring wobbles visibly when landing
- [ ] Wobble direction varies (not always same axis)
- [ ] Amplitude decreases naturally
- [ ] Ring settles to stable rotation (identity)
- [ ] No jitter or snap at end
- [ ] Works with rapid successive landings
