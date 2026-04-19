# Brainstorm - queue full stuck sync

## Problem

- Khi main conveyor full, nhieu row tren sub conveyor van tiep tuc move/chase len, roi bi stuck noi duoi.
- User khong chap nhan fix tam theo kieu giam bug mot case roi khi full lai restore ve nhu cu.
- Muc tieu la dong bo that giua queue va main conveyor.

## Evidence

- Screenshot tool bi fail 429 balance, khong doc anh duoc.
- Nhưng code hien tai da cho thay root cause ro:
  - `QueueConveyor.Update()` moi frame goi `RefreshReadyRows()` + `ResumeActiveRows()`
  - `ConveyorFeeder` chi poll `TryGetSubInsertDistance()`
  - `ConveyorController` quyet dinh bang heuristic khoang cach quanh anchor
- Day la model polling + heuristic, khong phai shared state / occupancy.

## Root cause

1. Queue khong biet main dang "full logic"; no chi biet row dau da ready chua.
2. Main khong cap mot slot reservation ro rang cho queue.
3. PathFollower dang giai quyet va cham bang spacing heuristic, nen row sau van chase len roi ket co hoc.
4. Nghia la bug khong nam o speed/tween nua; bug nam o model state.

## Approaches

### A. Tiep tuc tweak threshold/speed

**Pros**
- Diff nho.

**Cons**
- Sai huong.
- Rat de restore bug cu khi level/prefab/frame timing doi.
- Khong giai quyet back-pressure.

### B. Them back-pressure state cho front row

**Pros**
- Tot hon hien tai.
- It pha code hon rewrite lon.

**Cons**
- Van dua vao polling.
- Row sau van co the chase len neu spacing logic lech.
- Chi giam bug, chua giai quyet tan goc.

### C. Slot / occupancy model cho junction (recommended)

**Pros**
- Giai quyet tan goc.
- Queue va main dong bo that.
- Full thi queue dung logic ngay, khong co chase/stuck gia.

**Cons**
- Scope rewrite vua-phai.
- Can doi model tu heuristic sang state machine/slot map.

## Recommended solution

- Chon **C**.

## What it means concretely

### Main conveyor

- Tai `queueInsertAnchor`, define 1 `insert slot` logic.
- Slot co 2 state:
  - `Open`
  - `Blocked`
- `Blocked` neu co row trong vung `desiredSpacing` quanh slot.

### Queue

- Queue row dau chi co 2 state:
  - `WaitingAtExit`
  - `Transferred`
- Row sau chi duoc tien len neu slot truoc no trong.
- Khong cho row sau "tu lao len roi bi dung boi physics/heuristic".

### Transfer

- Main khong poll random distance nua.
- Main hoi: `insert slot open?`
- Neu open:
  - reserve slot
  - pop front row tu queue
  - insert vao main
  - release queue slot 0
  - queue compact logic theo slot

### Key principle

- Queue movement phai la consequence cua slot trong, khong phai consequence cua tween/timing.

## Why current model fails

- `QueueConveyor` hien tai van cho row sau continue move neu `distance > queueEntryDistance + threshold`.
- Nghia la no khong biet row truoc dang bi block vi main full hay simply chua den entry.
- Day la ly do ban thay nhieu row cung move roi ket lai thanh cuc stuck.

## Success criteria

- Khi main full, chi row front wait tai exit; row sau khong chase vo ich.
- Khi slot main vua mo, front row transfer ngay.
- Sau moi transfer, queue compact mot cach on dinh theo slot truoc.
- Khong can retune lien tuc threshold/speed moi level.

## Risks

- Can thay doi kha nhieu trong `QueueConveyor` + `ConveyorFeeder` + mot phan `PathFollower`.
- Neu lam nua voi heuristic cu se de lai code cheo, kho maintain.

## Next steps

1. Stop tweak threshold/speed.
2. Define explicit slot model for queue exit + main insert.
3. Rewrite queue compact based on predecessor-slot occupancy.
4. Retest on `Level_02` first.

## Unresolved questions

- Can screenshot/video runtime de xac nhan exact visual jam shape sau khi slot model duoc lam.
