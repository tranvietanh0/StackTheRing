# Entry Gate Timing Retest

- **Build:** Not run (Unity project requires manual/Editor build; no CLI build defined).
- **Tests:** None executed; repository currently lacks automated test suites for conveyor timing logic.
- **Coverage:** Not generated.
- **Key observations:** Entry gate reset now clears `processingAtEntry` and `entryBusyUntil` (see `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`). Gate-block decisions rely on `entryPathDistances` per follower (`PathFollower.cs`). No automated coverage or stress tests were available to validate timing; manual verification needed.
- **Static risks:** `entryBusyUntil` uses `Time.time`, so any time scale changes or pause/resume could desynchronize entry windows. `entryBusyUntil` entries persist even if `entryNodes` shrink; cleanup only runs during controller `Cleanup`, so dynamic level changes may leak old entries.
- **Next steps:** (1) Run Unity play mode tests or recorder to watch entry gate behavior after resetting state. (2) Consider adding deterministic unit/integration tests covering `entryBusyUntil` gating logic. (3) Investigate guard for `Time.time` vs Unity’s fixed time when pausing.
