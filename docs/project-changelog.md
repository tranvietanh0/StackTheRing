# Project Changelog — Stack The Ring

All notable changes to this project will be documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Added
- Initial project documentation (`docs/`)
  - `codebase-summary.md` — Project overview and structure
  - `system-architecture.md` — Architectural patterns and data flow
  - `code-standards.md` — Coding conventions and patterns
  - `project-changelog.md` — This file
  - `development-roadmap.md` — Future development plan

---

## [0.1.0] - 2024-XX-XX (Initial Setup)

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
