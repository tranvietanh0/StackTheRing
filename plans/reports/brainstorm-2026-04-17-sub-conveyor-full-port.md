# Brainstorm - sub conveyor full port

## Problem

- Unity sub conveyor hien tai da bi patch nhieu lan.
- Logic dang pha tron `feed cadence`, `visual compact`, `handoff`, `gap reservation`.
- User muon feel/phoi hop `y het` Cocos ref: `C:\Projects\TheOneProject\Cocos\BeadsOutRemasterPLA\CocosBeadsOutRemasterPLA`.

## Requirement

- Sub conveyor phai cho feel giong Cocos, khong chi "gan dung".
- Row vao queue, dung tai entry, main hut, queue don len dep.
- Cadence phai dong bo, khong lag, khong throttle ao.
- Uu tien chuan + dep hon patch nhanh.

## Brutal truth

- Co the lam. Nhung khong nen tiep tuc patch tren Unity flow hien tai.
- Neu user muon "y het" Cocos, cach dung la **rewrite sub conveyor flow theo Cocos mental model**, khong phai fix tung bug tren current hybrid.
- Tiep tuc va chạm tren current code se ton thoi gian hon, regressions cao hon, va van khong bao gio ra "feel y het".

## Current mismatch vs Cocos

- Cocos dung queue `_readyToFill`, row dau `waiting at entry`, main moi pop.
- Cocos khong co qua nhieu reservation/handoff heuristics.
- Cocos compact queue theo model don gian: row dau slide ve entry, row sau tu follow spacing/coast len.
- Unity hien tai dang mix:
  - reservation tren main
  - immediate insert
  - visual blend
  - queue logical snap
  - compact slide
- Ket qua: DX kho maintain, UX kho chot.

## Approaches

### A. Continue patching current Unity flow

**Pros**
- It file scope nho.
- Co the ship tung buoc.

**Cons**
- Sai huong neu muc tieu la "y het Cocos".
- Root cause van la model sai.
- Mỗi fix moi de mo them 1 throttle/race khac.

### B. Full sub-conveyor port by behavior (recommended)

- Giu Unity infra hien co (`RowBall`, `PathFollower`, `ConveyorController`, signals).
- Rewrite rieng `QueueConveyor` + `ConveyorFeeder` theo behavior Cocos.
- Port **mental model**, khong copy syntax 1:1.

**Pros**
- Dung muc tieu user.
- De reason hon.
- Long-term maintainable hon patch chain.

**Cons**
- Scope lon hon 1 fix nho.
- Can retest level 2 + moi level co queue.

### C. Literal code port 1:1 tu Cocos sang Unity

**Pros**
- Gan nhat voi ref ve surface logic.

**Cons**
- Toi khong recommend.
- Engine khac nhau: Cocos node/path/update vs Unity transform/spline/lifecycle.
- Copy 1:1 de sinh debt xau, adapter loang ngoang, bug subtle.

## Recommended solution

- Chon **B. Full sub-conveyor port by behavior**.

### Port these exact Cocos ideas

1. `readyToFill queue`
   - Row den entry -> stop moving
   - `isWaitingAtEntry = true`
   - push vao queue

2. `main pops sub`
   - Main khong reservation phuc tap
   - Main hoi sub: `co row san sang khong?`
   - Neu co -> pop front row

3. `compact after pop`
   - Slide row dau con lai ve entry
   - Row sau follow theo spacing / limited movement
   - Khong recompute blend/handoff heuristic lung tung

4. `cached/queued spawn rule`
   - Neu can, spawn them row vao cuoi sub conveyor sau khi front row bi pop, giong Cocos

## Concrete architecture change

### QueueConveyor should own

- `List<RowBall> readyToFill`
- `OnRowReachedEntry()`
- `PopFrontRow()`
- `SlideRemainingRowBalls()`
- optional `TrySpawnCachedRow()` if design can need more than static queue config

### ConveyorFeeder should become thin

- no gap reservation solver
- no moving-slot prediction
- no visual handoff policy
- chi:
  - check `sub.HasReadyRow()`
  - check `main.CanAcceptFromSub()`
  - `row = sub.PopFrontRow()`
  - insert row vao main theo merge rule don gian

### ConveyorController should expose only

- `CanAcceptFromSub()`
- `InsertRowFromSub(row)`
- maybe `GetSubMergeDistance()`

No more:
- full-gap reservation
- dynamic stale-gap recompute loop
- mixed visual/logical handoff layers

## Key design decision

### Should handoff be visual or logical?

If user wants "y het Cocos":

- Logic should be **entry queue -> main pop -> attach to main path**
- Visual should be minimal and subordinate to cadence
- Nghia la: neu visual lam tre cadence, visual must lose

No compromise here.

## Risks

- Rewrite nay se va cham toi level tuning.
- Co the can re-author queue merge point trong prefab.
- Can xac nhan Cocos ref dang la single-sub-conveyor flow, khong phai co extra hidden assumptions tu cached rows/spawner.

## Success criteria

- Main conveyor hut row tu sub voi cadence giong Cocos.
- Queue front dung tai entry, main pop front row dung model Cocos.
- Sau moi pop, queue don len dep, khong lag, khong throttle ao.
- Code queue/main feeder de doc va debug hon hien tai.

## Next steps

1. Stop patching current hybrid flow.
2. Tao implementation plan rieng: `sub-conveyor-full-port`.
3. Rewrite 3 thanh phan theo Cocos behavior:
   - `QueueConveyor`
   - `ConveyorFeeder`
   - phan `insert from sub` trong `ConveyorController`
4. Sau do retune prefab/anchors.
5. Cuoi cung compare runtime side-by-side voi Cocos.

## My recommendation

- Neu ban that su muon "y het" ref, toi khuyen **xoa bo logic sub conveyor vua patch, rewrite theo behavior Cocos ngay bay gio**.
- Day la scope hop ly, co chi phi, nhung la duong ngan nhat de ra ket qua dung.

## Unresolved questions

- Ban muon `literal same feel` hay `literal same code flow`? Toi recommend same feel + same behavior flow, not same code.
- Co can port ca `CachedRowManager/BallSpawner` model tu Cocos, hay queue level data hien tai la du?
