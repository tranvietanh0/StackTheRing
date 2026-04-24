# Code Standards - Stack The Ring

## 1. Quy uoc dang duoc su dung trong code

### Naming

- Namespace theo feature: `HyperCasualGame.Scripts.<Feature>`
- Class/struct/interface: PascalCase
- Interface: prefix `I`
- Field private: camelCase
- Serialized field: camelCase + `[SerializeField]`
- Signal: ten ket thuc bang `Signal`

### Folder organization

- Chia theo gameplay feature thay vi theo technical layer thuan tuy
- Mot so feature con chia tiep thanh runtime/controller/config/state

### MVP / Screen pattern

- Man hinh dang theo pattern `{Name}ScreenView.cs`
- Model/View/Presenter co the nam cung file
- Presenter ke thua `BaseScreenPresenter<TView>`

## 2. DI va lifecycle standards

### Muc tieu kien truc

- Root scope cho core services
- Scene scope cho service theo scene
- Constructor injection la chuan uu tien
- Signal dang ky tai scene scope qua `builder.DeclareSignal<T>()`

### Thuc te hien tai

- `GameLifetimeScope` dang ky framework services
- `LoadingSceneScope` khoi dong loading screen
- `MainSceneScope` dang ky `LevelManager`, `CollectAreaBucketService`, `GameStateMachine`
- `LevelController` khong duoc constructor inject truc tiep vi la `MonoBehaviour`; thay vao do dung `SetInjectCallback(...)` + `Inject(...)`

## 3. Async, tween, event

- Async uu tien `UniTask`
- Jump/animation dung DOTween
- Event flow qua `SignalBus`
- Cac object runtime can unsubscribe trong `Exit()` hoac `OnDestroy()`

## 4. Quy uoc gameplay dang thay trong code

### Conveyor

- `ConveyorController` la owner cua danh sach `ActiveRowBalls`
- Entry point detection duoc chay trong `Update()` khi conveyor dang active
- Queue conveyor tach rieng khoi main conveyor

### Bucket

- Bucket theo doi ca `CollectedBallCount` va `IncomingBallCount`
- Slot trong bucket duoc tinh qua `GetRemainingSlotCount()`
- Bucket chi completion khi da du target va `incoming == 0`
- Hidden bucket dung state `IsHidden`; visual concealment hien tai dung `ColorType.Black` truoc khi reveal
- Reveal logic xay ra o `BucketColumnManager`, khong nam trong input layer

### Collect area

- `CollectAreaManager` chi quan ly slot occupancy
- `CollectAreaBucketService` moi la lop query/business logic de chon target bucket

### Level

- `LevelData` la nguon cau hinh level
- New levels nen uu tien `BucketGrid` thay vi `BucketColumns`
- New levels nen uu tien `QueueLanes` thay vi `HasQueue` + `QueueRings`
- Hidden bucket duoc khai bao qua `HiddenBuckets` va phai hop le voi reveal chain runtime
- `LevelManager` quan ly load/unload/save progress
- `LevelController` ket noi toan bo he thong runtime cua level instance

## 5. Tin hieu hien co

### Signal gameplay hien tai

- `BucketTappedSignal`
- `BucketJumpedToAreaSignal`
- `BucketCompletedSignal`
- `BallCollectedSignal`
- `RowBallReachEntrySignal`
- `RowBallCompletedLoopSignal`
- `AllRingsClearedSignal`
- `QueueRowTransferredSignal`
- `QueueEmptySignal`
- `LevelStartSignal`
- `LevelWinSignal`
- `LevelLoseSignal`

### Legacy signals van con trong code

- `CollectorTappedSignal`
- `CollectorPlacedSignal`
- `RingAttractedSignal`
- `RingStackedSignal`
- `StackClearedSignal`
- `RingCompletedLoopSignal`

Tai lieu va code moi nen uu tien nhom signal bucket/ball/queue/level. Nhung signal legacy chi nen xem la compatibility layer cho giai doan chuyen doi.

## 6. Cac lech chuan hien tai can biet

- `GameWinState` va `GameLoseState` dang dung `async void` trong `ShowWinScreen()` / `ShowLoseScreen()`
- Co nhieu comment nhac den "Matches Cocos ..." va logic migration
- Script `Setup.bat` / `Setup.sh` van mang assumption tu `HyperCasualTemplate`
- Docs cu tung nhac `GameManager`, slot system, va tap mau 4 color; nhung runtime hien tai la bucket-grid / collect-area / queue-lane voi 12 mau
- `MainSceneScope` hien dang hardcode bootstrap vao level `23`, can luu y khi doc docs hoac test progression

## 7. Nguyen tac cap nhat code ve sau

- Uu tien giu `LevelController` / `LevelManager` / `GamePlayState` dong vai tro ro rang, khong tao them orchestration layer trung lap neu chua can
- Neu them screen moi, tiep tuc theo file `{Name}ScreenView.cs`
- Neu them gameplay signal moi, dinh nghia bang `class`
- Neu sua docs, cap nhat `docs/` truoc, sau do rut gon vao `README.md`
