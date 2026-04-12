# Project Overview & Product Development Requirements — Stack The Ring

## Tổng quan dự án / Project Overview
- **Tiếng Việt**: Stack The Ring là game mobile hyper-casual Unity 6000.3.10f1 với luồng gameplay dây chuyền (conveyor) ➜ bucket ➜ CollectArea, sử dụng VContainer/UniTask/MessagePipe làm xương sống kiến trúc.
- **English**: Stack The Ring is a Unity 6000.3.10f1 hyper-casual mobile game where spline-driven row balls travel along a conveyor, fill bucket columns, and land in CollectAreas; the project relies on VContainer, UniTask, and MessagePipe for DI, async, and signaling.

## Yêu cầu phát triển sản phẩm / Product Development Requirements

### Yêu cầu chức năng / Functional Requirements
1. **Cấu hình level dữ liệu / Level data configuration**
   - **Tiếng Việt**: `LevelData` ScriptableObject bao gồm `Rings`, `AvailableCollectors`, `BucketColumns`, `BucketColumnSpacing` và `BucketRowSpacing` để điều chỉnh số lượng ring, màu và lưới bucket từng level.
   - **English**: The `LevelData` ScriptableObject exposes `Rings`, `AvailableCollectors`, `BucketColumns`, and spacing parameters so designers can tune ring counts, collector palettes, and bucket grid layouts without touching code.
2. **Bucket → CollectArea → CollectAreaBucketService**
   - **Tiếng Việt**: `BucketColumnManager` sinh cột bucket theo `LevelData`, `CollectAreaManager` quản lý vị trí hạ cánh, `BucketInputController` bắt tap, và `CollectAreaBucketService` cung cấp thông tin màu/số slot còn lại để tính toán chiến lược thu thập.
   - **English**: `BucketColumnManager` spawns bucket columns per `LevelData`, `CollectAreaManager` tracks landing pads, `BucketInputController` drives taps, and `CollectAreaBucketService` exposes bucket colors and remaining slots so gameplay logic knows which balls can still be collected.
3. **JumpService và hiệu ứng**
   - **Tiếng Việt**: `JumpService` cung cấp quỹ đạo nhảy hình parabol khi bucket/ball chuyển vào CollectArea và giữ lại tham số cấu hình `JumpConfig.DefaultBucket`/`DefaultBall` để đồng nhất animation.
   - **English**: `JumpService` animates parabolic jumps for buckets and balls landing in CollectAreas, using shared `JumpConfig.DefaultBucket` and `DefaultBall` presets so motion stays consistent.
4. **Luồng game & điều kiện chiến thắng/thua**
   - **Tiếng Việt**: `GameManager` khởi tạo `ConveyorController`, `BucketColumnManager`, `CollectAreaManager`, nối signal `BucketTappedSignal` → `OnBucketTapped`, và `GamePlayState` xử lý `AllRingsClearedSignal`, `RowBallCompletedLoopSignal`, `BucketCompletedSignal`, kiểm tra mất nước move bằng `CollectAreaBucketService`.
   - **English**: The `GameManager` wires conveyor, bucket, and collect area systems, routes `BucketTappedSignal`, and `GamePlayState` subscribes to `AllRingsClearedSignal`, `RowBallCompletedLoopSignal`, and `BucketCompletedSignal` while using `CollectAreaBucketService` to detect no-move losses.
5. **State machine & screen flow**
   - **Tiếng Việt**: `GameStateMachine` (auto-discover `IGameState`) chuyển giữa `GameHomeState`, `GamePlayState`, `GameWinState`, `GameLoseState`; `ScreenManager` sử dụng MVP pattern toàn bộ trong file `{Name}ScreenView.cs`.
   - **English**: `GameStateMachine` auto-discovers `IGameState` implementations and transitions between home/play/win/lose states, while `ScreenManager` renders UI using the in-file MVP (`{Name}ScreenView.cs`) convention from GameFoundationCore.

### Yêu cầu phi chức năng / Non-functional Requirements
1. **Dependency Injection & lifetime**
   - **Tiếng Việt**: Root `GameLifetimeScope` đăng ký `RegisterGameFoundation()` + `RegisterUITemplate()`, mỗi scene có `SceneScope` (Loading/Main) cài đặt service riêng; mọi dependency được inject qua constructor (no `[Inject]`).
   - **English**: `GameLifetimeScope` registers `RegisterGameFoundation()` and `RegisterUITemplate()`, scene-specific `SceneScope`s configure their services, and all dependencies are constructor-injected (no `[Inject]` attributes) to preserve clarity.
2. **Performance & async flow**
   - **Tiếng Việt**: Mọi thao tác bất đồng bộ/animation dùng `UniTask`, DOTween, Addressables cache; CPU-bound logic cục bộ (bucket checks, row loops) được giới hạn trong `ITickable.Tick()` và `ITickable`/`ITickable` cycles.
   - **English**: All async work uses `UniTask`, DOTween, and Addressables caching; CPU-bound loops (bucket eligibility, row updates) live inside `ITickable.Tick()` or signal callbacks so the update loop stays deterministic.
3. **Hệ thống logging và tín hiệu**
   - **Tiếng Việt**: Dùng `SignalBus` (MessagePipe wrapper) với signal class (`BucketCompletedSignal`, `RowBallCompletedLoopSignal`, `AllRingsClearedSignal`, v.v.) và `ILogger` từ `ILoggerManager` để ghi trạng thái gameplay.
   - **English**: The project uses `SignalBus` (MessagePipe) with class-based signals (`BucketCompletedSignal`, `RowBallCompletedLoopSignal`, `AllRingsClearedSignal`, etc.) and `ILoggerManager` to log game state transitions.

### Ràng buộc / Constraints
1. **Phiên bản Unity & thư viện**
   - **Tiếng Việt**: Giữ nguyên Unity 6000.3.10f1, OpenUPM packages: VContainer 1.16.9, UniTask 2.5.10, MessagePipe 1.8.1, Addressables 2.9.0, InputSystem 1.18.0.
   - **English**: Maintain Unity 6000.3.10f1 and OpenUPM packages: VContainer 1.16.9, UniTask 2.5.10, MessagePipe 1.8.1, Addressables 2.9.0, InputSystem 1.18.0.
2. **docs/ là nguồn chính**
   - **Tiếng Việt**: Tất cả thông tin onboarding/kiến trúc/fonction đều nằm trong `docs/` (Project overview, code standards, system architecture, PDR, roadmap, changelog).
   - **English**: `docs/` is the single source of truth for onboarding, architecture, code standards, and planning (project overview, code standards, system architecture, PDR, roadmap, changelog).
