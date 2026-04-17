# Phase 03 - Rewire Level_02 prefab

- Date: 2026-04-17
- Priority: High
- Status: Planned

## Overview

- Align prefab anchors and queue geometry with rewritten runtime.

## Implementation steps

1. Add explicit sub entry / transfer exit anchor if needed.
2. Add explicit main fill anchor.
3. Wire serialized refs on `QueueConveyor` and `ConveyorController`.
4. Save both resource and prefab copies.

## Success criteria

- `Level_02` runs on explicit anchors, no null fallback.

## Risks

- Wrong local positions make behavior look off despite correct code.

## Next steps

- Compile and validate.
