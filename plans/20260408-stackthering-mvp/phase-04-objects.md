# Phase 4: Game Objects

**Effort:** M (3 days)
**Dependencies:** Phase 1 (Data), Phase 2 (Signals), Phase 3 (Services)
**Blocks:** Phase 5, 6

## Objective

Create MonoBehaviour components for game entities: Ball, RowBall, Bucket, CollectArea.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Objects/Ball.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Objects/RowBall.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Objects/Bucket.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Objects/CollectArea.cs` | CREATE | this phase |

## Implementation

### 1. Ball.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Objects
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Services;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;

    public class Ball : MonoBehaviour
    {
        [SerializeField] private Renderer ballRenderer;

        public int RowId { get; private set; } = -1;
        public int BallIndex { get; private set; } = -1;
        public ColorType BallColor { get; private set; }

        private bool isCollected;
        private IColorService colorService;
        private IJumpService jumpService;
        private SignalBus signalBus;
        private StackTheRingConfig config;

        public void Initialize(
            int rowId,
            int ballIndex,
            ColorType color,
            Vector3 position,
            IColorService colorService,
            IJumpService jumpService,
            SignalBus signalBus,
            StackTheRingConfig config)
        {
            RowId = rowId;
            BallIndex = ballIndex;
            BallColor = color;
            isCollected = false;

            this.colorService = colorService;
            this.jumpService = jumpService;
            this.signalBus = signalBus;
            this.config = config;

            transform.localPosition = position;
            UpdateColor(color);
        }

        public void UpdateRowReference(int newRowId, int newBallIndex)
        {
            RowId = newRowId;
            BallIndex = newBallIndex;
        }

        public async UniTask JumpToBucket(Bucket targetBucket, bool alreadyReserved = false)
        {
            if (isCollected || targetBucket == null || targetBucket.IsBucketCompleted)
                return;

            if (!alreadyReserved)
            {
                var availableSlots = targetBucket.GetRemainingSlotCount();
                if (availableSlots <= 0) return;
                targetBucket.StartIncomingBall();
            }

            isCollected = true;

            // Fire signal before jump
            signalBus.Fire(new BallCollectedSignal(RowId, BallIndex, BallColor));

            // Perform jump animation
            await jumpService.JumpToTarget(
                transform,
                targetBucket.transform,
                config.BallJumpHeight,
                config.BallJumpDuration
            );

            // Add to bucket after landing
            if (targetBucket != null && !targetBucket.IsBucketCompleted)
            {
                targetBucket.AddBall(this);
            }

            targetBucket?.CompleteIncomingBall();
        }

        private void UpdateColor(ColorType color)
        {
            BallColor = color;
            if (colorService != null && ballRenderer != null)
            {
                colorService.ApplyColor(ballRenderer, color);
            }
        }
    }
}
```

### 2. RowBall.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Objects
{
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Services;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;

    public class RowBall : MonoBehaviour
    {
        [SerializeField] private Transform spawnRoot;
        [SerializeField] private GameObject ballPrefab;

        private const int MaxBalls = 5;
        private static readonly float[] ZPositions = { 0.3f, 0.15f, 0f, -0.15f, -0.3f };

        private int rowId;
        private Ball[] slots = new Ball[MaxBalls];

        private IColorService colorService;
        private IJumpService jumpService;
        private SignalBus signalBus;
        private StackTheRingConfig config;

        private static int globalRowIdCounter;

        public void Initialize(
            RowConfig rowConfig,
            IColorService colorService,
            IJumpService jumpService,
            SignalBus signalBus,
            StackTheRingConfig config)
        {
            this.colorService = colorService;
            this.jumpService = jumpService;
            this.signalBus = signalBus;
            this.config = config;

            rowId = globalRowIdCounter++;
            ClearAllSlots();

            for (int i = 0; i < rowConfig.BallColors.Length && i < MaxBalls; i++)
            {
                SpawnBallInSlot(i, rowConfig.BallColors[i]);
            }

            signalBus.Subscribe<BallCollectedSignal>(OnBallCollected);
        }

        private void OnDestroy()
        {
            signalBus?.Unsubscribe<BallCollectedSignal>(OnBallCollected);
            ClearAllSlots();
        }

        private void OnBallCollected(BallCollectedSignal signal)
        {
            if (signal.RowId == rowId && signal.BallIndex < slots.Length)
            {
                slots[signal.BallIndex] = null;
            }
        }

        private void SpawnBallInSlot(int index, ColorType color)
        {
            if (ballPrefab == null || index >= MaxBalls) return;

            var parent = spawnRoot != null ? spawnRoot : transform;
            var ballGO = Instantiate(ballPrefab, parent);
            var ball = ballGO.GetComponent<Ball>();

            if (ball != null)
            {
                var position = new Vector3(0, 0, ZPositions[index]);
                ball.Initialize(rowId, index, color, position, colorService, jumpService, signalBus, config);
                slots[index] = ball;
            }
        }

        private void ClearAllSlots()
        {
            foreach (var ball in slots.Where(b => b != null))
            {
                if (ball.gameObject != null)
                    Destroy(ball.gameObject);
            }
            slots = new Ball[MaxBalls];
        }

        public int GetRowId() => rowId;
        public int GetBallCount() => slots.Count(s => s != null);
        public int GetEmptySlotCount() => slots.Count(s => s == null);
        public List<Ball> GetActiveBalls() => slots.Where(s => s != null).ToList();
        public Ball GetBallAt(int index) => index >= 0 && index < MaxBalls ? slots[index] : null;

        public List<int> GetEmptySlotIndices()
        {
            var indices = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) indices.Add(i);
            }
            return indices;
        }

        public bool AddBallToSlot(Ball ball, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxBalls || slots[slotIndex] != null)
                return false;

            var parent = spawnRoot != null ? spawnRoot : transform;
            ball.transform.SetParent(parent);
            ball.transform.localPosition = new Vector3(0, 0, ZPositions[slotIndex]);
            ball.UpdateRowReference(rowId, slotIndex);
            slots[slotIndex] = ball;
            return true;
        }

        public Ball RemoveBallAt(int index)
        {
            if (index < 0 || index >= MaxBalls) return null;
            var ball = slots[index];
            slots[index] = null;
            return ball;
        }
    }
}
```

### 3. Bucket.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Objects
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Services;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using TMPro;
    using UnityEngine;

    public class Bucket : MonoBehaviour
    {
        [SerializeField] private Renderer[] meshRenderers;
        [SerializeField] private TextMeshPro progressLabel;
        [SerializeField] private GameObject coverNode;
        [SerializeField] private Collider bucketCollider;

        public BucketConfig Config { get; private set; }
        public bool IsInCollectArea { get; private set; }
        public bool IsBucketCompleted { get; private set; }

        private List<Ball> collectedBalls = new();
        private int incomingBalls;
        private IColorService colorService;
        private IJumpService jumpService;
        private SignalBus signalBus;
        private StackTheRingConfig gameConfig;

        public void Initialize(
            BucketConfig config,
            IColorService colorService,
            IJumpService jumpService,
            SignalBus signalBus,
            StackTheRingConfig gameConfig)
        {
            Config = config;
            this.colorService = colorService;
            this.jumpService = jumpService;
            this.signalBus = signalBus;
            this.gameConfig = gameConfig;

            IsInCollectArea = false;
            IsBucketCompleted = false;
            collectedBalls.Clear();
            incomingBalls = 0;

            UpdateColor();
            UpdateProgressUI();
            UpdateVisualState();
        }

        private void UpdateColor()
        {
            if (colorService == null) return;
            foreach (var renderer in meshRenderers)
            {
                colorService.ApplyColor(renderer, Config.Color);
            }
        }

        private void UpdateProgressUI()
        {
            if (progressLabel == null) return;
            var percent = Mathf.Min(100, Mathf.FloorToInt((float)collectedBalls.Count / Config.TargetBallCount * 100));
            progressLabel.text = $"{percent}%";
        }

        private void UpdateVisualState()
        {
            if (progressLabel != null)
                progressLabel.gameObject.SetActive(IsInCollectArea);
            if (coverNode != null)
                coverNode.SetActive(false);
        }

        public async UniTask JumpToCollectArea(Transform targetArea)
        {
            IsInCollectArea = true;

            await jumpService.JumpToTarget(
                transform,
                targetArea,
                gameConfig.BucketJumpHeight,
                gameConfig.BucketJumpDuration
            );

            transform.SetParent(targetArea);
            transform.localPosition = Vector3.zero;
            UpdateVisualState();
        }

        public int GetTargetBallCount() => Config?.TargetBallCount ?? gameConfig.DefaultTargetBallCount;
        public int GetCollectedBallCount() => collectedBalls.Count;
        public int GetIncomingBallCount() => incomingBalls;
        public int GetRemainingSlotCount(int additionalIncoming = 0)
            => Mathf.Max(0, GetTargetBallCount() - collectedBalls.Count - incomingBalls - additionalIncoming);

        public void StartIncomingBall() => incomingBalls++;
        public void CompleteIncomingBall()
        {
            incomingBalls = Mathf.Max(0, incomingBalls - 1);
            CheckComplete();
        }

        public void AddBall(Ball ball)
        {
            if (IsBucketCompleted) return;

            collectedBalls.Add(ball);
            if (ball != null)
            {
                ball.transform.SetParent(transform);
                ball.transform.localPosition = Vector3.zero;
            }

            UpdateProgressUI();
            TriggerShake();
            CheckComplete();
        }

        private void CheckComplete()
        {
            if (IsBucketCompleted) return;
            if (collectedBalls.Count >= GetTargetBallCount() && incomingBalls == 0)
            {
                OnBucketComplete().Forget();
            }
        }

        private async UniTaskVoid OnBucketComplete()
        {
            IsBucketCompleted = true;

            // Hide progress
            if (progressLabel != null)
                progressLabel.gameObject.SetActive(false);

            // Destroy collected balls
            foreach (var ball in collectedBalls)
            {
                if (ball != null && ball.gameObject != null)
                    Destroy(ball.gameObject);
            }
            collectedBalls.Clear();

            // Show cover and animate
            if (coverNode != null)
                coverNode.SetActive(true);

            // Move up
            await transform.DOMoveY(transform.position.y + 3f, 0.5f).AsyncWaitForCompletion();

            // Rotate
            await transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.LocalAxisAdd)
                .AsyncWaitForCompletion();

            // Scale down
            await transform.DOScale(Vector3.zero, 0.5f).AsyncWaitForCompletion();

            // Fire completion signal
            signalBus.Fire(new BucketCompletedSignal(Config.Color));

            // Destroy bucket
            Destroy(gameObject);
        }

        private void TriggerShake()
        {
            transform.DOShakeScale(0.1f, 0.1f, 10, 90, true).SetEase(Ease.OutQuad);
        }
    }
}
```

### 4. CollectArea.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Objects
{
    using UnityEngine;

    public class CollectArea : MonoBehaviour
    {
        [SerializeField] private Transform bucketSlot;

        public bool IsOccupied { get; private set; }
        public Bucket CurrentBucket { get; private set; }

        public Transform GetSlotTransform() => bucketSlot != null ? bucketSlot : transform;

        public bool TryOccupy(Bucket bucket)
        {
            if (IsOccupied) return false;

            IsOccupied = true;
            CurrentBucket = bucket;
            return true;
        }

        public void Release()
        {
            IsOccupied = false;
            CurrentBucket = null;
        }
    }
}
```

## Verification

- [ ] All 4 files compile without errors
- [ ] Ball.JumpToBucket uses UniTask (async)
- [ ] RowBall subscribes/unsubscribes to BallCollectedSignal correctly
- [ ] Bucket completion fires BucketCompletedSignal
- [ ] No `[Inject]` attributes — dependencies passed via Initialize()

## Notes

- Objects receive dependencies via Initialize() method (not constructor injection)
- This pattern is used because MonoBehaviours can't have constructors with DI
- Factory pattern will be used in Core Logic phase to create these objects
