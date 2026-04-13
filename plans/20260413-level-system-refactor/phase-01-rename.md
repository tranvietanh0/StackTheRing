# Phase 1: Rename GameManager → LevelController

**Effort:** S (< 1 day)
**Dependencies:** None

## Objective

Đổi tên GameManager thành LevelController để phản ánh đúng responsibility: quản lý gameplay trong 1 level cụ thể.

## Files to Modify

| File | Action | Description |
|------|--------|-------------|
| `Scripts/Core/GameManager.cs` | Rename | → `Scripts/Level/LevelController.cs` |
| `Scripts/Scenes/Main/MainSceneScope.cs` | Update | Change reference type |
| `Scripts/StateMachines/Game/States/GamePlayState.cs` | Update | Change reference if any |
| `1.MainScene.unity` | Update | Component reference (auto via Unity) |

## Implementation Steps

### Step 1: Rename File & Class

```csharp
// OLD: Scripts/Core/GameManager.cs
public class GameManager : MonoBehaviour, IInitializable

// NEW: Scripts/Level/LevelController.cs  
public class LevelController : MonoBehaviour, IInitializable
```

### Step 2: Update Namespace

```csharp
// OLD
namespace HyperCasualGame.Scripts.Core

// NEW
namespace HyperCasualGame.Scripts.Level
```

### Step 3: Update MainSceneScope Reference

```csharp
// OLD
[SerializeField] private GameManager gameManager;

// NEW
[SerializeField] private LevelController levelController;
```

### Step 4: Update Inject Call

```csharp
// OLD
this.gameManager.Inject(...)

// NEW
this.levelController.Inject(...)
```

### Step 5: Update GamePlayState (if references GameManager)

Check và update bất kỳ reference nào đến GameManager.

## Validation

```bash
# Compile check
# Unity Editor: Open project, check Console for errors
# Play mode: Verify gameplay still works
```

## Checklist

- [ ] File renamed: GameManager.cs → LevelController.cs
- [ ] Class renamed: GameManager → LevelController
- [ ] Namespace updated
- [ ] MainSceneScope updated
- [ ] GamePlayState updated (if needed)
- [ ] No compile errors
- [ ] Play mode works
