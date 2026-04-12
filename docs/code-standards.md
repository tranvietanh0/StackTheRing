# Code Standards — Stack The Ring

## Quy tắc đặt tên / Naming Conventions
| Element | Convention | Example |
|---------|------------|---------|
| Namespace | PascalCase, phản ánh thư mục | `HyperCasualGame.Scripts.Bucket` |
| Class / Interface | PascalCase / `I` prefix | `BucketColumnManager`, `IGameState` |
| Method & Property | PascalCase | `Initialize()`, `CollectedBallCount` |
| Field (private) | camelCase, `readonly` nếu bất biến | `private readonly SignalBus signalBus;` |
| Parameter / local | camelCase | `(SignalBus signalBus)` / `var targetArea = ...` |
| Constant | PascalCase hoặc UPPER_SNAKE | `DefaultJumpDuration`, `MAX_CONVEYOR_SPEED` |

## Tổ chức thư mục / File Organization
- **Tiếng Việt**: Sắp xếp thư mục theo chức năng (`Core`, `Conveyor`, `Bucket`, `CollectArea`, `Level`, `Services`, `Signals`, `StateMachines`, `Scenes`); mỗi file chứa một class hoặc MVP trừ các presenter theo quy tắc `{Name}ScreenView.cs`.
- **English**: Structure folders by feature (`Core`, `Conveyor`, `Bucket`, `CollectArea`, `Level`, `Services`, `Signals`, `StateMachines`, `Scenes`); prefer one class per file except MVP files named `{Name}ScreenView.cs` that include model/view/presenter together.

## Dependency Injection & Lifetime / DI và vòng đời
- **Tiếng Việt**: `GameLifetimeScope` đăng ký GameFoundation + UITemplate, `LoadingSceneScope` và `MainSceneScope` thêm `ILevelManager`, `GameManager`, `GameStateMachine`; mọi dependency cấp qua constructor, dùng `#region Inject` để nhóm.
- **English**: `GameLifetimeScope` registers GameFoundation/UITemplate cores while `LoadingSceneScope`/`MainSceneScope` register scene-specific services (LevelManager, GameManager, GameStateMachine); dependencies flow through constructors and stay grouped inside `#region Inject`.

## Đồng bộ bất đồng bộ & MVP / Async + MVP patterns
- **Tiếng Việt**: Luôn dùng `UniTask` cho async, `DOTween` cho animation, `await ... .Forget()` chỉ khi muốn fire-and-forget; presenter kế thừa `BaseScreenPresenter<TView>`/`BasePopupPresenter`, `BindData()` không thêm hậu tố "Async" vì framework định nghĩa.
- **English**: All async work uses `UniTask`, DOTween animates timed effects, and `.Forget()` is limited to safe fire-and-forget cases; presenters inherit `BaseScreenPresenter<TView>`/`BasePopupPresenter` and override `BindData()` (the base API lacks an `Async` suffix).

## Quy ước gameplay đặc thù / Gameplay-specific conventions
1. **Bucket / CollectArea**
   - **Tiếng Việt**: `BucketColumnManager` tính toán `TargetBallCount` theo `LevelData.BucketColumns`, lắng nghe `BucketTappedSignal`, gọi `CollectAreaManager.GetFirstEmptyArea()` và khởi động `Bucket.JumpToCollectArea` → `JumpService`.
   - **English**: `BucketColumnManager` evenly divides `TargetBallCount` per color, listens for `BucketTappedSignal`, finds the next `CollectArea`, and triggers `Bucket.JumpToCollectArea` which uses `JumpService`.
2. **CollectAreaBucketService**
   - **Tiếng Việt**: Dịch vụ này trả về danh sách màu bucket đang chiếm CollectArea, số slot còn lại (`Bucket.GetRemainingSlotCount()`), và xây dựng kế hoạch cân bằng ball theo màu.
   - **English**: This service exposes CollectArea bucket colors, remaining slots (`Bucket.GetRemainingSlotCount()`), and balanced bucket plans for upcoming balls.
3. **GameConstants**
   - **Tiếng Việt**: Tập trung tất cả hằng số (colors, bucket config, collect area spacing, jump heights) tại `GameConstants` để tránh magic numbers trong gameplay.
   - **English**: Centralize colors, bucket/collect area configs, and jump heights inside `GameConstants` so gameplay logic references named constants instead of magic numbers.

## Tín hiệu & logging / Signals & logging
- **Tiếng Việt**: Dùng `SignalBus` (trong GameFoundationCore) với signal `class` (không phải `struct`). Các tập tin `GameSignals.cs`, `BucketSignals.cs` định nghĩa `AllRingsClearedSignal`, `BucketCompletedSignal`, `RowBallCompletedLoopSignal`, `BucketTappedSignal`, v.v.; luôn unsubscribe trong `Exit()`/`OnDestroy()`.
- **English**: SignalBus (from GameFoundationCore) works with class-based signals; `GameSignals.cs` and `BucketSignals.cs` declare `AllRingsClearedSignal`, `BucketCompletedSignal`, `RowBallCompletedLoopSignal`, `BucketTappedSignal`, etc., and subscriptions must be unwound in `Exit()`/`OnDestroy()`.

## Hướng dẫn tài liệu / Documentation guidance
- **Tiếng Việt**: Viết docs/ bằng cả tiếng Việt và tiếng Anh (tham khảo file này); comment không cần giải thích rõ tên rõ ràng, nhưng các phần phức tạp (bucket calculation, CollectArea plan) nên có `/// summary` mô tả.
- **English**: Document `docs/` in both Vietnamese and English (as shown here); avoid redundant comments for obvious code, but annotate complex behaviors (bucket balancing, CollectArea plans) with `/// summary` when needed.

## Cấm / Prohibitions
| Pattern | Lý do / Reason | Thay thế / Alternative |
|---------|----------------|------------------------|
| `[Inject]` attribute | Khó theo dõi dependency | Inject qua constructor và `Lifetime.Singleton` |
| `FindObjectOfType<T>()` / `GameObject.Find()` | Brittle, không predictable | Serialize reference hoặc inject qua container |
| Singleton static | Khó test | Đăng ký service qua VContainer |
| `async void` | Không xử lý exception | `async UniTask` |
| Magic string/number | Khó thay đổi | `GameConstants` hoặc config SO |
| `struct` signals | MessagePipe yêu cầu reference | `class` signals |
| Hệ thống slot cũ (SlotManager/AttractionController) | Đã thay bằng bucket/CollectArea | Sử dụng `BucketColumnManager`, `CollectAreaManager`, `CollectAreaBucketService` |
