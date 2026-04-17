# Brainstorm - queue entry motion

## Problem

- Queue hien tai sai model UX: khong co `1 entry point` that su.
- Queue khong compact sau moi lan fill vi `AdvanceQueue()` dang rong.
- Feeder dang fill vao `largest gap` tren toan ring, khong phai fill qua entry.
- Handoff queue -> main dang giong teleport/re-parent, nen nhin giat, bat/tat.

## Requirements

- Queue chi co 1 entry.
- Main conveyor con cho o vung ngay sau entry thi queue moi fill vao.
- Sau moi lan fill, queue don dan ve phia entry.
- Motion phai muot, co cam giac sub conveyor feed vao main conveyor.
- Khong over-engineer; uu tien KISS/YAGNI.

## Brutal truth

- Logic hien tai khong phai "queue" theo nghia gameplay, no la `holding list + pop + insert anywhere`.
- Neu chi them tween nho ma giu `largest gap` thi UX van sai ban chat.
- Neu copy nguyen he thong Cocos thi qua tay cho scope hien tai; regression risk cao, tuning lau.

## Approaches

### A. Minimal patch

- Bo `largest gap`, thay bang `entry clearance check`.
- Sau moi dequeue, tween lai ca queue ve target distances moi.
- Insert row vao main tai `entryDistanceOnMain`.

**Pros**
- Ship nhanh.
- Diff nho.

**Cons**
- Motion van co the co hoc.
- Neu insert van la re-parent truc tiep thi van con cam giac bat/tat.

### B. Hybrid recommended

- Queue co state ro: `front row waiting at entry`.
- Feeder chi check `main entry downstream clear?`.
- Khi co cho:
  - lock transfer
  - lay front row
  - handoff tween tu `queueEntryAnchor` -> `mainEntryAnchor`
  - xong moi attach vao main path tai `entryDistanceOnMain`
- Sau do compact queue ve entry bang `slideToDistance`.

**Pros**
- Dung model UX user muon.
- Dep hon ro ret.
- Scope van gon, tap trung 3 file conveyor + 1 helper motion.

**Cons**
- Them state machine nho cho queue/feeder.
- Can dat them anchors/rule clearance ro rang.

### C. Gan Cocos nhat

- Bien queue thanh sub conveyor spacing-aware day du.
- Row sau follow row truoc, row dau stop tai entry, motion dong nhat voi Cocos.

**Pros**
- Feel dep nhat.
- Scale tot neu sau nay co nhieu sub conveyors.

**Cons**
- Qua tam scope.
- De loi/lech feel khi port sang Unity.
- Over-engineered voi 1 queue.

## Cocos refs nen muon

- `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA\assets\scripts\CocosBeadsOutRemasterPLA\Conveyor\SubConveyorController.ts:196`
  - chi slide row dau ve entry
  - set `isWaitingAtEntry`
- `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA\assets\scripts\CocosBeadsOutRemasterPLA\Conveyor\PathFollower.ts:544`
  - `slideToDistance()`
  - ease-out theo path distance

## Cocos refs khong nen copy

- `CachedRowManager`
- multi-sub-conveyor routing
- bucket fill orchestration cua game kia

## Final recommendation

- Chon **B. Hybrid recommended**.

### Why

- Giai quyet dung root cause, khong chi make-up animation.
- Giu code de maintain.
- Du dep de user thay "queue feed vao main".
- Khong can mang ca framework Cocos vao Unity.

## Proposed runtime model

### Queue states

- `Idle`
- `Compacting`
- `ReadyAtEntry`
- `Transferring`

### Main rules

1. Queue front luon co target distance co dinh = `queueEntryDistance`.
2. Feeder khong tim largest gap nua.
3. Feeder chi hoi: `entry tren main co du cho cho 1 row moi khong?`
4. Handoff visual phai la tween ngoai path main, khong insert truc tiep ngay frame dequeue.
5. Insert vao main chi xay ra sau khi handoff tween xong.

## Pseudo flow

```text
QueueReadyAtEntry
  -> if mainEntryHasSpace && !transferInProgress
  -> dequeue front row
  -> play handoff tween queueEntryAnchor -> mainEntryAnchor
  -> on complete: insert row vao main tai entryDistance
  -> compact queue rows con lai ve target distances moi
  -> mark front row moi = ReadyAtEntry
```

## Implementation considerations

- `QueueConveyor`
  - phai own `queue slot layout`
  - can method kieu `CompactTowardEntry()`
  - can method kieu `GetFrontReadyRow()` thay vi scan `max distance`
- `ConveyorFeeder`
  - thay `FindLargestGap()` bang `CanInsertAtEntry()`
  - can `isTransferInProgress`
- `ConveyorController`
  - expose helper `CanInsertAtEntry(entryDistance, clearanceDistance)`
  - `InsertRowBall()` nen co branch cho handoff-completed insert
- `PathFollower`
  - co the them helper `slideToDistance()` giong Cocos
  - khong nen bien no thanh avoidance system day du luc nay

## Risks

- Neu clearance rule qua naive, row moi co the overlap row dau tren main.
- Neu compact tween chay dong thoi voi transfer ma khong lock state, queue se rung/giat.
- Neu attach vao main truoc khi tween handoff xong, van quay lai feel teleport.
- Neu scene/prefab khong co `queueEntryAnchor` va `mainEntryAnchor`, tuning se rat cam tinh.

## Success metrics

- User nhin thay row di vao main qua 1 diem co dinh.
- Sau moi fill, queue tu don ve entry, khong snap frame-to-frame.
- Khong con insert vao vi tri bat ky tren ring.
- Khong overlap/hut giat o 10-20 lan transfer lien tiep.
- Code doc duoc, khong tao them abstraction du thua.

## Next steps

1. Chot phuong an B.
2. Review prefab/scene de dat `queueEntryAnchor` + `mainEntryAnchor`.
3. Tach task implement nho theo 4 muc: clearance rule, queue compact, handoff tween, validation/tuning.
