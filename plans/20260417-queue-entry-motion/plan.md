# Sua logic queue conveyor / Queue Conveyor Motion Plan

## Overview

- Muc tieu: sua queue de chi co `1 entry point`, tu fill khi main conveyor co cho, sau moi lan fill thi queue don muot ve phia entry, va bo cam giac fill kieu bat/tat.
- Pham vi chinh: phan tich va dinh huong cho `UnityStackTheRing/Assets/Scripts/Conveyor/QueueConveyor.cs`, `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorFeeder.cs`, `UnityStackTheRing/Assets/Scripts/Conveyor/ConveyorController.cs`.
- Tham chieu: hoc tu Cocos `SubConveyorController` + `PathFollower.slideToDistance()` theo huong muon primitive motion, khong be nguyen he thong multi-sub-conveyor/cached-row.

## Requirements

### Functional

1. Queue chi duoc day vao main conveyor qua mot diem entry co dinh.
2. Khi main conveyor co du cho ngay sau entry, feeder phai lay row dau queue va chuyen vao main.
3. Sau moi lan chuyen, queue phai don ve entry theo motion lien tuc, khong giat.
4. Hieu ung handoff tu queue sang main phai co cam giac "di vao", khong phai bien mat roi xuat hien.

### Non-functional

1. Giu logic du don gian cho du an hien tai: 1 queue, 1 entry, khong them framework orchestration moi neu chua can.
2. Tan dung `PathFollower`/`ConveyorPath` hien co, tranh copy cac feature Cocos chi phuc vu nhieu sub conveyors.
3. Han che regression o flow collect ball tai entry cua main conveyor.

## Danh gia logic hien tai dang sai o dau

### 1. Sai ve mo hinh UX

- `QueueConveyor.StartQueue()` dang de row dung yen hoan toan, va `AdvanceQueue()` la no-op, nen queue khong he "don ve entry" sau khi dequeue; dieu nay di nguoc yeu cau UX.
- `ConveyorFeeder.TryTransferRow()` dang tim `largest gap` tren toan vong main roi insert thang vao `gapCenter`; nguoi choi se thay row xuat hien o bat ky vi tri nao tren vong, khong phai di qua 1 entry point.
- `ConveyorController.InsertRowBall()` re-parent + reinitialize follower truc tiep len main path, nen handoff nhin nhu teleport/bat/tat, khong co bridge motion.

### 2. Sai ve causal logic

- Feeder hien quyet dinh transfer theo `largest gap anywhere`, trong khi yeu cau thuc te la `can entry feed now?`.
- Queue front dang duoc hieu bang `distance lon nhat tren queue path`; dieu nay chi dung neu entry nam o cuoi path, nhung code khong encode ro `entry slot` nhu mot invariant runtime.
- `ConveyorFeeder.Initialize()` goi `ringConveyor.SetHasQueueRows(true)` vo dieu kien; neu level queue rong hoac bi clear som thi state nay co the lech.

### 3. Sai ve feel/motion

- Khong co state `ready at entry` / `transferring` / `compacting`, nen transfer va compact xay ra nhu cac side effect roi rac.
- Khong co easing/tween rieng cho queue compact va bridge handoff, nen motion thieu anticipation va continuity.

## Kien truc de xuat

### Phuong an A - Minimal patch: entry-gated insert + compact tween toan queue

**Y tuong**

- Bo `FindLargestGap()` cho feeder queue.
- Dinh nghia 1 `entryDistanceOnMain` co dinh.
- Khi main du cho ngay sau entry, dequeue row dau queue.
- Recompute target distance cho tat ca row con lai tren queue path va tween tung row ve target moi.
- Insert row moi len main tai `entryDistanceOnMain`; co the them tween bridge ngan tu vi tri queue-end sang main-entry.

**Uu diem**

- It thay doi nhat, de ship.
- De doc, it state.
- Phu hop YAGNI/KISS neu muon sua nhanh UX chinh.

**Nhuoc diem**

- Tween toan queue moi lan dequeue co the hoi co hoc neu so row lon.
- Neu van insert truc tiep vao main path ma khong co bridge tween thi cam giac bat/tat van con mot phan.
- Logic spacing cua queue van la `teleport co easing`, chua phai conveyor feel that.

**Khi nen chon**

- Can cai thien nhanh, chap nhan motion `du on` thay vi `rat tu nhien`.

### Phuong an B - Hybrid recommended: queue co front-slot state + handoff tween + compact theo target distance

**Y tuong**

- Queue giu mot invariant ro: row dau luon tien toi `queueEntryDistance` va dung cho o do.
- Feeder chi hoi: `Entry co downstream clearance tren main khong?`
- Khi du cho:
  1. khoa transfer (`isTransferInProgress`),
  2. lay front row dang cho o entry,
  3. chay handoff tween ngan tu queue-end anchor sang main-entry anchor,
  4. hoan tat moi attach vao main path tai `entryDistanceOnMain`.
- Sau do queue compact: row ke tiep tween ve entry, cac row sau tween ve target distance moi hoac follow spacing don gian.

**Uu diem**

- Dung mental model `1 entry point`.
- Motion dep hon ro ret nhung van giu code kha gon.
- Muon dung tinh than Cocos: `waitingAtEntry` + `slideToDistance`, nhung khong be ca he sinh thai multi-conveyor.

**Nhuoc diem**

- Can them state nho cho queue/feeder.
- Phai dinh nghia ro entry anchors va clearance rule tren main.

**Khi nen chon**

- Day la diem can bang tot nhat giua UX, do an toan, va effort.

### Phuong an C - Full conveyor parity gan Cocos: follower spacing-driven sub conveyor

**Y tuong**

- Queue van hanh nhu sub conveyor that: reverse-direction, non-loop, spacing-aware, row tu chay ve entry roi stop bang `isWaitingAtEntry`.
- Sau moi pop, chi row dau tiep theo slide ve entry; cac row sau tu follow bang spacing logic trong `PathFollower`.
- Main va sub cung dung cung abstraction motion/follower.

**Uu diem**

- Feel tu nhien nhat, scale tot neu sau nay co nhieu sub conveyors.
- Gan reference Cocos nhat ve behavior.

**Nhuoc diem**

- De over-engineer cho bai toan hien tai.
- Rui ro regression cao hon vi dung sau vao `PathFollower` va collision/spacing semantics.
- Ton thoi gian tuning nhieu hon loi ich thuc te ngan han.

**Khi nen chon**

- Chi nen chon neu roadmap sap toi chac chan mo rong nhieu queue/sub-conveyor.

## Recommendation

- Chon **Phuong an B - Hybrid recommended**.
- Ly do:
  1. Sua dung van de cot loi: queue phai feed qua 1 entry duy nhat.
  2. Cai thien manh cam giac motion bang handoff tween + compact tween.
  3. Khong can copy cac phan phuc tap cua Cocos nhu cached rows, multiple fill points, balanced multi-bucket logic.
  4. Giu pham vi sua chu yeu trong 3 file conveyor hien tai, co the chi bo sung helper nho o `PathFollower` neu thuc su can.

## Pseudo-flow event/state

```text
State: QueueReady
  - frontRow dang o queue entry
  - feeder poll/observe downstream clearance tai main entry

Event: MainEntryHasSpace
  - if queue empty -> return
  - if transferInProgress -> return
  - transferInProgress = true
  - frontRow = queue.PopFrontReadyRow()
  - PlayHandoffTween(frontRow, queueEntryAnchor -> mainEntryAnchor)

Event: HandoffTweenCompleted(frontRow)
  - ring.InsertRowBallAtEntry(frontRow, entryDistanceOnMain)
  - queue.RebuildTargetDistances()
  - queue.CompactTowardEntry()
  - transferInProgress = false

State: QueueCompacting
  - row[0] tween -> queueEntryDistance
  - row[1..n] tween/follow -> spacing targets behind row truoc

Event: CompactCompleted
  - frontRow mark ReadyAtEntry
  - quay ve QueueReady
```

## Architecture

### Main rule changes

1. `QueueConveyor` tro thanh owner cua `queue slot layout` thay vi chi owner cua list row tinh.
2. `ConveyorFeeder` chuyen tu `global gap search` sang `entry clearance check`.
3. `ConveyorController` expose helper kieu `CanInsertAtEntry(entryDistance, requiredSpacing)` thay vi feeder phai suy ra tu `FindLargestGap()`.

### Entry clearance rule de xuat

- Toi thieu: kiem tra khoang trong tu `entryDistanceOnMain` toi row ke tiep tren main co lon hon `RowSpacing + buffer` khong.
- Khong can full largest-gap algorithm cho queue fill.
- Neu main dang trong hoan toan: cho phep insert ngay tai entry.

### Motion primitives nen muon tu Cocos

Chi muon 3 primitive:

1. `waitingAtEntry` - row dau queue co state dung cho o entry.
2. `slideToDistance(duration, easeOut)` - tween theo path distance de compact muot.
3. `spacing-aware following` o muc toi thieu - row sau khong chong len row truoc.

### Khong nen copy tu Cocos

- Khong mang `CachedRowManager`.
- Khong mang multi-sub-conveyor routing.
- Khong mang he plan bucket phuc tap cua main conveyor Cocos.
- Khong mang cache/sibling optimization som neu profiling chua chung minh can.

## Implementation Steps

1. Chuan hoa lai domain language trong code:
   - `queue entry`, `main entry`, `front row`, `ready ro
