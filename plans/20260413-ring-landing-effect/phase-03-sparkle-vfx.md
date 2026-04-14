# Phase 3: Sparkle VFX

**Effort**: S (1 day)
**Dependencies**: Phase 1 (Ring Stacking)
**Owner**: implementer
**Can parallelize with**: Phase 2

## Objective

Add sparkle burst VFX when ring lands on pole - particles burst outward in ring color.

## VFX Design

- **Style**: Radial burst, short-lived sparkles
- **Color**: Matches ring color (Red/Yellow/Green/Blue)
- **Emission**: Single burst, 20-30 particles
- **Lifetime**: 0.3-0.5s per particle
- **Shape**: Sphere/hemisphere, outward velocity

## Implementation

### 3.1 Add VFX Config to GameConstants

```csharp
// GameConstants.cs
public static class SparkleConfig
{
    public const int ParticleCount = 25;
    public const float ParticleLifetime = 0.4f;
    public const float BurstSpeed = 2f;
    public const float SparkleScale = 0.02f;
}
```

### 3.2 Create SparkleEffect.cs

```csharp
namespace HyperCasualGame.Scripts.Effects
{
    using HyperCasualGame.Scripts.Core;
    using UnityEngine;

    [RequireComponent(typeof(ParticleSystem))]
    public class SparkleEffect : MonoBehaviour
    {
        private ParticleSystem particles;
        private ParticleSystem.MainModule mainModule;

        private void Awake()
        {
            this.particles = this.GetComponent<ParticleSystem>();
            this.mainModule = this.particles.main;
        }

        public void Play(ColorType ringColor)
        {
            // Set color based on ring
            var color = GameConstants.GetColor(ringColor);
            this.mainModule.startColor = new ParticleSystem.MinMaxGradient(color, color * 1.2f);
            
            // Emit burst
            this.particles.Emit(GameConstants.SparkleConfig.ParticleCount);
        }

        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }
    }
}
```

### 3.3 Create RingSparkle.prefab (Particle System Setup)

Unity Particle System settings:

```yaml
Main Module:
  Duration: 1
  Looping: false
  Start Lifetime: 0.3-0.5 (random)
  Start Speed: 1.5-2.5 (random)
  Start Size: 0.02-0.04 (random)
  Start Color: White (overridden by script)
  Simulation Space: World
  Play On Awake: false
  Max Particles: 50

Emission:
  Rate over Time: 0
  Bursts: (controlled by script)

Shape:
  Shape: Sphere
  Radius: 0.05
  Emit from: Shell

Velocity over Lifetime:
  Space: Local
  Radial: 1

Color over Lifetime:
  Gradient: Full opacity -> 0% at end

Size over Lifetime:
  Curve: 1 -> 0.3 (shrink)

Renderer:
  Render Mode: Billboard
  Material: Default-Particle (or custom sparkle material)
```

### 3.4 Create SparkleEffectPool.cs (Object Pooling)

```csharp
namespace HyperCasualGame.Scripts.Effects
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class SparkleEffectPool : MonoBehaviour
    {
        [SerializeField] private SparkleEffect prefab;
        [SerializeField] private int poolSize = 5;

        private static SparkleEffectPool instance;
        public static SparkleEffectPool Instance => instance;

        private readonly Queue<SparkleEffect> pool = new();

        private void Awake()
        {
            instance = this;
            this.InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < this.poolSize; i++)
            {
                var effect = Instantiate(this.prefab, this.transform);
                effect.gameObject.SetActive(false);
                this.pool.Enqueue(effect);
            }
        }

        public async UniTask PlayAt(Vector3 position, ColorType color)
        {
            SparkleEffect effect;
            
            if (this.pool.Count > 0)
            {
                effect = this.pool.Dequeue();
            }
            else
            {
                effect = Instantiate(this.prefab, this.transform);
            }

            effect.gameObject.SetActive(true);
            effect.SetPosition(position);
            effect.Play(color);

            // Return to pool after particles die
            await UniTask.Delay(1000); // 1 second buffer
            effect.gameObject.SetActive(false);
            this.pool.Enqueue(effect);
        }
    }
}
```

### 3.5 Integrate with Landing

```csharp
// Ball.cs or RingLandingEffect.cs - After wobble starts

// Play sparkle at landing position
if (SparkleEffectPool.Instance != null)
{
    SparkleEffectPool.Instance.PlayAt(
        this.transform.position,
        this.BallColor
    ).Forget();
}
```

### 3.6 Add Pool to Scene

- Create empty "EffectsPool" GameObject in MainScene
- Add SparkleEffectPool component
- Assign RingSparkle prefab

## File Ownership

| File | Lines | Exclusive |
|------|-------|-----------|
| `Scripts/Core/GameConstants.cs` | 135-141 | Yes |
| `Scripts/Effects/SparkleEffect.cs` | All (New) | Yes |
| `Scripts/Effects/SparkleEffectPool.cs` | All (New) | Yes |
| `Prefabs/Effects/RingSparkle.prefab` | All (New) | Yes |

## Alternative: Addressables Loading

If prefab should be loaded on-demand:

```csharp
// In SparkleEffectPool
[SerializeField] private AssetReference sparkleRef;

private async UniTask LoadPrefab()
{
    var handle = Addressables.LoadAssetAsync<GameObject>(this.sparkleRef);
    await handle;
    this.prefab = handle.Result.GetComponent<SparkleEffect>();
}
```

## Verification

- [ ] Sparkle burst plays when ring lands
- [ ] Color matches ring color (Red/Yellow/Green/Blue)
- [ ] Particles spread outward naturally
- [ ] Particles fade and shrink
- [ ] No particle leak (pool works correctly)
- [ ] Multiple rapid landings don't cause issues
