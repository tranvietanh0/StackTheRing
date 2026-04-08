# Phase 6: Core Game Logic

**Effort:** M (3 days)
**Dependencies:** Phase 1-5
**Blocks:** Phase 7, 8

## Objective

Create main game controller, level loader, and input handler.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Core/StackTheRingController.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Core/LevelLoader.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Core/InputHandler.cs` | CREATE | this phase |

## Implementation

### 1. StackTheRingController.cs

Main game orchestrator — manages game state, tracks bucket completion.

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Conveyor;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;

    public class StackTheRingController : IInitializable
    {
        private readonly SignalBus signalBus;
        private readonly StackTheRingConfig config;
        private readonly LevelLoader levelLoader;

        private int totalBuckets;
        private int completedBuckets;
        private int currentLevelIndex;
        private bool isLevelComplete;

        private List<CollectArea> collectAreas = new();
        private List<Bucket> activeBuckets = new();

        public StackTheRingController(
            SignalBus signalBus,
            StackTheRingConfig config,
            LevelLoader levelLoader)
        {
            this.signalBus = signalBus;
            this.config = config;
            this.levelLoader = levelLoader;
        }

        public void Initialize()
        {
            signalBus.Subscribe<BucketCompletedSignal>(OnBucketCompleted);
            signalBus.Subscribe<RowBallReachEntrySignal>(OnRowBallReachEntry);
        }

        public void Dispose()
        {
            signalBus.Unsubscribe<BucketCompletedSignal>(OnBucketCompleted);
            signalBus.Unsubscribe<RowBallReachEntrySignal>(OnRowBallReachEntry);
        }

        public void StartLevel(int levelIndex)
        {
            currentLevelIndex = levelIndex;
            completedBuckets = 0;
            isLevelComplete = false;

            levelLoader.LoadLevel(levelIndex, out totalBuckets, out collectAreas, out activeBuckets);
        }

        public void ResetLevel()
        {
            levelLoader.CleanupLevel();
            completedBuckets = 0;
            isLevelComplete = false;
            activeBuckets.Clear();
        }

        private void OnBucketCompleted(BucketCompletedSignal signal)
        {
            if (isLevelComplete) return;

            completedBuckets++;
            Debug.Log($"[StackTheRing] Bucket completed: {signal.BucketColor}. Progress: {completedBuckets}/{totalBuckets}");

            activeBuckets.RemoveAll(b => b == null || b.IsBucketCompleted);

            if (completedBuckets >= totalBuckets)
            {
                TriggerLevelComplete();
            }
        }

        private void TriggerLevelComplete()
        {
            if (isLevelComplete) return;

            isLevelComplete = true;
            Debug.Log($"[StackTheRing] Level {currentLevelIndex} Complete!");
            signalBus.Fire(new LevelCompleteSignal(currentLevelIndex));
        }

        private void OnRowBallReachEntry(RowBallReachEntrySignal signal)
        {
            // Find matching buckets for balls in the RowBall
            var rowBall = signal.RowBallTransform.GetComponent<RowBall>();
            if (rowBall == null) return;

            var activeBalls = rowBall.GetActiveBalls();
            foreach (var ball in activeBalls)
            {
                var targetBucket = GetAvailableBucketByColor(ball.BallColor);
                if (targetBucket != null)
                {
                    ball.JumpToBucket(targetBucket).Forget();
                }
            }
        }

        public Bucket GetAvailableBucketByColor(ColorType color)
        {
            return activeBuckets.FirstOrDefault(b =>
                b != null &&
                !b.IsBucketCompleted &&
                b.IsInCollectArea &&
                b.Config.Color == color &&
                b.GetRemainingSlotCount() > 0);
        }

        public List<ColorType> GetTargetColorsFromBuckets()
        {
            return activeBuckets
                .Where(b => b != null && !b.IsBucketCompleted && b.IsInCollectArea)
                .Select(b => b.Config.Color)
                .Distinct()
                .ToList();
        }

        public int GetCompletedBuckets() => completedBuckets;
        public int GetTotalBuckets() => totalBuckets;
        public bool IsLevelComplete() => isLevelComplete;
    }
}
```

### 2. LevelLoader.cs

Loads level data and spawns game objects.

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Core
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Conveyor;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using HyperCasualGame.Scripts.StackTheRing.Services;
    using UnityEngine;

    public class LevelLoader
    {
        private readonly SignalBus signalBus;
        private readonly StackTheRingConfig config;
        private readonly IColorService colorService;
        private readonly IJumpService jumpService;

        // Prefab references (set via scene or addressables)
        private GameObject ballPrefab;
        private GameObject rowBallPrefab;
        private GameObject bucketPrefab;

        // Scene references
        private ConveyorController mainConveyor;
        private Transform collectAreaContainer;
        private List<CollectArea> collectAreas = new();

        public LevelLoader(
            SignalBus signalBus,
            StackTheRingConfig config,
            IColorService colorService,
            IJumpService jumpService)
        {
            this.signalBus = signalBus;
            this.config = config;
            this.colorService = colorService;
            this.jumpService = jumpService;
        }

        public void SetupReferences(
            GameObject ballPrefab,
            GameObject rowBallPrefab,
            GameObject bucketPrefab,
            ConveyorController mainConveyor,
            Transform collectAreaContainer,
            List<CollectArea> collectAreas)
        {
            this.ballPrefab = ballPrefab;
            this.rowBallPrefab = rowBallPrefab;
            this.bucketPrefab = bucketPrefab;
            this.mainConveyor = mainConveyor;
            this.collectAreaContainer = collectAreaContainer;
            this.collectAreas = collectAreas;
        }

        public void LoadLevel(
            int levelIndex,
            out int totalBuckets,
            out List<CollectArea> areas,
            out List<Bucket> buckets)
        {
            // TODO: Load from Blueprint CSV based on levelIndex
            // For MVP, use hardcoded test data
            var levelData = GetTestLevelData(levelIndex);

            // Spawn buckets
            buckets = SpawnBuckets(levelData.Buckets);
            totalBuckets = buckets.Count;

            // Move first bucket of each color to collect area
            MoveBucketsToCollectAreas(buckets);

            // Setup conveyor
            mainConveyor.Setup(signalBus, config);
            mainConveyor.Initialize(0, config.ConveyorSpeed, null);

            // Spawn initial rows on conveyor
            SpawnInitialRows(levelData.InitialRows);

            areas = collectAreas;
        }

        private LevelData GetTestLevelData(int levelIndex)
        {
            // Hardcoded test level for MVP
            return new LevelData
            {
                LevelId = levelIndex,
                Buckets = new[]
                {
                    new BucketConfig(0, 0, 0, ColorType.Red, 10),
                    new BucketConfig(1, 0, 1, ColorType.Blue, 10),
                    new BucketConfig(2, 0, 2, ColorType.Green, 10),
                },
                InitialRows = new[]
                {
                    new RowConfig(0, new[] { ColorType.Red, ColorType.Blue, ColorType.Green, ColorType.Red, ColorType.Blue }),
                    new RowConfig(1, new[] { ColorType.Green, ColorType.Red, ColorType.Blue, ColorType.Green, ColorType.Red }),
                    new RowConfig(2, new[] { ColorType.Blue, ColorType.Green, ColorType.Red, ColorType.Blue, ColorType.Green }),
                }
            };
        }

        private List<Bucket> SpawnBuckets(BucketConfig[] configs)
        {
            var buckets = new List<Bucket>();

            foreach (var config in configs)
            {
                if (bucketPrefab == null) continue;

                var bucketGO = Object.Instantiate(bucketPrefab);
                var bucket = bucketGO.GetComponent<Bucket>();

                if (bucket != null)
                {
                    bucket.Initialize(config, colorService, jumpService, signalBus, this.config);
                    buckets.Add(bucket);
                }
            }

            return buckets;
        }

        private void MoveBucketsToCollectAreas(List<Bucket> buckets)
        {
            var areaIndex = 0;
            foreach (var bucket in buckets)
            {
                if (areaIndex >= collectAreas.Count) break;

                var area = collectAreas[areaIndex];
                if (area.TryOccupy(bucket))
                {
                    bucket.JumpToCollectArea(area.GetSlotTransform()).Forget();
                    areaIndex++;
                }
            }
        }

        private void SpawnInitialRows(RowConfig[] configs)
        {
            if (mainConveyor == null || rowBallPrefab == null) return;

            var spawnPoint = mainConveyor.GetSpawnPoint();
            var offset = 0f;

            foreach (var rowConfig in configs)
            {
                var rowBallGO = Object.Instantiate(rowBallPrefab, spawnPoint);
                rowBallGO.transform.localPosition = new Vector3(0, 0, offset);
                offset += 1f; // Spacing between rows

                var rowBall = rowBallGO.GetComponent<RowBall>();
                if (rowBall != null)
                {
                    rowBall.Initialize(rowConfig, colorService, jumpService, signalBus, config);
                    mainConveyor.AddRowBall(rowBall);
                }
            }
        }

        public void CleanupLevel()
        {
            mainConveyor?.ClearAllRowBalls();

            foreach (var area in collectAreas)
            {
                area.Release();
            }
        }
    }
}
```

### 3. InputHandler.cs

Handles touch/click input for bucket interaction.

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Core
{
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using UnityEngine;

    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask bucketLayer;

        private bool canTouch = true;
        private float touchCooldown = 0.1f;

        private void Update()
        {
            if (!canTouch) return;

            // Handle touch input
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    HandleInput(touch.position);
                }
            }
            // Handle mouse input (for editor testing)
            else if (Input.GetMouseButtonDown(0))
            {
                HandleInput(Input.mousePosition);
            }
        }

        private void HandleInput(Vector2 screenPosition)
        {
            canTouch = false;
            Invoke(nameof(ResetTouch), touchCooldown);

            var ray = mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit, 100f, bucketLayer))
            {
                var bucket = hit.collider.GetComponentInParent<Bucket>();
                if (bucket != null)
                {
                    OnBucketClicked(bucket);
                }
            }
        }

        private void OnBucketClicked(Bucket bucket)
        {
            if (bucket.IsInCollectArea || bucket.IsBucketCompleted)
            {
                Debug.Log($"[InputHandler] Bucket already in collect area or completed");
                return;
            }

            // Find available collect area
            var collectAreas = FindObjectsOfType<CollectArea>();
            foreach (var area in collectAreas)
            {
                if (!area.IsOccupied && area.TryOccupy(bucket))
                {
                    bucket.JumpToCollectArea(area.GetSlotTransform()).Forget();
                    break;
                }
            }
        }

        private void ResetTouch()
        {
            canTouch = true;
        }

        public void SetCamera(Camera camera)
        {
            mainCamera = camera;
        }
    }
}
```

## Verification

- [ ] All 3 files compile without errors
- [ ] StackTheRingController tracks bucket completion correctly
- [ ] LevelLoader spawns buckets and initial rows
- [ ] InputHandler detects bucket clicks via raycast
- [ ] LevelCompleteSignal fires when all buckets complete

## Notes

- LevelLoader uses hardcoded test data for MVP
- Blueprint CSV loading to be implemented in future phase
- InputHandler uses LayerMask to filter raycast hits
- StackTheRingController implements IInitializable for VContainer lifecycle
