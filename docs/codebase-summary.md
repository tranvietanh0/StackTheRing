# Codebase Summary — Stack The Ring

## Tổng quan dự án / Overview
- **Tiếng Việt**: Kho mã nằm trong `UnityStackTheRing/Assets/Scripts` dùng VContainer để kết hợp các hệ thống conveyor, bucket, CollectArea, và state machine; phần còn lại dựa vào Submodule GameFoundationCore/UITemplate để tái sử dụng DI, ScreenManager, SignalBus.
- **English**: The codebase lives under `UnityStackTheRing/Assets/Scripts`, leveraging VContainer to orchestrate conveyor, bucket, and CollectArea systems together with the auto-discovering state machine; GameFoundationCore/UITemplate submodules provide DI, screen MVP, signaling, and asset management.

## Tech stack
| Category | Technology | Version |
|----------|------------|---------|
| Engine | Unity | 6000.3.10f1 |
| Dependency Injection | VContainer | 1.16.9 |
| Async / Task | UniTask | 2.5.10 |
| Pub/Sub | MessagePipe via SignalBus | 1.8.1 |
| Asset Loading | Unity Addressables | 2.9.0 |
| Input | Unity Input System | 1.18.0 |
| Tween | DOTween Pro | — |
| Spline | Dreamteck Splines | — |

## Cấu trúc thư mục chính / Directory layout
```
StackTheRing/
├── UnityStackTheRing/
│   ├── Assets/
│   │   ├── Scripts/             # Game-specific code
│   │   │   ├── Bucket/          # Column bucket grid, Bucket, BucketColumnManager, BucketInputController
│   │   │   ├── CollectArea/      # CollectArea slots + manager
│   │   │   ├── Conveyor/         # RowBall/Conveyor spline logic
│   │   │   ├── Core/             # GameManager, GameConstants, ColorType, RingState
│   │   │   ├── Level/            # LevelData, LevelManager
│   │   │   ├── Ring/             # Ball, RowBall data
│   │   │   ├── Services/          # JumpService, CollectAreaBucketService
│   │   │   ├── Signals/           # GameSignals, BucketSignals
│   │   │   ├── StateMachines/    # Game FSM + states
│   │   │   └── Scenes/           # Scopes & screens
│   │   ├── Submodules/           # GameFoundationCore, UITemplate, Extensions, Logging
│   ├── Packages/
└── docs/
```

## Assemblies và điểm khởi động / Assemblies & Entry Points
- **Tiếng Việt**: `HyperCasualGame.Scripts` (chứa toàn bộ logic gameplay/Bucket/CollectArea), `GameFoundationCore.Scripts` và `UITemplate.Scripts` qua submodule giữ DI, MVP, SignalBus, và Blueprint Infrastructure.
- **English**: `HyperCasualGame.Scripts` hosts conveyor, bucket, state machine, and service logic while the submodules `GameFoundationCore.Scripts` and `UITemplate.Scripts` catalogue DI helpers, MVP ScreenManager, SignalBus, and blueprint utilities.
- **Entry Points**:
  1. `0.LoadingScene` → `LoadingSceneScope` → `LoadingScreenPresenter` tải `UserLocalData` + `1.MainScene` (through Addressables).
  2. `1.MainScene` → `MainSceneScope` registers `GameManager`, `GameStateMachine`, `LevelManager`.
  3. `GameManager` → initializes `ConveyorController`, `BucketColumnManager`, `CollectAreaManager`, wires `CollectAreaBucketService`, subscribes to `BucketTappedSignal`, loads level, then transitions state machine.

## Hệ thống cốt lõi / Core Gameplay Systems
### Conveyor & RowBall
- **Tiếng Việt**: `ConveyorController` dùng Dreamteck Spline để sinh `RowBall` liên tục; mỗi `RowBall` chứa một chuỗi `Ball` (màu từ `LevelData.Rings`). `PathFollower` điều khiển sự di chuyển, `ConveyorConfig` cấp tốc độ.
- **English**: `ConveyorController` uses Dreamteck Splines to spawn `RowBall` instances populated with `Ball` prefabs (colors supplied by `LevelData.Rings`); `PathFollower` drives spline movement while `ConveyorConfig` defines speed and spacing.

### Bucket Grid & Column
- **Tiếng Việt**: `BucketColumnManager` tạo `Bucket` mỗi column/row theo `LevelData.BucketColumns`, tính `TargetBallCount` chia đều theo màu, dùng `BucketInputController` để nhận tap, và gọi `Bucket.JumpToCollectArea`/`JumpService`.
- **English**: `BucketColumnManager` instantiates `Bucket` prefabs arranged by `LevelData.BucketColumns`, balances `TargetBallCount` per color, listens for taps through `BucketInputController`, and animates bucket jumps via `Bucket.JumpToCollectArea` which in turn uses `JumpService`.

### CollectArea / Service
- **Tiếng Việt**: `CollectAreaManager` duy trì tập landing pad, `CollectAreaBucketService` cung cấp danh sách bucket hiện tại trong CollectAreas, số slot còn lại, và xây dựng kế hoạch phân phối ball theo màu.
- **English**: `CollectAreaManager` tracks landing pads, while `CollectAreaBucketService` exposes the buckets currently in CollectAreas, available slot counts, and balanced assignment plans for upcoming balls.

### JumpService & Visuals
- **Tiếng Việt**: `JumpService.JumpToDestination` tạo quỹ đạo parabol dùng DOTween; `JumpConfig.DefaultBucket`/`DefaultBall` chia sẻ tham số height/duration/rotation.
- **English**: `JumpService.JumpToDestination` uses DOTween to animate parabolic arcs, with shared `JumpConfig.DefaultBucket` and `DefaultBall` to keep rotations/height consistent.

### Signals & State Machine
- **Tiếng Việt**: SignalBus (MessagePipe) truyền `RowBallCompletedLoopSignal`, `AllRingsClearedSignal`, `BucketCompletedSignal`, `BucketTappedSignal` để `GamePlayState` theo dõi win/lose; `GameStateMachine` auto-discover `IGameState` (Home/Play/Win/Lose).
- **English**: SignalBus flows include `RowBallCompletedLoopSignal`, `AllRingsClearedSignal`, `BucketCompletedSignal`, and `BucketTappedSignal` so `GamePlayState` can decide win/lose; `GameStateMachine` auto-discovers `IGameState` implementations (Home/Play/Win/Lose).

## Scenes & DI scopes / Scenes & DI
- **Tiếng Việt**: `GameLifetimeScope` root gọi `RegisterGameFoundation()`, `RegisterUITemplate()`; `LoadingSceneScope` mở `LoadingScreenPresenter`; `MainSceneScope` đăng ký `GameStateMachine`, `LevelManager`, `GameManager`.
- **English**: `GameLifetimeScope` registers GameFoundation and UITemplate cores; `LoadingSceneScope` instantiates the loading presenter, and `MainSceneScope` wire `GameStateMachine`, `LevelManager`, `GameManager`.

## Luồng dữ liệu & gameplay / Data Flow & Gameplay
1. **Khởi động**: `LoadingScreenPresenter.BindData()` tải user data, gọi `GameAssets.LoadSceneAsync("1.MainScene")`, `MainSceneScope.Configure()` build container, `GameManager` initialize systems.
2. **Thiết lập level**: `LevelManager.LoadLevel(1)` trả về `LevelData`; `GameManager.SetupLevel` gọi `ConveyorController.SetupLevel`, `BucketColumnManager.SpawnBuckets`, `CollectAreaManager.SpawnAreas`.
3. **GamePlayState**: `Enter` bật conveyor, subscribe signals; `Tick` gọi `CheckLoseCondition()` khi CollectArea full; win khi `AllRingsClearedSignal` hoặc tất cả bucket hoàn thành.
4. **Bucket lifecycle**: bucket nhận `BallCollectedSignal`, `Bucket.StartIncomingBall()`, `Bucket.CompleteIncomingBall()`, khi đủ `TargetBallCount` và incoming 0 thì phát `BucketCompletedSignal`.

## Cấu hình level & dữ liệu / Level configuration
- **Tiếng Việt**: Level designer chỉnh `Rings[]` cho màu + số lượng, `AvailableCollectors` để hiển thị bộ thu, `BucketColumns[]` cho màu mỗi cột, `BucketColumnSpacing`/`BucketRowSpacing` để điều chỉnh layout; `LevelManager` dùng Addressables/Resources.
- **English**: Designers tweak `Rings[]` for color/count, `AvailableCollectors` for collectors in play, `BucketColumns[]` for column palettes, and spacing fields; `LevelManager` loads the SO via Addressables/Resources.

## Phụ thuộc & Submodule / Dependencies & Submodules
- **Tiếng Việt**: Phụ thuộc chính: `GameFoundationCore` (SignalBus, ScreenManager, Blueprints), `UITemplate` (StateMachine, Screen flow), `Extensions` (helper), `Logging` (ILogger). Giữ một số package OpenUPM: VContainer, UniTask, MessagePipe, Addressables, InputSystem, DOTween Pro, Dreamteck Splines.
- **English**: The core dependencies are `GameFoundationCore` (signals/screens/assets), `UITemplate` (state machine/screen flow), `Extensions`, and `Logging`. OpenUPM packages include VContainer, UniTask, MessagePipe, Addressables, InputSystem plus DOTween Pro and Dreamteck Splines.
