# Project Overview & PDR - Stack The Ring

## 1. Tong quan du an

Stack The Ring la mot game hyper-casual Unity dang duoc to chuc quanh gameplay:

`main conveyor -> bucket vao collect area -> ball nhay vao bucket -> bucket hoan thanh -> giai phong slot`

Codebase hien tai cho thay du an dang o giai doan refactor tu template/logic cu sang pipeline bucket + collect area + queue conveyor. Tai lieu cu nhac den `GameManager` va Unity 6000 da khong con dung voi ma nguon hien tai.

## 2. Trang thai codebase hien tai

- Unity project thuc te: `UnityStackTheRing/`
- Unity version thuc te: `2022.3.35f1`
- Runtime bootstrap:
  - `GameLifetimeScope` dang ky core services
  - `LoadingSceneScope` mo `LoadingScreenPresenter`
  - `LoadingScreenPresenter` load user data, preload `Level_01`, sau do load `1.MainScene`
  - `MainSceneScope` dang ky signals, `LevelManager`, `CollectAreaBucketService`, `GameStateMachine`
  - `MainSceneScope` gan inject callback cho `LevelController` va auto load level 1
  - `LevelController` moi la diem orchestration chinh cua gameplay trong scene hien tai

## 3. Muc tieu san pham o code hien tai

### 3.1 Muc tieu gameplay

- Sinh cac `RowBall` chay tren conveyor theo `LevelData`
- Cho phep bucket hop le roi khoi luoi va nhay vao `CollectArea`
- Tu entry point cua conveyor, thu nhung ball trung mau vao bucket dang nam trong collect area
- Ho tro queue conveyor de bo sung row moi khi tren vong chinh xuat hien khoang trong
- Tinh toan thang/thua dua tren:
  - tat ca ball da duoc xoa / tat ca bucket hoan thanh
  - hoac khong con nuoc di hop le khi collect area da day

### 3.2 Muc tieu ky thuat

- Dung VContainer cho DI va scene scope
- Dung SignalBus/MessagePipe cho event flow
- Dung UniTask cho async/loading/jump orchestration
- Dung Addressables de load scene va level prefab, co fallback sang `Resources`
- Giu `docs/` lam nguon su that cho onboarding va kien truc

## 4. PDR hien tai

### 4.1 Functional requirements

1. **Loading flow**
   - Game phai bat dau tu `0.LoadingScene`
   - Loading screen phai load user data, preload `Level_01`, sau do chuyen sang `1.MainScene`

2. **Level loading**
   - `LevelManager` phai load level prefab theo key `Level_XX`
   - Thu tu uu tien: Addressables truoc, `Resources/Levels` sau
   - Level prefab phai co `LevelController`

3. **Main conveyor**
   - `ConveyorController` phai spawn `RowBall` tu `LevelData.Rings`
   - Moi row gom nhieu `Ball` cung mau
   - Conveyor phai loop tren spline va kiem tra entry points khi dang chay

4. **Bucket grid**
   - `BucketColumnManager` phai spawn bucket theo `LevelData.BucketColumns`
   - Moi bucket co `TargetBallCount` duoc phan bo theo tong so ball cung mau
   - Chi bucket dau moi cot moi duoc phep nhay vao collect area

5. **Collect area**
   - `CollectAreaManager` phai sinh so luong slot hoat dong cho level
   - Moi slot luu bucket dang chiem cho service va lose-check su dung

6. **Bucket collection logic**
   - `CollectAreaBucketService` phai xac dinh bucket hop le theo mau
   - `ConveyorController` phai thu cac ball trung mau tai entry point vao bucket dang target
   - Bucket phai theo doi `incoming` + `collected` de tranh vuot qua suc chua

7. **Queue conveyor**
   - Neu `LevelData.HasQueue = true`, queue conveyor phai spawn them rows tu `QueueRings`
   - `ConveyorFeeder` phai tim gap lon nhat tren main conveyor va chen row tu queue vao khi du dieu kien

8. **Win/Lose**
   - Win khi tat ca ball da clear va queue rong, hoac khi tat ca bucket da completed
   - Lose khi moi collect area deu occupied va khong con ball nao tren conveyor/queue co the vao bucket hop le

### 4.2 Non-functional requirements

1. **DI va lifecycle**
   - Root scope va scene scope phai tiep tuc tach biet ro rang
   - Dependency uu tien constructor injection; hien tai `LevelController` dung inject callback tu `MainSceneScope`

2. **Async va animation**
   - Async phai dung `UniTask`
   - Chuyen dong nhay bucket/ball dung DOTween

3. **Observability**
   - Signal quan trong phai di qua `SignalBus`
   - He thong log qua `ILoggerManager`

4. **Tai lieu**
   - Moi thay doi kien truc/gameplay can cap nhat trong `docs/` truoc README

## 5. Pham vi he thong hien tai

### Co san trong code

- Loading scene + main scene
- Main conveyor va queue conveyor
- Bucket grid, collect area, ball-to-bucket assignment
- State machine gom `GameHomeState`, `GamePlayState`, `GameWinState`, `GameLoseState`
- Addressables groups cho `Levels`, `Scenes`, `UIs`

### Chua hoan thien / can luu y

- `GameWinState` va `GameLoseState` chua mo popup that su
- Van con legacy signals va naming tu giai doan truoc
- Script `Setup.bat` / `Setup.sh` mang tinh template bootstrap cu, khong mo ta dung setup hang ngay hien tai

## 6. File tham chieu chinh

- `UnityStackTheRing/ProjectSettings/ProjectVersion.txt`
- `UnityStackTheRing/Packages/manifest.json`
- `UnityStackTheRing/Assets/Scripts/Scenes/GameLifetimeScope.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/Loading/LoadingSceneScope.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/Main/MainSceneScope.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/Screen/LoadingScreenView.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelManager.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelData.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- `UnityStackTheRing/Assets/Scripts/Bucket/BucketColumnManager.cs`
- `UnityStackTheRing/Assets/Scripts/Bucket/Bucket.cs`
- `UnityStackTheRing/Assets/Scripts/CollectArea/CollectAreaManager.cs`
- `UnityStackTheRing/Assets/Scripts/Services/CollectAreaBucketService.cs`
- `UnityStackTheRing/Assets/Scripts/StateMachines/Game/GameStateMachine.cs`
- `UnityStackTheRing/Assets/Scripts/StateMachines/Game/States/GamePlayState.cs`
