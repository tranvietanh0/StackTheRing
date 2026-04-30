# Performance Baseline Report — Stack The Ring

## Context

Plan: `plans/260501-performance-optimization-plan.md`

Goal: profile-first baseline before code changes, targeting mobile 60 FPS and loading spike reduction.

## Environment

| Item | Value |
|------|-------|
| Unity project | `UnityStackTheRing` |
| Unity version from `ProjectSettings/ProjectVersion.txt` | `2022.3.35f1` |
| Unity MCP project info | `2022.3.35f1`, platform `Android` |
| Active start scene | `Assets/Scenes/0.LoadingScene.unity` |
| Runtime scene after Play | `Assets/Scenes/1.MainScene.unity` |
| Note | Project docs/CLAUDE mention Unity 6, but actual project is Unity 2022.3.35f1. Treat Unity version mismatch as a documentation/config blocker before release validation. |

## MCP/tooling notes

- Unity MCP had 2 connected editor instances; active instance was pinned to `UnityStackTheRing@eb7556fc5b105955` before collecting data.
- `manage_scene(action="get_loaded_scenes")` is exposed in the client schema but unsupported by the active Unity package. Fallback used: `editor_state` and `manage_scene(action="get_active")`.
- Available project tools include `manage_graphics`, `read_console`, `find_gameobjects`, `run_tests`, `manage_camera`, `manage_scene`.

## Console baseline

After clearing console and entering Play Mode from `0.LoadingScene`, one error appears:

```text
Failed to set the cursor because the specified texture ('Colorful_toy_ring_stack_with_motion') was not CPU accessible.
```

Impact: likely not the main gameplay lag source, but it is a runtime error and should be fixed or removed before final validation.

## Scene/runtime baseline

Play Mode transitioned from loading scene to main scene successfully.

Main scene root objects:

- `HyperCasualRootUI`
- `Main Camera`
- `Directional Light`
- `EventSystem`
- `MainSceneScope`
- `LevelRoot`

Gameplay object counts in current loaded level:

| Component | Count |
|-----------|-------|
| `PathFollower` | 245 |
| `RowBall` | 245 |
| `Ball` | 1225 |
| `Bucket` | 4 |
| `ConveyorController` | 1 |
| `QueueConveyor` | 1 |

Interpretation: current level is a heavy benchmark candidate. Any per-frame scan over `PathFollower`/`RowBall`/`Ball`, per-frame `GetComponent`, LINQ allocation, or path/sibling scan can scale badly.

## Rendering/memory snapshot via MCP

Editor Play Mode snapshot:

| Metric | Value |
|--------|-------|
| Draw calls | 0 via MCP stats |
| Batches | 0 via MCP stats |
| Render textures | 23 |
| Render texture memory | ~245 MB |
| Total allocated | ~330.71 MB |
| Total reserved | ~903.39 MB |
| Unused reserved | ~572.68 MB |
| Mono used | ~221.64 MB |
| Mono heap | ~485.41 MB |
| Graphics driver | ~351.67 MB |

Note: draw/batch counters returning 0 suggests MCP rendering stats are not sufficient as the only source for frame/render profiling in this editor state. Use Unity Profiler/Profile Analyzer on device for final numeric FPS/frame-time validation.

## Baseline conclusion

Phase 1 has enough evidence to start Phase 2 safely:

1. The currently loaded benchmark level contains 245 moving row/path objects and 1225 balls.
2. Scout identified hot code paths that run per frame or frequently against these object sets.
3. Console has one runtime error unrelated to main lag but must be tracked.
4. Full mobile profiler data is still required before claiming 60 FPS done, but not required to begin low-risk hot-path allocation/cache work.

## Recommended next implementation phase

Start Phase 2 from the plan:

- `ConveyorController.cs`
- `PathFollower.cs`
- `QueueConveyor.cs`
- `ConveyorFeeder.cs`
- `CollectAreaBucketService.cs`
- `GamePlayState.cs`

Focus first on measured/static-evidence hot spots:

- remove per-frame LINQ/list allocations in hot paths;
- cache `PathFollower` references instead of repeated `GetComponent` loops;
- reduce lose-condition full scans from every tick if safe;
- avoid closure/log string allocations in gameplay loops;
- preserve behavior with smoke tests for queue, bucket, hidden/locked, win/lose.

## Blockers before final performance claim

- Need actual Development Build profiler capture on target mobile device for FPS/P95/GC numbers.
- Need resolve Unity version mismatch in documentation or project setup.
- Need fix or intentionally remove cursor texture runtime error.
