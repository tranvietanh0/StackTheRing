# Phase 5: Addressables Setup

**Effort:** S (< 1 day)
**Dependencies:** Phase 4

## Objective

Configure Addressables để load level prefabs:
- Add Level prefabs vào Addressables groups
- Setup labels cho filtering
- Test load/unload cycle

## Files to Modify

| File | Action | Description |
|------|--------|-------------|
| `AddressableAssetsData/` | Update | Add level prefabs |
| `Prefabs/Levels/*.prefab` | Update | Mark as Addressable |

## Implementation Steps

### Step 1: Create Addressables Group cho Levels

1. Window → Asset Management → Addressables → Groups
2. Create New Group: "Levels"
3. Settings:
   - Bundle Mode: Pack Together
   - Build Path: LocalBuildPath
   - Load Path: LocalLoadPath

### Step 2: Add Level Prefabs to Group

1. Select `Prefabs/Levels/Level_01.prefab`
2. Check "Addressable" in Inspector
3. Set Address: `Level_01`
4. Assign to Group: "Levels"
5. Add Label: `levels`

### Step 3: Addressables Address Convention

| Asset | Address | Labels |
|-------|---------|--------|
| Level_01.prefab | `Level_01` | `levels` |
| Level_02.prefab | `Level_02` | `levels` |
| ... | `Level_{NN}` | `levels` |

### Step 4: Fallback to Resources (Optional)

```csharp
// In LevelManager.LoadLevel()
public async UniTask<LevelController> LoadLevel(int levelNumber)
{
    var prefabKey = $"Level_{levelNumber:D2}";
    
    // Try Addressables first
    try
    {
        var handle = this.gameAssets.LoadAssetAsync<GameObject>(prefabKey);
        await handle.Task;
        
        if (handle.Result != null)
        {
            return this.InstantiateLevel(handle.Result, levelNumber);
        }
    }
    catch (System.Exception ex)
    {
        this.logger.Warning($"Addressables load failed: {ex.Message}");
    }
    
    // Fallback to Resources
    var prefab = Resources.Load<GameObject>($"Levels/{prefabKey}");
    if (prefab != null)
    {
        this.logger.Info($"Loaded level from Resources fallback");
        return this.InstantiateLevel(prefab, levelNumber);
    }
    
    this.logger.Error($"Failed to load level: {prefabKey}");
    return null;
}
```

### Step 5: Build Addressables

```bash
# Unity Editor
# Window → Asset Management → Addressables → Groups
# Build → New Build → Default Build Script
```

### Step 6: Test Load Cycle

```csharp
// Test script
async void TestLevelLoading()
{
    var levelManager = FindObjectOfType<MainSceneScope>()
        .Container.Resolve<ILevelManager>();
    
    // Load level 1
    var controller1 = await levelManager.LoadLevel(1);
    Debug.Assert(controller1 != null, "Level 1 failed to load");
    
    await UniTask.Delay(1000);
    
    // Load level 2 (should unload level 1)
    var controller2 = await levelManager.LoadLevel(2);
    Debug.Assert(controller2 != null, "Level 2 failed to load");
    Debug.Assert(controller1 == null || controller1.gameObject == null, "Level 1 not unloaded");
}
```

## Validation

- Addressables group "Levels" exists
- Level_01.prefab marked as Addressable
- Address matches expected pattern
- Build Addressables succeeds
- Runtime load từ Addressables works
- Fallback to Resources works (if configured)

## Checklist

- [x] Resources fallback implemented and tested
- [x] Level_01.prefab copied to Resources/Levels/
- [x] Runtime load từ Resources works
- [ ] Addressables group "Levels" created (manual)
- [ ] Level_01.prefab added to group (manual)
- [ ] Address: `Level_01` (manual)
- [ ] Addressables build succeeds (manual)

## Status

**Resources fallback: WORKING**

Addressables setup cần thực hiện thủ công trong Unity Editor:
1. Window → Asset Management → Addressables → Groups
2. Create New Group: "Levels"
3. Drag `Assets/Prefabs/Levels/Level_01.prefab` vào group
4. Set Address: `Level_01`
5. Build: Build → New Build → Default Build Script

## Next Steps

Sau khi hoàn thành Phase 5:

1. **Tạo thêm levels:**
   - Duplicate Level_01.prefab → Level_02.prefab
   - Create LevelData_02.asset
   - Assign LevelData_02 vào Level_02.prefab
   - Add Level_02 vào Addressables

2. **Blueprint integration (future):**
   - Load level list từ CSV
   - Dynamic level config
