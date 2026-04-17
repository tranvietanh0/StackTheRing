# Phase 01 - Research and architecture

- Date: 2026-04-17
- Priority: High
- Status: Done

## Context links

- `plans/reports/brainstorm-2026-04-17-sub-conveyor-full-port.md`
- `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA\assets\scripts\CocosBeadsOutRemasterPLA\Conveyor\SubConveyorController.ts`
- `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA\assets\scripts\CocosBeadsOutRemasterPLA\Conveyor\ConveyorManager.ts`

## Overview

- Port behavior, not literal code.
- Main owns pull timing.
- Sub owns waiting queue at entry.

## Key insights

- Cocos flow is simpler than current Unity hybrid.
- Current Unity reservation/handoff layers are main source of desync and tuning pain.

## Requirements

- Row reaches sub entry and stops.
- Main checks fill point clear, then pops front row from sub.
- Queue compacts by path movement, not logical teleport hacks.

## Architecture

- `QueueConveyor` -> ready queue + entry detection + pop front + active row bookkeeping.
- `ConveyorFeeder` -> thin poller.
- `ConveyorController` -> clear-check + insert-from-sub.

## Related code files

- `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`
- `UnityStackTheRing/Assets/Scripts/Conveyor/PathFollower.cs`

## Success criteria

- Runtime model matches Cocos mental model.

## Risk assessment

- Prefab anchor mismatch.
- Queue win/lose accounting drift.

## Security considerations

- None special.

## Next steps

- Rewrite runtime flow.
