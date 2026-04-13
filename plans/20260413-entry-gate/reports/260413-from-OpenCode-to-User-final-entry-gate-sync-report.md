# Final Entry Gate Sync Retest

- **Build:** Not rerun; `dotnet build UnityStackTheRing.sln` already passed per main agent.
- **Tests:** Repository lacks automated suites for the conveyor/entry gate logic, so no tests were executed.
- **Coverage:** Not generated.
- **Key observations:** `ConveyorController` now tracks `entryBusyUntil` and blocks followers near busy entries (`UpdateEntryGateBlock`/`MarkEntryBusy`). `PathFollower` honours the new `IsBlockedByEntryGate` flag early in `Update`, spacing, and collision checks.
- **Static risks:** The `entryBusyUntil` dictionary uses `Time.time` and is only cleared on controller cleanup, so time-scale changes, pause/resume, or dynamic entry-node reconfiguration could leave gates perpetually blocked; stale entries also accumulate if `entryNodes` shrink. The follower `IsBlockedByEntryGate` flag can keep a follower permanently stopped if `entryBusyUntil` entries aren’t removed after a level reset, so any missed cleanup is critical.
- **Recommendations:** (1) Validate entry gate behavior manually in Unity play mode with the new spacing logic. (2) Consider deterministic coverage (unit/integration) for `entryBusyUntil` timing and the gating flag to catch time-scale edge cases. (3) Add cleanup hooks whenever entry configuration changes or when `entryBusyUntil` expires even without a controller reset.
- **Unresolved questions:** None.
