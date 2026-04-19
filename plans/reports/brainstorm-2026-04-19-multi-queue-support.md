# Brainstorm - multi queue support

## Problem

- User muon support nhieu queue cho cung 1 level.
- Current runtime chi support 1 queue that.

## Current architecture reality

- `LevelData` chi co 1 queue payload:
  - `HasQueue`
  - `QueueRings`
  - `QueueSpeed`
- `LevelController` chi co 1 `QueueConveyor` + 1 `ConveyorFeeder`.
- `GamePlayState` chi start/stop/check lose tren 1 queue.
- `ConveyorFeeder` chi poll 1 queue.
- `ConveyorController` chi co 1 `queueInsertAnchor`.

=> Brutal truth: hien tai khong the support 2+ queue dung nghia ma khong refactor.

## Options

### A. Fake multi-queue

- 1 queue logic
- cac queue con lai chi visual

**Pros**
- Nhanh
- It risk

**Cons**
- Khong phai gameplay multi-queue that
- Se vo mat ngay khi user muon behavior doc lap

### B. Multi-queue that, chung 1 insert point

- Nhieu `QueueConveyor`
- 1 coordinator chon queue nao duoc feed
- 1 insert slot tren main

**Pros**
- Dung feature request
- Reuse main ring geometry

**Cons**
- Arbitration phuc tap hon
- De race/jam neu khong co coordinator ro rang

### C. Multi-queue that, moi queue 1 insert point (recommended if design cho phep)

- Nhieu `QueueConveyor`
- moi queue co `transferExitAnchor` rieng
- moi queue co `insertAnchor` rieng tren main
- 1 coordinator van nen co, nhung de hon

**Pros**
- Don gian hon B
- UX ro hon
- Giam tranh chap slot

**Cons**
- Can sua prefab path/anchors level
- Can support data model nhieu lane

## Recommendation

- Neu user can gameplay 2+ queue that: chon **C**.
- Neu chi can visual phuc tap hon: chon **A**.

Toi khong recommend B neu khong bat buoc, vi no la noi sinh bug jam/deadlock nhieu nhat.

## Design recommendations

### 1. Data model

- Thay queue singleton bang array:

```text
QueueLaneData[]
- QueueRings
- QueueSpeed
- TransferExitAnchorName / id
- InsertAnchorName / id
- Priority
```

- Bo `HasQueue`, `QueueRings`, `QueueSpeed` singleton sau migration.

### 2. Runtime boundaries

- `QueueConveyor`
  - chi own 1 lane
  - own ready rows / slot movement cua lane do

- `QueueFeedCoordinator` (new)
  - own list cac queue
  - decide queue nao duoc feed
  - reserve insert slot
  - pop row tu queue duoc chon

- `ConveyorController`
  - expose `CanAcceptAtInsertAnchor(anchor, spacing)`
  - expose `InsertRowBall(row, distance)`
  - khong nen tu chon queue

### 3. State integration

- `LevelController`
  - hold `List<QueueConveyor>`
  - hold 1 `QueueFeedCoordinator`

- `GamePlayState`
  - start/stop all queues
  - lose/win check must scan all queues

## Technology guidance

- Khong can framework moi.
- Khong can ECS/network/event bus moi.
- Chi can refactor from singleton references -> collection + coordinator.

KISS path:
- keep MonoBehaviour
- keep `QueueConveyor`
- add 1 coordinator class
- change data model to array

## Implementation strategy

### Phase 1

- Introduce `QueueLaneData[]`
- Keep old fields temporarily for migration compatibility

### Phase 2

- Make `LevelController` accept `QueueConveyor[]`
- Add `QueueFeedCoordinator`

### Phase 3

- Update `GamePlayState` checks to iterate all queues

### Phase 4

- Update prefab conventions for multiple queue anchors

### Phase 5

- Remove old singleton queue fields after migration complete

## Risks

- Biggest risk: try to bolt 2nd queue onto current singleton architecture without coordinator.
- 2nd risk: keep both old singleton fields and new multi-queue fields too long -> drift/confusion.
- 3rd risk: use same insert anchor for many queues without deterministic arbitration.

## Success criteria

- 2+ queues can exist in one level.
- Each queue keeps independent ready/slot logic.
- Main insert arbitration deterministic.
- Win/lose checks remain correct across all queues.

## Next steps

1. Chot: fake multi-queue hay multi-queue that.
2. Neu multi-queue that, chot: shared insert hay per-queue insert.
3. Toi recommend per-queue insert.
4. Sau do moi lap implementation plan.

## Unresolved questions

- User co can 2 queue feed cung luc vao 2 insert points khac nhau, hay van chi 1 row/frame max?
- Multi-queue chi dung cho `Level_03+` hay can migrate ca `Level_02`?
