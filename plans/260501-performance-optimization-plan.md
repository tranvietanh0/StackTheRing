# Plan: Full Pipeline Performance Optimization — Stack The Ring

## Mục tiêu

Tối ưu toàn tuyến runtime cho Unity project `Stack The Ring`, ưu tiên đồng thời:

- Gameplay FPS ổn định trên mobile mục tiêu 60 FPS.
- Giảm loading spike khi vào scene/level và khi chuyển level.
- Quy trình bắt buộc: profile trước khi sửa, đo lại sau từng phase, chỉ tối ưu hotspot đã được chứng minh bằng dữ liệu.

## Success metrics

| Nhóm | Metric mục tiêu | Cách đo |
|------|-----------------|---------|
| Gameplay frame time | Median <= 16.6 ms, P95 <= 20 ms trên device mục tiêu | Unity Profiler / Profile Analyzer, Development Build |
| GC Alloc gameplay | 0 B/frame ở steady-state gameplay hoặc có whitelist rõ ràng | Profiler Timeline + GC Alloc column |
| Loading spike | Không có main-thread spike > 100 ms khi load/chuyển level sau warmup | Profiler Timeline, Addressables events |
| Runtime Instantiate/Destroy | Không Instantiate/Destroy lặp lại trong gameplay loop chính | Profiler Timeline + hierarchy markers |
| UI rebuild | Không rebuild TMP/Canvas không cần thiết trong steady-state | UI Profiler / Timeline |
| Regression | Win/lose, bucket, queue, hidden/locked bucket vẫn pass level smoke test | Unity Test Runner + manual device script |

## Assumptions

- Project đang chạy Unity 6 theo `CLAUDE.md`, nhưng `docs/project-overview-pdr.md` còn ghi Unity 2022.3.35f1; cần xác nhận bằng `ProjectSettings/ProjectVersion.txt` ở phase đo baseline.
- Không sửa code trong bước lập kế hoạch này.
- Hotspot scout đã có giá trị định hướng, nhưng không được dùng thay profiling evidence để quyết định sửa.
- Installed modules/skills context đã đọc: `unity-base`, `animation`, `audio`, `editor`, `ui` v1.62.1; activation liên quan: `t1k-plan`, `t1k-profile`, `unity-profiling`, `unity-addressables`, `unity-animation`/`litmotion`, `unity-ugui`, `unity-mobile-ui`, `unity-monobehaviour`, `unity-game-patterns`.
- Docs đã kiểm tra: `docs/system-architecture.md`, `docs/code-standards.md`, `docs/codebase-summary.md`, `docs/development-roadmap.md`, `docs/project-overview-pdr.md`, `docs/project-changelog.md`.

## Feasibility

- Reuse check: REUSE-FIRST. Tận dụng hệ thống hiện có: `LevelController`, `LevelManager`, `CollectAreaBucketService`, `ConveyorController`, `QueueConveyor`, `ConveyorFeeder`, `GamePlayState`, `LoadingScreenPresenter`, `SparkleEffectPool`, DOTween, Addressables, VContainer/SignalBus. Không tạo orchestration layer mới nếu chưa được profiler chứng minh cần thiết.
- Complexity: Complex, vì tối ưu chạm nhiều subsystem gameplay/loading/UI/VFX và cần kiểm soát regression.
- Backwards compatibility: Additive/refactor nội bộ. Level authoring hiện có (`BucketGrid`, `QueueLanes`, legacy fallback `BucketColumns`, `HasQueue`, `QueueRings`) phải giữ nguyên hành vi.
- YAGNI/KISS: Không viết framework performance tổng quát; chỉ tạo cache/pool/batch update tại hotspot đã đo được.

## Data flow và ownership tổng quát

```text
LoadingScreenPresenter
  -> BlueprintReaderManager / LevelBlueprintReader
  -> Addressables preload current + next level
  -> IGameAssets.LoadSceneAsync(1.MainScene)
  -> MainSceneScope
  -> LevelManager.LoadCurrentLevel()
  -> LevelController.Initialize(...)
  -> ConveyorController / QueueConveyor / ConveyorFeeder
  -> PathFollower movement
  -> CollectAreaBucketService target query
  -> Bucket / Ball / RowBall animation + completion
  -> GamePlayState win/lose tick
  -> GameplayScreenView UI update
```

Ownership nguyên tắc:

- Loading asset ownership: `LoadingScreenPresenter` và `LevelManager` phải có ranh giới handle/preload/release rõ ràng.
- Gameplay state ownership: `LevelController` vẫn là coordinator, không dời logic orchestration sang singleton mới.
- Matching ownership: `CollectAreaBucketService` là nơi query bucket/slot, không duplicate logic trong controller khác.
- Movement ownership: `PathFollower`/conveyor giữ ownership movement; tối ưu không phá flow signal.
- Visual ownership: `Bucket`, `Ball`, `RingLandingEffect`, `SparkleEffectPool` quản lý visual/tween/pool.

## Dependency graph

```text
Phase 1 Baseline profiling
  -> Phase 2 Gameplay CPU + allocation hotspots
      -> Phase 3 Pooling + material/tween lifecycle
          -> Phase 5 VFX/UI polish perf
  -> Phase 4 Loading + Addressables spike
      -> Phase 6 Startup/DI reflection review
Phase 7 Regression test matrix + docs sync depends on all implementation phases
```

Parallel-safe sau Phase 1:

- Phase 2 và Phase 4 có thể research song song, nhưng không merge sửa cùng lúc nếu cùng chạm `LevelManager`/`LevelController`.
- Phase 5 chỉ bắt đầu sau Phase 3 để tránh pool/tween ownership conflict.
- Phase 6 chỉ bắt đầu sau Phase 1 baseline và trước final validation.

## Phases

### Phase 1: Baseline profiling & benchmark harness — Effort: S

**Scope:** Đo baseline, tạo chuẩn pass/fail trước khi sửa.

**Files owned:**

- Không sửa code bắt buộc.
- Nếu cần tooling additive: `UnityStackTheRing/Assets/Scripts/Editor/Performance/` hoặc test/perf runner mới, nhưng chỉ khi không thể đo thủ công lặp lại.
- Output report: `plans/260501-performance-baseline-report.md` hoặc profiler artifacts ngoài source nếu quá lớn.

**Tasks:**

1. Xác nhận Unity version thực tế bằng `UnityStackTheRing/ProjectSettings/ProjectVersion.txt` và sync note nếu docs lệch.
2. Chọn 3 level benchmark đại diện:
   - Level nhẹ không queue.
   - Level có queue/multi-queue.
   - Level nặng có hidden/locked buckets và nhiều màu.
3. Build Development + Autoconnect Profiler cho Android/iOS target hoặc device proxy nếu chưa có device.
4. Record các mốc:
   - Cold start loading scene.
   - MainScene entry + current level instantiate.
   - 60 giây gameplay steady-state.
   - Bucket tap liên tục + ball collection burst.
   - Win/next level transition.
5. Capture Unity Profiler modules: CPU Timeline, Memory, GC Alloc, Rendering, UI, Addressables, DOTween markers nếu có.
6. Chốt baseline table: FPS, frame time P50/P95/P99, GC alloc/frame, spike top 10, Instantiate/Destroy count, material instance count, Addressables handle lifecycle.

**Validation:**

- Pass khi có profiler captures cho đủ benchmark levels và có top hotspots được xếp hạng theo ms/frame + alloc.
- Không bắt đầu sửa phase sau nếu thiếu before snapshot.

**Rollback:**

- Không có code change; chỉ xóa report/artifacts nếu cần.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Đo trên Editor dẫn đến tối ưu sai cho mobile | 4 | 4 | 16 | Bắt buộc có ít nhất một Development Build trên device hoặc ghi rõ blocker trước khi sửa |
| Profiler overhead làm số liệu nhiễu | 3 | 3 | 9 | Dùng nhiều run, so sánh relative delta, ưu tiên Timeline hotspot lặp lại |
| Level benchmark không đại diện content thật | 3 | 4 | 12 | Chọn level theo scout hotspot: queue, hidden/locked, nhiều ball, loading transition |

### Phase 2: Gameplay CPU scans & zero-allocation loop — Effort: L

**Scope:** Giảm cost per-frame trong gameplay loop chính sau khi Phase 1 xác nhận hotspot.

**Files owned:**

- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/PathFollower.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- `UnityStackTheRing/Assets/Scripts/Services/CollectAreaBucketService.cs`
- `UnityStackTheRing/Assets/Scripts/StateMachines/Game/States/GamePlayState.cs`

**Tasks:**

1. Review profiler evidence cho:
   - `ConveyorController.Update`, `CheckEntryPoints`, `CollectMatchingBallsAtEntry`.
   - `PathFollower.Update`, `CalculateLimitedMovement`, `CheckSpacingAndStop`, `UpdateEntryPointDetection`.
   - `GamePlayState.Tick`, `CheckLoseCondition`.
   - `CollectAreaBucketService` queries.
   - `QueueConveyor.Update`, `ConveyorFeeder.Update`.
2. Replace per-frame full scans bằng dirty-state/cache nếu profiler chứng minh:
   - Cache eligible buckets theo color/slot state trong `CollectAreaBucketService`.
   - Update cache khi signal/state thay đổi: bucket moved, incoming changed, completed, slot released.
   - Giữ `GamePlayState` lose-check theo cadence/event gate, không scan toàn bộ mỗi tick nếu state chưa đổi.
3. Loại bỏ LINQ/list allocations trong hot path đã đo được.
4. Giảm `GetComponent`/hierarchy lookup loops bằng serialized references hoặc initialization-time cache.
5. Kiểm tra spacing/entry detection của `PathFollower`: chỉ tính phần cần thiết, không duplicate check giữa row và conveyor nếu có thể reuse cached progress.
6. Đảm bảo mọi cache có invalidation rõ ràng, không silent fallback.

**Validation:**

- Unity Test Runner EditMode/PlayMode nếu đang có tests.
- Manual smoke 3 benchmark levels: bucket tap, queue feed, hidden reveal, locked unlock, win/lose.
- Profiler after snapshot: gameplay CPU frame time giảm so với baseline; GC Alloc steady-state về 0 B/frame hoặc có whitelist.
- So sánh số lần gọi/check per frame trước/sau nếu có custom profiler markers.

**Rollback:**

- Revert riêng các file phase 2; không phụ thuộc Phase 3 nếu chưa merge pooling.
- Vì phase này sở hữu gameplay logic nhiều file, không trộn sửa visual/loading trong cùng commit.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Cache invalidation sai gây miss bucket hợp lệ hoặc false lose | 4 | 5 | 20 | Trước khi sửa phải viết/ghi test matrix cho signal invalidation; sau sửa smoke hidden/locked/queue levels |
| Tối ưu scan làm thay đổi timing collection | 3 | 4 | 12 | Giữ behavior theo public outcomes; dùng replay/manual script cùng level seed |
| Loại LINQ quá rộng gây refactor lan rộng | 3 | 3 | 9 | Chỉ thay LINQ trong profiler hot path, không rewrite toàn subsystem |

### Phase 3: Runtime allocation, pooling, material, DOTween lifecycle — Effort: L

**Scope:** Giảm Instantiate/Destroy, material instance, DOTween sequence churn trong gameplay.

**Files owned:**

- `UnityStackTheRing/Assets/Scripts/Ring/RowBall.cs`
- `UnityStackTheRing/Assets/Scripts/Ring/Ball.cs`
- `UnityStackTheRing/Assets/Scripts/Bucket/Bucket.cs`
- `UnityStackTheRing/Assets/Scripts/Effects/SparkleEffectPool.cs`
- `UnityStackTheRing/Assets/Scripts/Effects/SparkleEffect.cs`
- `UnityStackTheRing/Assets/Scripts/Effects/RingLandingEffect.cs`
- Pool config/constants nếu có sẵn trong `UnityStackTheRing/Assets/Scripts/Core/`

**Tasks:**

1. Xác minh profiler evidence cho:
   - `RowBall` LINQ/Instantiate.
   - `Ball` `renderer.material`, jump/destroy.
   - `Bucket.UpdateColor`, progress UI, DOTween shake.
   - `SparkleEffectPool`/`RingLandingEffect` DOTween/VFX allocations.
2. Reuse existing pool trước; nếu chưa có, tạo pool nhỏ, feature-local cho `Ball`/`RowBall`/effect, không tạo framework tổng quát.
3. Thay runtime `renderer.material` hot path bằng shared material/MaterialPropertyBlock strategy nếu visual cho phép.
4. Tái sử dụng DOTween tweens/sequences hoặc kill/complete đúng lifecycle; tránh tạo sequence mới mỗi frame/mỗi micro event không cần thiết.
5. Kiểm soát object return-to-pool khi ball collected, row empty, bucket completed, level unload.
6. Đưa pool size vào constants/config; không hardcode magic number rải rác.

**Validation:**

- Profiler: Instantiate/Destroy biến mất khỏi steady-state gameplay, GC Alloc giảm.
- Memory snapshot: material instance count không tăng theo số ball/bucket qua nhiều level.
- Visual smoke: ball jump, bucket shake/complete, sparkle/landing effect không bị mất hoặc reuse sai vị trí.
- Transition smoke: replay 3 level liên tiếp, pool không giữ reference stale.

**Rollback:**

- Pooling changes isolated in Ring/Bucket/Effects; revert phase không ảnh hưởng loading nếu phase 4 tách commit.
- Có thể tắt pool bằng config nội bộ trong quá trình debug, nhưng không để silent fallback trong final.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Pool giữ state cũ gây lỗi màu/incoming/animation | 4 | 5 | 20 | Thiết kế reset checklist cho từng pooled type; test replay multi-level và hidden/locked bucket |
| MaterialPropertyBlock phá batching hoặc shader property sai | 3 | 4 | 12 | So sánh Rendering Profiler trước/sau; fallback có log nếu shader không hỗ trợ property |
| DOTween lifecycle sai gây tween chạy trên object inactive | 3 | 4 | 12 | Kill/rewind theo OnDisable/ReturnToPool; Memory Profiler check dangling tween target |

### Phase 4: Loading pipeline & Addressables spike control — Effort: M

**Scope:** Giảm spike khi loading scene, preload current/next level, instantiate/destroy old level.

**Files owned:**

- `UnityStackTheRing/Assets/Scripts/Scenes/Screen/LoadingScreenView.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelManager.cs`
- `UnityStackTheRing/Assets/Scripts/Level/LevelController.cs` chỉ nếu cần hook initialize/unload lifecycle; sequencing sau Phase 2 nếu cùng đụng runtime init.
- Addressables group settings assets chỉ khi profiler chứng minh config gây spike.

**Tasks:**

1. Profile Addressables events: catalog/asset load, instantiate, release, scene activation.
2. Làm rõ handle ownership:
   - `LoadingScreenPresenter` preload current + next level.
   - `LevelManager` consume/reuse preloaded handle hay load lại.
   - Old level release/destroy đúng thời điểm, không double release.
3. Nếu instantiate prefab là spike chính, lên kế hoạch staged activation/warmup:
   - preload asset trong loading.
   - instantiate khi loading UI còn che.
   - defer non-critical VFX/UI setup sau first frame nếu an toàn.
4. Nếu destroy old level spike chính, defer release/unload sau transition hoặc pool/reuse object theo phase 3 boundary.
5. Đồng bộ progress UI để không update quá dày gây TMP/UI rebuild.
6. Không loại bỏ `Resources` fallback nếu vẫn cần backwards compatibility; chỉ log rõ khi fallback xảy ra.

**Validation:**

- Before/after timeline cho cold start và next-level transition.
- Addressables Event Viewer không còn duplicate load/release bất thường.
- Loading progress không stall giả hoặc nhảy lùi.
- 3-level consecutive transition không tăng memory không kiểm soát.

**Rollback:**

- Revert riêng loading/level manager changes.
- Nếu Addressables group settings đổi, commit tách để revert asset config dễ dàng.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Sai handle ownership gây leak hoặc asset bị release khi đang dùng | 4 | 5 | 20 | Viết ownership table trước khi sửa; kiểm tra Addressables Event Viewer và multi-level transition |
| Staged activation làm level bắt đầu khi dependency chưa sẵn sàng | 3 | 5 | 15 | Gate `LevelController.Initialize` bằng readiness explicit; high risk phải mitigation trước phase |
| Progress UI update tối ưu quá mức làm UX loading kém | 2 | 3 | 6 | Giữ cadence update đủ mượt, đo UI rebuild thay vì đoán |

### Phase 5: UI, TMP, VFX, logging hot path — Effort: M

**Scope:** Tối ưu polish systems chỉ sau khi gameplay/pooling rõ ràng.

**Files owned:**

- `UnityStackTheRing/Assets/Scripts/Scenes/Screen/GameplayScreenView.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/Screen/LoadingScreenView.cs` nếu chưa hoàn tất Phase 4; phải sequence rõ.
- `UnityStackTheRing/Assets/Scripts/Bucket/Bucket.cs` phần progress UI/visual; phải sequence sau Phase 3.
- `UnityStackTheRing/Assets/Scripts/Effects/` nếu chưa hoàn tất Phase 3; không song song cùng file.
- Logging call sites trong hotspot files đã được profiler chứng minh.

**Tasks:**

1. Profile UI rebuild/TMP update trong gameplay và loading.
2. Chỉ update text/progress khi value thay đổi; throttle progress display nếu rebuild quá nhiều.
3. Kiểm tra Canvas split nếu one dirty element làm rebuild toàn HUD.
4. Tối ưu bucket progress UI: không set color/text/fill mỗi frame nếu không đổi.
5. Kiểm tra logging/string formatting trong hot path: guard log level hoặc remove debug log khỏi per-frame release path theo existing logger convention.
6. VFX burst: đảm bảo effect pool không tạo/destroy runtime; giới hạn concurrent effect bằng config.

**Validation:**

- UI Profiler: Canvas/TMP rebuild giảm trong steady-state.
- CPU Timeline: logging/string formatting không còn trong top hot path.
- Visual QA: HUD/loading/progress/bucket UI vẫn đúng.

**Rollback:**

- UI/VFX/logging commit tách riêng; revert không ảnh hưởng gameplay logic phase 2 nếu file overlap đã sequence.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Throttle UI làm progress/score hiển thị trễ hoặc sai | 3 | 3 | 9 | Test value-change cases, không throttle state change quan trọng |
| Tắt log làm mất observability khi debug bug thật | 2 | 4 | 8 | Dùng log-level guard/config, không xóa error/warning cần thiết |
| Canvas split làm layout/prefab phức tạp hơn cần thiết | 2 | 3 | 6 | Chỉ split nếu UI Profiler chứng minh rebuild là hotspot |

### Phase 6: Startup DI/reflection and scene initialization review — Effort: M

**Scope:** Giảm startup reflection/registration spike mà không phá VContainer/state machine conventions.

**Files owned:**

- `UnityStackTheRing/Assets/Scripts/Scenes/Main/MainSceneScope.cs`
- `UnityStackTheRing/Assets/Scripts/Scenes/GameLifetimeScope.cs`
- `UnityStackTheRing/Assets/Scripts/StateMachines/Game/GameStateMachine.cs`
- Chỉ đụng submodule `GameFoundationCore` nếu profiler chứng minh spike nằm trong framework và có approval riêng vì đây là git submodule.

**Tasks:**

1. Profile startup `MainSceneScope` reflection/registration cost.
2. Nếu `GameStateMachine` auto-discovery reflection là hotspot, cân nhắc cache registration hoặc explicit registration tối thiểu nhưng vẫn DRY và không hardcode mapping rải rác.
3. Review VContainer registrations: singleton/scoped đúng lifetime, tránh resolve/instantiate duplicate.
4. Đảm bảo `LevelController` inject callback không chạy nhiều lần hoặc allocate không cần thiết.
5. Không refactor DI toàn cục nếu startup spike không đáng kể so với Addressables/instantiate.

**Validation:**

- Startup timeline before/after MainSceneScope.
- PlayMode smoke: state transition GamePlay/Win/Lose vẫn đúng.
- VContainer diagnostics không có duplicate registration/resolve lỗi.

**Rollback:**

- Revert startup/DI commit riêng.
- Nếu đụng submodule, phải kiểm tra git status parent + submodule và có kế hoạch commit riêng cho submodule.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Thay reflection auto-discovery làm mất state mới trong tương lai | 3 | 4 | 12 | Nếu cần explicit registry, đặt một SSOT gần state machine và test missing registration |
| Đụng submodule làm phức tạp git/CI | 2 | 5 | 10 | Chỉ sửa submodule khi bắt buộc; chạy từ parent root; commit submodule riêng |
| Tối ưu DI không đáng kể gây lãng phí | 3 | 3 | 9 | Chỉ làm nếu Phase 1/4 chỉ ra startup reflection nằm trong top spike |

### Phase 7: Regression test matrix, device validation, docs sync — Effort: M

**Scope:** Chốt chất lượng, chứng minh đạt mục tiêu, cập nhật docs.

**Files owned:**

- Test files nếu thêm: `UnityStackTheRing/Assets/Tests/` hoặc existing test folder.
- Docs:
  - `docs/system-architecture.md`
  - `docs/codebase-summary.md`
  - `docs/code-standards.md` nếu thêm convention performance/pooling.
  - `docs/development-roadmap.md` update Optimization progress.
  - `docs/project-changelog.md`
- Plan/report artifacts trong `plans/`.

**Tasks:**

1. Tạo final before/after report với cùng benchmark của Phase 1.
2. Run Unity Test Runner full suite: EditMode + PlayMode.
3. Device validation checklist:
   - Cold start.
   - Gameplay 60 giây mỗi benchmark level.
   - Queue feed burst.
   - Hidden reveal + locked unlock.
   - Win/lose transition.
   - 3 consecutive level loads.
4. Memory soak: chơi/chuyển 5-10 level, kiểm tra memory/material/tween/pool không tăng tuyến tính.
5. Docs sync: ghi architecture/performance ownership mới, loading ownership, pool lifecycle, known measurement targets.
6. Chỉ báo done khi zero test failures hoặc có documented skipped justification.

**Validation:**

- Full test suite pass.
- Final profiler report đạt hoặc nêu rõ metric chưa đạt với blocker cụ thể.
- Docs cập nhật khớp code thực tế.

**Rollback:**

- Docs/test changes revert riêng.
- Nếu performance change nào fail regression, revert đúng phase theo file ownership đã tách commit.

**Risk:**

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Test coverage hiện tại thiếu, bug chỉ lộ khi manual | 4 | 4 | 16 | Bắt buộc manual device matrix và thêm smoke tests cho cache/pool critical path |
| Docs drift với implementation cuối | 3 | 3 | 9 | Docs sync sau final code, không viết docs giả định trước |
| Metric 60 FPS không đạt trên low-end vì GPU/draw call ngoài scope | 3 | 5 | 15 | Nếu CPU đã đạt nhưng GPU không đạt, mở follow-up rendering/asset optimization plan trước release |

## Overall Risk Assessment

| Risk | Likelihood (1-5) | Impact (1-5) | Score | Mitigation |
|------|------------------|--------------|-------|------------|
| Tối ưu không dựa trên profiler dẫn tới sửa sai chỗ | 4 | 5 | 20 | Phase 1 là hard gate; mọi phase phải có before/after capture |
| Regression gameplay do cache/pool thay đổi stateful flow | 4 | 5 | 20 | File ownership tách phase, smoke test theo level benchmark, add tests cho invalidation/reset |
| Addressables preload/release sai gây leak/crash | 4 | 5 | 20 | Ownership table, Event Viewer, multi-level memory soak trước merge |
| Scope creep thành rewrite architecture | 3 | 4 | 12 | KISS/YAGNI: chỉ sửa hotspot đã đo; giữ LevelController/LevelManager/Service boundaries |
| Không có thiết bị mobile mục tiêu để xác nhận 60 FPS | 3 | 5 | 15 | Blocker phải nêu trước cook; dùng Development Build device thật trước claim done |

Risk score >= 15 cần mitigation hoàn tất trước khi bắt đầu phase liên quan.

## Timeline

| Phase | Effort | Notes |
|-------|--------|-------|
| Phase 1: Baseline profiling & benchmark harness | S (~1 ngày) | Blocker bắt buộc cho mọi sửa code |
| Phase 2: Gameplay CPU scans & zero-allocation loop | L (~1 tuần) | Critical path gameplay FPS; phụ thuộc Phase 1 |
| Phase 3: Runtime allocation, pooling, material, DOTween lifecycle | L (~1 tuần) | Sau Phase 2 hoặc song song research, nhưng merge sau logic ổn định |
| Phase 4: Loading pipeline & Addressables spike control | M (~3 ngày) | Có thể research song song Phase 2; merge cẩn thận nếu chạm LevelController |
| Phase 5: UI, TMP, VFX, logging hot path | M (~3 ngày) | Sau Phase 3 cho VFX/Bucket overlap |
| Phase 6: Startup DI/reflection and scene initialization review | M (~3 ngày) | Chỉ làm nếu profiler chứng minh startup spike đáng kể |
| Phase 7: Regression test matrix, device validation, docs sync | M (~3 ngày) | Final gate, phụ thuộc tất cả phase sửa code |
| Total | ~4-5 tuần | Critical path: Phase 1 -> Phase 2 -> Phase 3 -> Phase 5 -> Phase 7; Phase 4/6 có thể xen kẽ sau baseline |

## Test matrix bắt buộc

| Area | Scenario | Pass/Fail |
|------|----------|-----------|
| Conveyor | Row đi qua entry point và collect đúng màu | Ball đúng màu vào bucket, không vượt capacity |
| Queue | Queue feeder chèn row khi có gap | Không overlap row, không mất row |
| Lose check | Collect area đầy nhưng còn/không còn move hợp lệ | Không false lose/false continue |
| Hidden buckets | Reveal neighbor sau bucket moved | Hidden đổi visual đúng, eligible đúng |
| Locked buckets | Unlock theo collected ball count | Progress đúng, unlock không sớm/trễ |
| Pool reset | Replay/chuyển level nhiều lần | Không stale color/tween/reference |
| Loading | Cold start + next level | Không duplicate load, progress hợp lý |
| UI | Gameplay/loading progress update | Không rebuild quá mức, hiển thị đúng |
| Memory | 5-10 level transitions | Memory/material/tween count không tăng tuyến tính |

## Rollback strategy

- Commit theo phase, không gộp gameplay logic + loading + UI/VFX trong một commit.
- Mỗi phase có profiler before/after artifact để quyết định giữ/revert.
- Nếu phase nào fail regression, revert phase đó trước; không sửa chồng thêm phase mới.
- Submodule `UnityStackTheRing/Assets/Submodules/GameFoundationCore` chỉ sửa khi có approval và commit riêng.

## Behavioral checklist

- [x] Data flows — traced loading -> level -> gameplay -> UI/VFX ownership.
- [x] Dependency graph — blockers, parallel-safe phases, critical path documented.
- [x] Risk assessment — likelihood x impact scored; score >= 15 has mitigation.
- [x] Backwards compatibility — additive/internal refactor; legacy level data fallbacks preserved.
- [x] Test matrix — phase validation and final pass/fail matrix included.
- [x] Rollback plan — phase-level rollback and commit boundaries included.
- [x] File ownership — each phase owns files; overlaps require sequencing.
- [x] Success criteria — objective profiler/test metrics listed.

## Cook handoff

Khi bắt đầu implementation, dùng:

`/t1k:cook plans/260501-performance-optimization-plan.md`
