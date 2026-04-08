# Phase 3: Services

**Effort:** M (2-3 days)
**Dependencies:** Phase 1 (Data)
**Blocks:** Phase 4, 5

## Objective

Create service interfaces and implementations for reusable game logic.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Services/IJumpService.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Services/JumpService.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Services/IColorService.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Services/ColorService.cs` | CREATE | this phase |

## Implementation

### 1. IJumpService.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Services
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public interface IJumpService
    {
        UniTask JumpToTarget(
            Transform source,
            Transform target,
            float jumpHeight,
            float duration,
            Vector3? endRotation = null);

        UniTask JumpToPosition(
            Transform source,
            Vector3 targetPosition,
            float jumpHeight,
            float duration,
            Vector3? endRotation = null);
    }
}
```

### 2. JumpService.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Services
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using UnityEngine;

    public class JumpService : IJumpService
    {
        public async UniTask JumpToTarget(
            Transform source,
            Transform target,
            float jumpHeight,
            float duration,
            Vector3? endRotation = null)
        {
            if (source == null || target == null) return;

            await JumpToPosition(source, target.position, jumpHeight, duration, endRotation);
        }

        public async UniTask JumpToPosition(
            Transform source,
            Vector3 targetPosition,
            float jumpHeight,
            float duration,
            Vector3? endRotation = null)
        {
            if (source == null) return;

            var sequence = DOTween.Sequence();

            // Jump movement
            sequence.Append(
                source.DOJump(targetPosition, jumpHeight, 1, duration)
                    .SetEase(Ease.OutQuad)
            );

            // Optional rotation during jump
            if (endRotation.HasValue)
            {
                sequence.Join(
                    source.DORotate(endRotation.Value, duration)
                        .SetEase(Ease.Linear)
                );
            }

            await sequence.AsyncWaitForCompletion();
        }
    }
}
```

### 3. IColorService.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Services
{
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using UnityEngine;

    public interface IColorService
    {
        Color GetColor(ColorType colorType);
        void ApplyColor(Renderer renderer, ColorType colorType);
        void ApplyColor(Material material, ColorType colorType);
    }
}
```

### 4. ColorService.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Services
{
    using System.Collections.Generic;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using UnityEngine;

    public class ColorService : IColorService
    {
        private static readonly Dictionary<ColorType, Color> ColorMap = new()
        {
            { ColorType.Red, new Color(0.9f, 0.2f, 0.2f) },
            { ColorType.Blue, new Color(0.2f, 0.4f, 0.9f) },
            { ColorType.Green, new Color(0.2f, 0.8f, 0.3f) },
            { ColorType.Yellow, new Color(0.95f, 0.9f, 0.2f) },
            { ColorType.Orange, new Color(1f, 0.6f, 0.2f) },
            { ColorType.Purple, new Color(0.6f, 0.2f, 0.8f) },
            { ColorType.Pink, new Color(1f, 0.5f, 0.7f) },
            { ColorType.Cyan, new Color(0.2f, 0.9f, 0.9f) },
            { ColorType.Brown, new Color(0.6f, 0.4f, 0.2f) },
            { ColorType.Mint, new Color(0.6f, 1f, 0.7f) },
            { ColorType.Silver, new Color(0.75f, 0.75f, 0.8f) },
            { ColorType.DarkOrange, new Color(0.9f, 0.4f, 0.1f) }
        };

        public Color GetColor(ColorType colorType)
        {
            return ColorMap.TryGetValue(colorType, out var color) ? color : Color.white;
        }

        public void ApplyColor(Renderer renderer, ColorType colorType)
        {
            if (renderer == null) return;

            var material = renderer.material;
            ApplyColor(material, colorType);
        }

        public void ApplyColor(Material material, ColorType colorType)
        {
            if (material == null) return;

            var color = GetColor(colorType);
            material.color = color;

            // Also set emission for glow effect (optional)
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color * 0.3f);
            }
        }
    }
}
```

## Verification

- [ ] All 4 files compile without errors
- [ ] IJumpService/JumpService use UniTask (not Task)
- [ ] ColorService maps all 12 ColorType values
- [ ] No static singletons — services will be registered via DI

## Notes

- Services are stateless — can be Singleton lifetime in DI
- JumpService uses DOTween (already in project)
- ColorService can be extended with material presets later
- Constructor injection only — no `[Inject]` attributes
