# System Architecture — Stack The Ring

## Kiến trúc tổng quan / High-Level Architecture
```
Unity Engine
└── GameLifetimeScope (RegisterGameFoundation + RegisterUITemplate)
    ├── LoadingSceneScope (0.LoadingScene)
    │   └── LoadingScreenPresenter (await user data, load 1.MainScene)
    └── MainSceneScope (1.MainScene)
        ├── LevelManager (loads LevelData SO)
        ├── GameManager (conveyor + bucket + CollectArea wiring)
        └── GameStateMachine (states auto-discovered via IGameState)
```
- **Tiếng Việt**: Root `GameLifetimeScope` giữ DI core; `LoadingSceneScope` chỉ mở màn hình load, `MainSceneScope` xây dựng bộ dịch vụ gameplay.
- **English**: `GameLifetimeScope` hosts the DI core; `LoadingSceneScope` handles loading presenter while `MainSceneScope` builds gameplay services.

## Hệ thống chính / Core Systems
1. **GameManager + GameStateMachine**
   - **Tiếng Việt**: `GameManager` inject `SignalBus`, `LevelManager`, `GameStateMachine`, `CollectAreaBucketService`; khởi tạo `ConveyorController`, `BucketColumnManager`, `CollectAreaManager`, đăng ký `BucketTappedSignal`, gọi `SetupLevel()` và chuyển trạng thái sang `GamePlayState`.
   - **English**: `GameManager` receives `SignalBus`, `LevelManager`, `GameStateMachine`, and `CollectAreaBucketService`; it wires the controller/manager trio, subscribes to `BucketTappedSignal`, sets up the level, and transitions into `GamePlayState`.
2. **ConveyorController / RowBall**
   - **Tiếng Việt**: `ConveyorController.SetupLevel()` sinh `RowBall` theo `LevelData.Rings`, `PathFollower` giữ bánh răng/tốc độ, `ConveyorConfig` cung cấp tham số.
   - **English**: `ConveyorController.SetupLevel()` spawns `RowBall` sequences from `LevelData.Rings`, uses `PathFollower` to move along Dreamteck Splines, and respects `ConveyorConfig` (speed, spacing).
3. **BucketColumnManager + Bucket / JumpService**
   - **Tiếng Việt**: `BucketColumnManager.SpawnBuckets()` tạo cột bucket với `BucketConfig`, cân bằng `TargetBallCount`, `BucketInputController` raycast chạm, `Bucket.JumpToCollectArea()` gọi `JumpService.JumpToDestination`.
   - **English**: `BucketColumnManager.SpawnBuckets()` arranges bucket columns via `BucketConfig`, balances `TargetBallCount`, listens for taps via `BucketInputController`, and uses `JumpService.JumpToDestination` to animate bucket flight.
4. **CollectAreaManager + CollectAreaBucketService**
   - **Tiếng Việt**: `CollectAreaManager.SpawnAreas()` tạo landing pad, `CollectAreaBucketService` cung cấp danh sách màu bucket hiện tại, slot trống và kế hoạch cân bằng (`BuildBalancedBucketPlanByColor`).
   - **English**: `CollectAreaManager.SpawnAreas()` spawns landing pads, `CollectAreaBucketService` shares current bucket colors, available slots, and balanced bucket plans (`BuildBalancedBucketPlanByColor`).

## Luồng khởi động / Startup Flow
1. **LoadingScene**
   - **Tiếng Việt**: `LoadingScreenPresenter.BindData()` tải `UserLocalData` qua `IHandleUserDataServices`, sau đó gọi `GameAssets.LoadSceneAsync("1.MainScene")` để mở scene chính.
   - **English**: `LoadingScreenPresenter.BindData()` loads `UserLocalData` via `IHandleUserDataServices`, then calls `GameAssets.LoadSceneAsync("1.MainScene")` to open the main scene.
2. **MainScene**
   - **Tiếng Việt**: `MainSceneScope.Configure()` đăng ký `LevelManager`, `GameManager`, `GameStateMachine`; VContainer khởi tạo mọi `IInitializable`.
   - **English**: `MainSceneScope.Configure()` registers `LevelManager`, `GameManager`, and `GameStateMachine` while VContainer resolves all `IInitializable` services.
3. **GameManager.Initialize()**
   - **Tiếng Việt**: `collectAreaManager.SpawnAreas(collectAreaCount)` → `collectAreaBucketService.SetCollectAreaManager(...)` → `bucketColumnManager.Initialize(...)` → `signalBus.Subscribe<BucketTappedSignal>()` → `levelManager.LoadLevel(1)` → `GameStateMachine.TransitionTo<GamePlayState>()`.
   - **English**: `collectAreaManager.SpawnAreas(collectAreaCount)` → `collectAreaBucketService.SetCollectAreaManager(...)` → `bucketColumnManager.Initialize(...)` → `signalBus.Subscribe<BucketTappedSignal>()` → `levelManager.LoadLevel(1)` → `GameStateMachine.TransitionTo<GamePlayState>()`.

## Luồng gameplay / Gameplay Flow
1. **GamePlayState.Enter()**
   - **Tiếng Việt**: Bật conveyor, subscribe `AllRingsClearedSignal`, `RowBallCompletedLoopSignal`, `BallCollectedSignal`, `BucketCompletedSignal`.
   - **English**: Start the conveyor and subscribe to `AllRingsClearedSignal`, `RowBallCompletedLoopSignal`, `BallCollectedSignal`, and `BucketCompletedSignal`.
2. **RowBall + Conveyor**
   - **Tiếng Việt**: `ConveyorController` đẩy `RowBall` trong `ActiveRowBalls` dọc spline do `PathFollower` điều khiển.
   - **English**: `ConveyorController` pushes each `RowBall` in `ActiveRowBalls` along a spline driven by `PathFollower`.
3. **Bucket tap flow**
   - **Tiếng Việt**: `BucketColumnManager.OnBucketTapped()` chọn bucket eligible, lấy `CollectAreaManager.GetFirstEmptyArea()`, gọi `Bucket.JumpToCollectArea()`, `signalBus.Fire(new BucketJumpedToAreaSignal { ... })`.
   - **English**: `BucketColumnManager.OnBucketTapped()` picks the eligible bucket, fetches an empty CollectArea, triggers `Bucket.JumpToCollectArea()`, and fires `BucketJumpedToAreaSignal`.
4. **Bucket fill lifecycle**
   - **Tiếng Việt**: `Bucket.AddBall()` gọi `StartIncomingBall()`/`CompleteIncomingBall()`, cập nhật UI pro tiến trình, phát `BucketCompletedSignal` khi đủ `TargetBallCount` và không còn incoming.
   - **English**: `Bucket.AddBall()` balances `StartIncomingBall()`/`CompleteIncomingBall()`, updates UI progress, and fires `BucketCompletedSignal` once target capacity and incoming slots are cleared.
5. **Win / Lose checks**
   - **Tiếng Việt**: `GamePlayState.CheckLoseCondition()` nếu mọi CollectArea đầy và `CollectAreaBucketService.GetAvailableSlotCountByColor(ballColor)` = 0 thì `LevelLoseState`; `AllRingsClearedSignal` hoặc tất cả bucket hoàn thành → `GameWinState`.
   - **English**: `GamePlayState.CheckLoseCondition()` transitions to `LevelLoseState` when all CollectAreas are full and every `CollectAreaBucketService.GetAvailableSlotCountByColor(ballColor)` returns 0; `AllRingsClearedSignal` or all buckets complete triggers `GameWinState`.

## Dữ liệu & config / Data & Configuration
- **Tiếng Việt**: `LevelData` gồm `Rings[]`, `AvailableCollectors[]`, `BucketColumns[]`, `BucketColumnSpacing`, `BucketRowSpacing`, `StackLimit`. `GameConstants` gom color map, bucket config, collect area spacing, jump heights, coefficient rung.
- **English**: `LevelData` wires `Rings[]`, `AvailableCollectors[]`, `BucketColumns[]`, spacing, and stack limit. `GameConstants` centralizes color maps, bucket/collect area configs, jump heights, and other thresholds (FillPoint, RowBall spacing).

## Tín hiệu & observability / Signals & Observability
- **Tiếng Việt**: `SignalBus` (MessagePipe wrapper) khai báo `BucketSignals`, `GameSignals`; `GameManager`/`GamePlayState` đăng ký ghi log qua `ILoggerManager.GetLogger(this)` đối với event quan trọng.
- **English**: `SignalBus` (MessagePipe) declares `BucketSignals` and `GameSignals`; `GameManager` and `GamePlayState` log key transitions via `ILoggerManager.GetLogger(this)`.

## Tài nguyên ngoài / External Resources
- **Tiếng Việt**: Submodule `GameFoundationCore` cung cấp DI helpers, `ScreenManager`, `SignalBus`; `UITemplate` cung cấp `StateMachine` base. OpenUPM packages: VContainer, UniTask, MessagePipe, Addressables, InputSystem, DOTween Pro, Dreamteck Splines.
- **English**: `GameFoundationCore` handles DI utilities, `ScreenManager`, `SignalBus`, while `UITemplate` provides the base `StateMachine`. OpenUPM packages include VContainer, UniTask, MessagePipe, Addressables, InputSystem, DOTween Pro, Dreamteck Splines.
