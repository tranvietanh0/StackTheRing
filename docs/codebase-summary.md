# Codebase Summary - Stack The Ring

## 1. Repo layout

```text
StackTheRing/
|- UnityStackTheRing/         # Unity project thuc te
|- docs/                      # Tai lieu chinh
|- plans/                     # Lich su plan/refactor reports
|- README.md
|- Setup.bat / Setup.sh
```

## 2. Unity project summary

- Unity version: `6000.3.10f1`
- Main package stack:
  - VContainer `1.16.9`
  - UniTask `2.5.10`
  - MessagePipe `1.8.1`
  - Addressables `2.9.0`
  - Input System `1.18.0`
  - ProBuilder `5.2.4`
  - TextMeshPro `3.0.9`
- External submodules:
  - `GameFoundationCore`
  - `Extensions`
  - `Logging`
  - `UITemplate`

## 3. Thu muc code chinh

`UnityStackTheRing/Assets/Scripts/`

- `Bucket/`
  - `Bucket.cs`: bucket runtime state, progress, completion animation
  - `BucketColumnManager.cs`: spawn bucket grid, xac dinh bucket hop le, dua bucket vao collect area
  - `BucketInputController.cs`: nhan input tap bucket
- `CollectArea/`
  - `CollectArea.cs`: slot runtime
  - `CollectAreaManager.cs`: spawn/clear/query cac slot collect area
- `Conveyor/`
  - `ConveyorController.cs`: main loop conveyor, entry detection, collect ball
  - `QueueConveyor.cs`: queue conveyor khong loop
  - `ConveyorFeeder.cs`: chen row tu queue vao main conveyor theo gap
  - `PathFollower.cs`, `ConveyorPath.cs`, `ConveyorConfig.cs`
- `Core/`
  - `GameConstants.cs`, `ColorType.cs`, `RingState.cs`
- `Effects/`
  - `RingLandingEffect.cs`, `SparkleEffect.cs`, `SparkleEffectPool.cs`
- `Level/`
  - `LevelData.cs`: cau hinh level
  - `LevelManager.cs`: load/unload level prefab, luu progress
  - `LevelController.cs`: runtime coordinator cho level dang active
- `Models/`
  - `UserLocalData.cs`
- `Ring/`
  - `RowBall.cs`, `Ball.cs`
- `Scenes/`
  - `GameLifetimeScope.cs`, `LoadingSceneScope.cs`, `MainSceneScope.cs`
  - `Screen/`: `LoadingScreenView.cs`, `HomeScreenView.cs`, `GameplayScreenView.cs`
- `Services/`
  - `CollectAreaBucketService.cs`, `JumpService.cs`
- `Signals/`
  - `GameSignals.cs`, `BucketSignals.cs`
- `StateMachines/`
  - `GameStateMachine.cs`
  - `States/`: `GameHomeState`, `GamePlayState`, `GameWinState`, `GameLoseState`

## 4. Scene va asset runtime <!-- updated 260426 -->

- Scenes:
  - `UnityStackTheRing/Assets/Scenes/0.LoadingScene.unity`
  - `UnityStackTheRing/Assets/Scenes/1.MainScene.unity`
- Level assets:
  - Prefab levels hien co: `UnityStackTheRing/Assets/Prefabs/Levels/Level_01.prefab` ... `Level_30.prefab`
  - Level data hien co: `UnityStackTheRing/Assets/Resources/Levels/Level_01.asset` ... `Level_30.asset`
  - Startup flow khong con hardcode level `23`; `LoadingScreenPresenter` preload current level + next level theo `LevelBlueprintReader`, sau do `MainSceneScope` goi `LevelManager.LoadCurrentLevel()`
  - Progression/load order duoc dinh nghia boi blueprint `LevelBlueprint`, khong suy ra truc tiep tu so thu tu file prefab
- Addressables:
  - `UnityStackTheRing/Assets/AddressableAssetsData/AssetGroups/Levels.asset`
  - `UnityStackTheRing/Assets/AddressableAssetsData/AssetGroups/Scenes.asset`
  - `UnityStackTheRing/Assets/AddressableAssetsData/AssetGroups/UIs.asset`
  - Khi them level moi, can dong bo ca Addressables group va catalog blueprint. Xem them `docs/system-architecture.md`.

## 5. Runtime architecture thuc te <!-- updated 260426 -->

### Bootstrap

1. `GameLifetimeScope` dang ky GameFoundation + UITemplate
2. `0.LoadingScene` khoi tao `LoadingSceneScope`
3. `LoadingScreenPresenter`:
   - load user data
   - load `LevelBlueprint` qua `BlueprintReaderManager`
   - preload current level va next level theo `LevelBlueprintReader`
   - load `1.MainScene`

### Main scene

1. `MainSceneScope` dang ky:
   - signals
   - `LevelManager`
   - `CollectAreaBucketService`
   - `GameStateMachine`
2. `MainSceneScope` gan inject callback cho `LevelController`
3. `MainSceneScope` goi `LevelManager.LoadCurrentLevel()`
4. `LevelManager` normalize level theo `LevelBlueprintReader`, load prefab theo blueprint record, va fallback sang `Resources` neu Addressables khong tra asset
5. `LevelController` initialize conveyor, collect area, bucket manager, queue feeders / `MultiQueueCoordinator`
6. `LevelController` bind references vao `GamePlayState`
7. `LevelController` chuyen sang `GamePlayState`

## 6. Gameplay loop thuc te <!-- updated 260426 -->

1. Main conveyor spawn cac `RowBall` tu `LevelData.Rings`
2. Bucket layout duoc tao theo `LevelData.BucketGrid` hoac fallback tu legacy `BucketColumns`
3. Cac bucket hop le dau cot duoc auto-place vao collect area; player co the tiep tuc tap bucket hop le khac
4. Hidden bucket duoc spawn voi visual concealment va se reveal khi bucket lien ke duoc dua ra khoi grid
5. Locked bucket co the duoc author qua `LevelData.LockedBuckets`; `BucketColumnManager` dem so ball da collect de mo khoa dan
6. Khi row di qua entry point, `ConveyorController` tim bucket dang nhan mau phu hop
7. Ball cung mau nhay vao bucket cho den khi het slot hoac het ball phu hop
8. Bucket day thi phat `BucketCompletedSignal`, chay animation, huy bucket, tra lai slot collect area
9. Neu co queue, `ConveyorFeeder` hoac `MultiQueueCoordinator` chen them row vao main conveyor khi co gap
10. `GamePlayState` quyet dinh win/lose

## 7. Cac diem can luu y khi doc code <!-- updated 260426 -->

- `GameManager` khong ton tai trong code hien tai, du docs cu con nhac den
- `ColorType` hien co 12 mau: `Red`, `Yellow`, `Green`, `Blue`, `Purple`, `Orange`, `Cyan`, `DarkGray`, `Pink`, `Brown`, `Black`, `Lime`
- Codebase dang o trang thai hybrid:
  - con legacy signals `Collector*`, `Ring*`, `Stack*`
  - con legacy authoring path `BucketColumns`, `HasQueue`, `QueueRings`
  - comment nhac den Cocos/original template
- `LevelController` dong vai tro runtime coordinator quan trong nhat trong scene
- `LevelManager` da chuyen sang runtime load/progression theo `LevelBlueprintReader`; level vuot max se normalize ve level dau tien
- UI runtime hien co `LoadingScreenView`, `HomeScreenView`, `GameplayScreenView`, `WinScreenView`; cac man hinh nhu pause/settings/level select chua thay trong code

## 8. File quan trong nhat de onboard nhanh

- `UnityStackTheRing/Assets/Scripts/Scenes/Main/MainSceneScope.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelManager.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- `UnityStackTheRing/Assets/Scripts/Bucket/BucketColumnManager.cs`
- `UnityStackTheRing/Assets/Scripts/Bucket/Bucket.cs`
- `UnityStackTheRing/Assets/Scripts/Services/CollectAreaBucketService.cs`
- `UnityStackTheRing/Assets/Scripts/StateMachines/Game/States/GamePlayState.cs`
