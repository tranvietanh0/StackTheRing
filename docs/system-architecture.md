# System Architecture - Stack The Ring

## 1. So do tong quan <!-- updated 260426 -->

```text
GameLifetimeScope
|- RegisterGameFoundation()
`- RegisterUITemplate()

0.LoadingScene
`- LoadingSceneScope
   `- LoadingScreenPresenter
      |- Load user data
      |- Load LevelBlueprint catalog
      |- Preload current + next level asset
      `- Load 1.MainScene

1.MainScene
`- MainSceneScope
   |- Register signals
   |- Register LevelManager
   |- Register CollectAreaBucketService
   |- Register GameStateMachine
   |- Set LevelController inject callback
   `- Load current level from LocalDataController

LevelManager
`- Resolve level via LevelBlueprintReader
   `- Instantiate blueprint-selected prefab
      `- LevelController
         |- ConveyorController
         |- QueueConveyor / QueueLaneBindings (optional)
         |- ConveyorFeeder / MultiQueueCoordinator (optional)
         |- BucketColumnManager
         |- CollectAreaManager
         `- Bind references into GamePlayState
```

## 2. Thanh phan va vai tro

### 2.1 Lifetime va scene scopes

- `GameLifetimeScope`
  - root DI scope
  - dang ky framework services tu GameFoundationCore + UITemplate
- `LoadingSceneScope`
  - chi khoi tao loading screen presenter
- `MainSceneScope`
  - noi tap trung dang ky signal/service/state machine cho runtime gameplay

### 2.2 Loading pipeline <!-- updated 260426 -->

- `LoadingScreenPresenter`
  - show loading progress UI
  - load user data qua `UserDataManager`
  - load blueprint catalog qua `BlueprintReaderManager`
  - resolve startup levels qua `LevelBlueprintReader` + `LocalDataController`
  - preload current level va next level asset cho flow startup
  - load `1.MainScene` qua `IGameAssets.LoadSceneAsync(...)`

### 2.3 Level orchestration <!-- updated 260426 -->

- `LevelManager`
  - resolve level theo `LevelBlueprintReader` thay vi hardcode key theo so level
  - normalize request level: level vuot max quay ve level dau tien, level bi thieu nhay toi level available tiep theo
  - uu tien Addressables, fallback sang `Resources`
  - instantiate level prefab vao `levelRoot`
  - luu `CurrentLevel`, `HighestUnlockedLevel`, progress
  - fire `LevelStartSignal`, `LevelWinSignal`, `LevelLoseSignal`
- `LevelController`
  - la runtime coordinator chinh cua level instance
  - initialize conveyor, collect areas, bucket manager, queue feeder
  - tao `MultiQueueCoordinator` khi level dung `QueueLanes`
  - connect `CollectAreaBucketService`
  - subscribe `BucketTappedSignal`
  - chuyen state sang `GamePlayState`

## 3. Gameplay architecture

### 3.1 Main conveyor

- `ConveyorController`
  - dung Dreamteck spline de tao vong loop
  - spawn `RowBall` tu `LevelData.Rings`
  - theo doi `ActiveRowBalls`
  - kiem tra entry point trong `Update()`
  - khi row den entry, tim target bucket theo mau va cho ball nhay vao bucket

### 3.2 Queue subsystem <!-- updated 260426 -->

- `QueueConveyor`
  - giu them row cho level co queue
  - spawn tu lane data active trong `LevelData.QueueLanes` hoac fallback `QueueRings`
  - chay tren non-loop path
- `ConveyorFeeder`
  - tim gap lon nhat tren main conveyor
  - chuyen front row tu queue vao ring khi du gap
  - dong bo co/khong con queue rows cho main conveyor
- `MultiQueueCoordinator`
  - quan ly nhieu `QueueConveyor` + `ConveyorFeeder` thong qua `QueueLaneBinding[]`
  - validate lane id/binding truoc khi setup level
  - tong hop `HasPendingRows` de dong bo trang thai queue cho main conveyor

### 3.3 Bucket subsystem <!-- updated 260426 -->

- `BucketColumnManager`
  - spawn dynamic columns tu `LevelData.BucketGrid`, fallback tu legacy `BucketColumns`
  - tinh `TargetBallCount` cho tung bucket theo tong so ball cung mau
  - chi bucket dau moi cot duoc xem la eligible
  - dua bucket hop le vao collect area dau tien con trong
  - reveal hidden buckets o tren / trai / phai sau khi bucket nguon nhay vao collect area
  - track `collectedUnlockBallCount` de refresh locked buckets theo `LevelData.LockedBuckets`
- `Bucket`
  - giu state cua bucket: color, target, incoming, collected, hidden/revealed/locked
  - hidden bucket hien thi mau `Black` truoc khi reveal
  - locked bucket hien thi tien do mo khoa theo `RequiredBallsToUnlock`
  - cho ball nhay vao, cap nhat progress, xu ly complete
  - khi complete: animate, phat `BucketCompletedSignal`, release collect area slot, destroy self

### 3.4 Collect area subsystem

- `CollectAreaManager`
  - spawn va query slot collect area
  - kiem tra tat ca slot da occupied hay chua
- `CollectAreaBucketService`
  - doc bucket dang o trong collect area
  - tim stable target bucket theo color
  - tinh available slots theo color
  - co balanced plan helpers cho assignment

## 4. State machine architecture

- `GameStateMachine`
  - build tu danh sach `IGameState` auto-discover
  - `Initialize()` transition thang sang `GamePlayState`
- `GameHomeState`
  - mo `HomeScreenPresenter`
- `GamePlayState`
  - mo `GameplayScreenPresenter`
  - start/stop conveyor, queue, feeder
  - subscribe signal de quyet dinh win/lose
- `GameWinState` / `GameLoseState`
  - da ton tai nhung popup flow chua hoan tat

## 5. Signal flow quan trong

### Bucket tap flow

1. Player tap bucket
2. `BucketTappedSignal` duoc fire
3. `LevelController.OnBucketTapped(...)` tim bucket theo index
4. `BucketColumnManager.OnBucketTapped(...)` dua bucket vao collect area
5. `BucketJumpedToAreaSignal` duoc fire

### Ball collection flow

1. `ConveyorController` phat hien row den entry point
2. `CollectAreaBucketService` tra ve bucket dang target theo mau
3. `Bucket.StartIncomingBall()` duoc goi de reserve slot
4. `Ball` nhay vao bucket
5. `Bucket.AddBall()` + `Bucket.CompleteIncomingBall()`
6. `BallCollectedSignal` ho tro gameplay state tracking

### Bucket complete / reveal flow

1. Bucket du target va khong con incoming
2. Bucket chay completion animation
3. `BucketCompletedSignal` duoc fire
4. `GamePlayState` tang so bucket completed
5. Neu du tong bucket, level win

### Hidden bucket reveal flow

1. Player hoac auto-place dua bucket hop le ra khoi grid
2. `BucketColumnManager` reveal bucket o tren, ben trai, ben phai neu chung dang hidden
3. Bucket vua reveal doi tu visual `Black` sang mau that cua no
4. Bucket revelead co the tro thanh bucket hop le tiep theo trong cot / cum lien ke

### Lose-check flow

1. `GamePlayState.Tick()` goi `CheckLoseCondition()`
2. Neu moi collect area da occupied
3. Quet ball tren main conveyor va queue conveyor
4. Neu khong co ball nao con the vao bucket hop le theo mau/slot
5. `LevelLoseSignal` + transition `GameLoseState`

## 6. Data architecture <!-- updated 260426 -->

### `LevelData`

- Basic:
  - `LevelNumber`
- Main conveyor:
  - `ConveyorSpeed`
  - `Rings[]`
- Bucket layout:
  - `BucketGrid`
  - `HiddenBuckets[]`
  - `LockedBuckets[]`
  - `BucketColumnSpacing`
  - `BucketRowSpacing`
  - legacy fallback: `BucketColumns[]`
- Queue:
  - `QueueLanes[]` cho authoring moi
  - legacy fallback: `HasQueue`, `QueueRings[]`, `QueueSpeed`
- Runtime / balancing helpers:
  - `TotalRingCount`
  - `TotalQueueRingCount`
  - `HasAnyQueue`
  - `GetActiveQueueLanes()`
- Validation:
  - `ValidateBucketGridForRuntime()`
  - `ValidateHiddenBucketReachability()` de tranh soft-lock do reveal chain khong the giai
  - validate hidden/locked bucket config trung lap, out-of-range, va conflict hidden+locked
- Extra placeholders / carry-over fields:
  - `AvailableCollectors`
  - `StackLimit`
  - `HasHiddenRings`
  - `BlockedSlotCount`

### `LevelBlueprint` catalog

- `LevelBlueprintReader` doc mapping `Level -> LevelName`
- la SSOT cho startup preload, level normalize, va next-level progression
- cho phep content progression khong phu thuoc thu tu file tren disk; xem them `docs/codebase-summary.md`

## 7. Kien truc asset loading <!-- updated 260426 -->

- Scenes va level duoc ho tro boi Addressables
- `LevelManager` van fallback sang `Resources` cho level prefab / level data khi can
- Hien dang co content level tu `Level_01` den `Level_30`
- `LoadingScreenPresenter` preload level hien tai va level ke tiep de giam hitch luc vao gameplay
- Addressables group `Levels.asset` va blueprint catalog can duoc cap nhat dong bo moi khi them level prefab/data moi

## 8. Architectural notes

- Tai lieu cu nhac `GameManager`, nhung runtime hien tai duoc dieu phoiboi `LevelController`
- Codebase the hien dau vet migration tu he thong cu/template/Cocos, vi vay can tach ro `legacy` va `active architecture` khi mo rong tiep
- `docs/` nen tiep tuc mo ta architecture theo ma nguon hien tai, khong theo template goc
