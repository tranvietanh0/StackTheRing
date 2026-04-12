# Project Changelog — Stack The Ring

All notable changes to this project will be documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added
- Documentation sync with latest codebase (2026-04-10)

---

## [0.2.0] - 2026-04-10 (Core Gameplay)

### Added
- **Core Systems**
  - `GameManager` — Central game orchestrator
  - `GameConstants` — Game configuration constants
  - `ColorType` enum — Red, Yellow, Green, Blue

- **Conveyor System**
  - `ConveyorController` — Spline-based conveyor belt management
  - `ConveyorPath` — Cached path sample data
  - `ConveyorConfig` — ScriptableObject configuration
  - `PathFollower` — Spline following component (Dreamteck integration)

- **Ring/Ball System**
  - `Ball` — Individual ball component with color & animations
  - `RowBall` — Container for 5 balls moving together
  - `RowBallConfig` — Row spawn configuration

- **Slot System**
  - `SlotManager` — Manages 4 stacking slots
  - `Slot` — Individual slot with stack logic
  - `ColorCollector` — Tap-to-place color selector
  - `CollectorPanel` — UI panel for collectors

- **Attraction System**
  - `AttractionController` — Ball-to-slot attraction with curved paths
  - `AttractionConfig` — Attraction zone & animation settings

- **Level System**
  - `LevelManager` (ILevelManager) — Level loading, progression, save
  - `LevelData` — ScriptableObject level configuration

- **Game States**
  - `GamePlayState` (ITickable) — Main gameplay loop with win/lose detection
  - `GameWinState` — Level completed state
  - `GameLoseState` — Game over state

- **Signals (15 new)**
  - Collector: `CollectorTappedSignal`, `CollectorPlacedSignal`
  - Ball: `BallCollectedSignal`, `BallAttractedSignal`, `BallStackedSignal`
  - Stack: `StackClearedSignal`
  - Row: `RowBallCompletedLoopSignal`
  - Game: `AllRingsClearedSignal`, `LevelStartSignal`, `LevelWinSignal`, `LevelLoseSignal`

- **Editor Tools**
  - `SplineSetupEditor` — Spline path visualization helper

### Dependencies
- Added Dreamteck Splines for conveyor path system

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
