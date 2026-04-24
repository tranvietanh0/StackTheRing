# Project Changelog — Stack The Ring

All notable changes to this project will be documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added
- Documentation sync with current runtime architecture, level content, and authoring workflow.
- Level content expanded through `Level_24` in both prefab and `LevelData` assets.
- New gameplay colors available in runtime and level authoring: `Brown`, `Black`, `Lime`.
- Hidden bucket authoring and reveal-chain gameplay via `LevelData.HiddenBuckets`, `Bucket.IsHidden`, and `BucketColumnManager.RevealNeighborHiddenBuckets(...)`.
- Multi-lane queue authoring via `LevelData.QueueLanes` for new levels.

### Changed
- Docs now reflect the active bucket-grid / collect-area / queue-lane architecture instead of older slot/collector descriptions.
- `MainSceneScope` bootstrap currently auto-loads level `23` during startup.

---

## [0.2.0] - 2026-04-10 (Core Gameplay)

### Added
- **Core Systems**
  - `GameConstants` — Shared gameplay constants and color helpers.
  - `ColorType` enum — Initial runtime color set, later extended in follow-up content updates.

- **Conveyor System**
  - `ConveyorController` — Spline-based conveyor belt management.
  - `ConveyorPath` — Cached path sample data.
  - `ConveyorConfig` — ScriptableObject configuration.
  - `PathFollower` — Spline following component.
  - `QueueConveyor` + `ConveyorFeeder` — Queue rows that feed back into the main conveyor.

- **Ring/Ball System**
  - `Ball` — Individual ball component with color and jump/landing flow.
  - `RowBall` — Container for a moving row of balls on conveyor paths.

- **Bucket / Collect Area System**
  - `Bucket` — Runtime bucket state, hidden/revealed visuals, progress tracking, completion animation.
  - `BucketColumnManager` — Spawns bucket layout, resolves eligible buckets, reveals neighbors, auto-places buckets into collect areas.
  - `CollectAreaManager` — Collect-area slot occupancy.
  - `CollectAreaBucketService` — Query/business logic for matching balls to active buckets.

- **Level System**
  - `LevelManager` (`ILevelManager`) — Level loading, progression, save.
  - `LevelData` — ScriptableObject level configuration for rings, bucket layout, hidden buckets, and queue lanes.
  - `LevelController` — Runtime coordinator for an instantiated level.

- **Game States**
  - `GamePlayState` (`ITickable`) — Main gameplay loop with win/lose detection.
  - `GameWinState` — Level completed state.
  - `GameLoseState` — Game over state.

- **Signals**
  - Gameplay: `AllRingsClearedSignal`, `LevelStartSignal`, `LevelWinSignal`, `LevelLoseSignal`.
  - Bucket flow: `BucketTappedSignal`, `BucketJumpedToAreaSignal`, `BucketCompletedSignal`.
  - Conveyor flow: `RowBallReachEntrySignal`, `RowBallCompletedLoopSignal`, `BallCollectedSignal`.
  - Legacy compatibility signals remain registered during migration.

- **Editor Tools**
  - `SplineSetupEditor` — Spline path visualization helper.

### Dependencies
- Dreamteck Splines for conveyor path system.

---

## [0.1.0] - 2026-04-08 (Initial Setup)

### Added
- Unity 6 (6000.3.10f1) project setup
- VContainer dependency injection (v1.16.9)
- UniTask async framework (v2.5.10)
- MessagePipe pub/sub messaging (v1.8.1)
- Addressables asset system (v2.9.0)
- DOTween Pro for animations

### Project Structure
- `GameLifetimeScope` — Root DI container
- `LoadingSceneScope` + `MainSceneScope` — Per-scene DI
- `GameStateMachine` — State management system
- `LoadingScreenPresenter` — Initial loading flow

### Git Submodules
- `GameFoundationCore` — Core framework services
- `UITemplate` — UI MVP framework
- `Extensions` — Utility helpers
- `Logging` — Logging abstractions

### Scenes
- `0.LoadingScene` — Entry point, data loading
- `1.MainScene` — Main game scene

---

## Version History Template

### [X.Y.Z] - YYYY-MM-DD

#### Added
- New features

#### Changed
- Changes in existing functionality

#### Deprecated
- Soon-to-be removed features

#### Removed
- Removed features

#### Fixed
- Bug fixes

#### Security
- Vulnerability fixes

---

## Commit Convention

This project follows [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `style` | Formatting, no code change |
| `refactor` | Code change, no feature/fix |
| `perf` | Performance improvement |
| `test` | Adding/updating tests |
| `chore` | Build, CI, dependencies |

### Scopes

| Scope | Area |
|-------|------|
| `core` | GameFoundationCore changes |
| `ui` | UI/Screen system |
| `state` | State machine |
| `assets` | Addressables, assets |
| `di` | Dependency injection |
| `docs` | Documentation |

### Examples

```
feat(state): add GamePlayState with level logic

fix(ui): prevent double-tap on play button

docs(readme): update build instructions

chore(deps): upgrade VContainer to 1.17.0
```
