# Stack The Ring

## Giới thiệu / Introduction
- **Tiếng Việt**: Stack The Ring là game hyper-casual Unity 6000.3.10f1, định hình lại gameplay qua conveyor → bucket → CollectArea, tất cả được điều phối bằng VContainer/UniTask/SignalBus.
- **English**: Stack The Ring is a Unity 6000.3.10f1 hyper-casual game where spline-driven row balls feed bucket columns that land in CollectAreas, all orchestrated via VContainer, UniTask, and SignalBus.

## Bắt đầu nhanh / Quick Start
- **Tiếng Việt**: Mở `UnityStackTheRing` bằng Unity 6000.3.10f1, chạy `Setup.bat` / `Setup.sh` để sync submodule và packages, sau đó play `0.LoadingScene` qua Addressables.
- **English**: Open `UnityStackTheRing` in Unity 6000.3.10f1, run `Setup.bat`/`Setup.sh` to sync submodules + packages, then launch `0.LoadingScene` so `LoadingScreenPresenter` pulls `1.MainScene` via Addressables.
- **Tiếng Việt**: Đọc chuỗi tài liệu trong `docs/` trước khi thay đổi logic: `codebase-summary`, `system-architecture`, `code-standards`, `project-overview-pdr`, `development-roadmap`, `project-changelog`.
- **English**: Review the `docs/` bundle before modifying gameplay: `codebase-summary`, `system-architecture`, `code-standards`, `project-overview-pdr`, `development-roadmap`, `project-changelog`.

## Cấu trúc repository / Repository layout
- **Tiếng Việt**: `UnityStackTheRing/Assets/Scripts` chứa các hệ thống Conveyor, Bucket, CollectArea, Level, Ring, Services, Signals và StateMachines; `Assets/Submodules` là GameFoundationCore/UITemplate/Extensions/Logging; `docs/` là single source of truth.
- **English**: `UnityStackTheRing/Assets/Scripts` holds Conveyor, Bucket, CollectArea, Level, Ring, Services, Signals, and StateMachines while `Assets/Submodules` hosts GameFoundationCore, UITemplate, Extensions, Logging; keep `docs/` as the single source of truth.

## Kiến trúc chính / Architecture highlights
- **Tiếng Việt**: Game vận hành qua `GameLifetimeScope` → `LoadingSceneScope` → `MainSceneScope` với `GameStateMachine` auto-discover states.
- **English**: The runtime flows through `GameLifetimeScope` → `LoadingSceneScope` → `MainSceneScope`, with `GameStateMachine` auto-discovering states.
- **Tiếng Việt**: `GameManager` kết nối `ConveyorController`, `BucketColumnManager`, `CollectAreaManager`, `CollectAreaBucketService`, xử lý `BucketTappedSignal`, `GamePlayState` tính win/lose.
- **English**: `GameManager` wires the conveyor/bucket/CollectArea trio, handles `BucketTappedSignal`, and lets `GamePlayState` drive win/lose logic.
- **Tiếng Việt**: `BucketColumnManager` dùng `LevelData.BucketColumns`, `CollectAreaBucketService` trả về slot/bucket màu, `JumpService` đa năng cho bucket/ball.
- **English**: `BucketColumnManager` reads `LevelData.BucketColumns`, `CollectAreaBucketService` provides slot/bucket color info, and `JumpService` animates both bucket and ball arcs.

## Tài liệu chi tiết / Docs index
- **Tiếng Việt**: `docs/project-overview-pdr.md` (overview + PDR), `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/code-standards.md`, `docs/development-roadmap.md`, `docs/project-changelog.md`.
- **English**: Key docs include `docs/project-overview-pdr.md` (overview + PDR), `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/code-standards.md`, `docs/development-roadmap.md`, and `docs/project-changelog.md`.

## Lưu ý / Notes
- **Tiếng Việt**: Injection qua constructor, signal là `class`, MVP trong `{Name}ScreenView.cs`.
- **English**: Continue constructor-injection, keep signals as classes, and house MVP per screen inside `{Name}ScreenView.cs`.
