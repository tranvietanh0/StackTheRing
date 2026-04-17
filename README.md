# Stack The Ring

Stack The Ring la Unity project hyper-casual hien dang phat trien quanh gameplay conveyor + bucket + collect area + queue conveyor.

## Hien trang nhanh

- Unity project: `UnityStackTheRing/`
- Unity version thuc te: `2022.3.35f1`
- Scenes chinh:
  - `UnityStackTheRing/Assets/Scenes/0.LoadingScene.unity`
  - `UnityStackTheRing/Assets/Scenes/1.MainScene.unity`
- Tech stack chinh:
  - VContainer
  - UniTask
  - MessagePipe / SignalBus
  - Addressables
  - Dreamteck Splines
  - DOTween

## Runtime flow

1. `0.LoadingScene` khoi tao `LoadingScreenPresenter`
2. Presenter load user data, preload `Level_01`, sau do load `1.MainScene`
3. `MainSceneScope` dang ky services + state machine + signals
4. `LevelManager` load level prefab
5. `LevelController` initialize gameplay systems va chuyen sang `GamePlayState`

## Gameplay architecture

- `ConveyorController`: main loop conveyor, entry point detection, collect ball vao bucket
- `QueueConveyor`: queue row bo sung cho level co queue
- `ConveyorFeeder`: chen row tu queue vao ring khi co gap
- `BucketColumnManager`: spawn bucket grid, dua bucket hop le vao collect area
- `Bucket`: track target/incoming/collected, completion animation
- `CollectAreaManager`: quan ly collect area slots
- `CollectAreaBucketService`: target bucket theo color, slot availability, balanced assignment
- `GamePlayState`: start/stop runtime systems, check win/lose

## Thu muc quan trong

```text
StackTheRing/
|- UnityStackTheRing/
|  |- Assets/Scripts/
|  |- Assets/Scenes/
|  |- Assets/Submodules/
|  |- Packages/
|  `- ProjectSettings/
|- docs/
`- plans/
```

## Doc truoc khi sua code

`docs/` la source of truth:

- `docs/project-overview-pdr.md`
- `docs/codebase-summary.md`
- `docs/code-standards.md`
- `docs/system-architecture.md`

## Ghi chu quan trong

- Docs cu tung nhac Unity 6000 va `GameManager`; code hien tai khong con nhu vay
- Runtime coordinator hien tai la `LevelController` + `LevelManager` + `MainSceneScope`
- `GameWinState` va `GameLoseState` da ton tai nhung popup flow chua hoan tat
- `Setup.bat` / `Setup.sh` mang dau vet template bootstrap cu, khong nen xem la tai lieu kien truc hien tai
