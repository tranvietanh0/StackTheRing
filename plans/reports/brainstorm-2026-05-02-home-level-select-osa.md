# Brainstorm - HomeScreen level select with OSA

## Problem

Them man chon level vao `HomeScreenView`.
Dung OSA da co san trong codebase.
Scope da chot:
- HomeScreen chi lam level grid
- OSA dang grid nhieu cot
- Level duoc mo tat ca

## Current context

- `HomeScreenView.cs` hien gan nhu trong, chua co UI logic
- `GameHomeState.cs` da mo `HomeScreenPresenter`
- Codebase da co wrapper OSA:
  - `BasicGridAdapter<TModel, TView, TPresenter>`
  - `BasicListAdapter<TModel, TView, TPresenter>`
- Runtime progression dang nam o `LevelManager` + `LocalDataController`
- Level order/load khong nen suy ra tu file name; can uu tien `LevelBlueprintReader` / progression source dang ton tai

## Evaluated approaches

| Approach | Summary | Pros | Cons |
|---|---|---|---|
| A. OSA grid embedded in `HomeScreenView` | Tao 1 level grid adapter + level item MVP, presenter bind list level | Hop style codebase, tan dung OSA wrapper, de mo rong sau | Can them 1 cum UI item + data model + adapter |
| B. ScrollRect/GridLayoutGroup thuong | Bo qua OSA, dung uGUI mac dinh | Nhanh cho it level | Trai yeu cau "dung OSA", khong tai su dung wrapper san co |
| C. Screen rieng cho Level Select | HomeScreen chi co nut vao man khac | Tach biet ro hon | Scope lon hon, them flow screen khong can thiet vi user muon HomeScreen la level select |

## Recommended solution

Chon **A**.

### UX

`HomeScreenView` tro thanh level select screen:
- Header don gian: title + maybe current selected level
- OSA vertical grid nhieu cot
- Moi item hien:
  - so level
  - state selected
  - state current level
  - state completed/uncompleted (neu data co)
- CTA chinh: `Play`
- Tap item => set selected level
- Tap `Play` => update current level roi vao gameplay flow

### Technical shape

#### 1. Keep HomeScreen as existing MVP entry
- Giu `GameHomeState -> OpenScreen<HomeScreenPresenter>()`
- Khong tao screen moi

#### 2. Add level-select data model layer
Can 1 model nho cho item:
- `LevelNumber`
- `IsSelected`
- `IsCurrentLevel`
- `IsUnlocked` (tam thoi always true theo scope da chot)
- optional `IsCompleted`

#### 3. Use existing OSA wrapper
- Prefer `BasicGridAdapter` vi UX da chot la grid nhieu cot
- Tao adapter concrete cho level items, khong dung thang generic wrapper trong presenter
- Presenter chi build list model + feed adapter

#### 4. Data source for level list
Nen dung thu tu level tu source runtime dang ton tai, uu tien:
1. `LevelBlueprintReader` / catalog level
2. fallback `HighestUnlockedLevel` / current local progression only for current selection state

Vi level load hien tai da di qua blueprint, level select cung nen doc cung nguon de tranh lech thu tu level so voi runtime load.

#### 5. Selection flow
- Presenter load danh sach level available
- Default selected item = current level
- Khi tap level item:
  - update selected state in models
  - refresh visible cells
- Khi tap `Play`:
  - save selected level as current level
  - dong HomeScreen / chuyen sang gameplay flow hien co

### File impact (expected)
- `UnityStackTheRing/Assets/Scripts/Scenes/Screen/HomeScreenView.cs`
- them level item view/presenter file (1 item UI)
- them concrete OSA grid adapter file for level select
- co the can read-only tiep can `LevelManager`, `LevelBlueprintReader`, `LocalDataController`
- prefab/UI screen HomeScreen can duoc update de chua viewport/content/item prefab reference

## Risks

1. **Level source mismatch**
   - neu build grid tu file count/resources thay vi blueprint reader se lech flow load level thuc te
2. **HomeScreen state vs gameplay bootstrap**
   - can xac dinh Play button se trigger state transition nao, tranh duplicate load level
3. **Generic OSA wrapper lifecycle**
   - presenter disposal/rebind phai theo pattern wrapper hien co
4. **All unlocked mode**
   - hien nhanh cho brainstorm, nhung neu doi sang lock/unlock sau nay can giu model field san

## Success criteria

- Vao `HomeScreenView` thay level grid scroll duoc
- Default selected = current level
- Tap item doi selected state dung
- Tap `Play` vao dung level da chon
- Khong can screen moi
- Khong hardcode list level bang tay

## Suggested implementation slices

1. Read exact level source API (`LevelBlueprintReader` / manager access)
2. Scaffold level item model + OSA grid adapter
3. Update `HomeScreenView` prefab/UI refs
4. Bind selection + play action in presenter
5. Verify mobile layout + scroll + state refresh

## Recommendation

Lam ban **MVP** truoc:
- grid item chi co number + selected highlight
- all levels unlocked
- 1 play button

Khong lam preview panel, stars, progress badge, paging, animation, hay separate screen o phase dau.
