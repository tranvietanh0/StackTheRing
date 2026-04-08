# Phase 5: Conveyor System

**Effort:** M (3 days)
**Dependencies:** Phase 3 (Services), Phase 4 (Objects)
**Blocks:** Phase 6

## Objective

Create conveyor belt system that moves RowBalls along a path.

## File Ownership

| File | Action | Owner |
|------|--------|-------|
| `Assets/Scripts/StackTheRing/Conveyor/IConveyor.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Conveyor/ConveyorController.cs` | CREATE | this phase |
| `Assets/Scripts/StackTheRing/Conveyor/PathFollower.cs` | CREATE | this phase |

## Implementation

### 1. IConveyor.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Conveyor
{
    using System.Collections.Generic;
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using UnityEngine;

    public interface IConveyor
    {
        int ConveyorId { get; }
        void Initialize(int id, float speed, Vector3[] pathPoints);
        void AddRowBall(RowBall rowBall);
        void RemoveRowBall(RowBall rowBall);
        void ClearAllRowBalls();
        List<RowBall> GetRowBalls();
        void SetSpeed(float speed);
        void Pause();
        void Resume();
    }
}
```

### 2. ConveyorController.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Conveyor
{
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Data;
    using HyperCasualGame.Scripts.StackTheRing.Objects;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;

    public class ConveyorController : MonoBehaviour, IConveyor
    {
        [SerializeField] private Transform[] pathNodes;
        [SerializeField] private Transform[] entryNodes;
        [SerializeField] private Transform spawnPoint;

        public int ConveyorId { get; private set; }

        private float speed;
        private bool isPaused;
        private List<RowBall> rowBalls = new();
        private Vector3[] pathPoints;
        private SignalBus signalBus;
        private StackTheRingConfig config;

        public void Setup(SignalBus signalBus, StackTheRingConfig config)
        {
            this.signalBus = signalBus;
            this.config = config;
        }

        public void Initialize(int id, float speed, Vector3[] pathPoints)
        {
            ConveyorId = id;
            this.speed = speed;
            this.pathPoints = pathPoints ?? GetPathPointsFromNodes();
            isPaused = false;
            rowBalls.Clear();
        }

        private Vector3[] GetPathPointsFromNodes()
        {
            if (pathNodes == null || pathNodes.Length == 0)
                return new Vector3[0];

            return pathNodes.Select(n => n.position).ToArray();
        }

        public void AddRowBall(RowBall rowBall)
        {
            if (rowBall == null || rowBalls.Contains(rowBall)) return;

            rowBalls.Add(rowBall);

            // Setup path follower
            var follower = rowBall.GetComponent<PathFollower>();
            if (follower == null)
                follower = rowBall.gameObject.AddComponent<PathFollower>();

            follower.Initialize(pathPoints, speed, ConveyorId, signalBus, entryNodes);
        }

        public void RemoveRowBall(RowBall rowBall)
        {
            rowBalls.Remove(rowBall);
        }

        public void ClearAllRowBalls()
        {
            foreach (var rowBall in rowBalls.ToList())
            {
                if (rowBall != null && rowBall.gameObject != null)
                    Destroy(rowBall.gameObject);
            }
            rowBalls.Clear();
        }

        public List<RowBall> GetRowBalls() => new(rowBalls);

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
            foreach (var rowBall in rowBalls)
            {
                var follower = rowBall?.GetComponent<PathFollower>();
                follower?.SetSpeed(newSpeed);
            }
        }

        public void Pause()
        {
            isPaused = true;
            foreach (var rowBall in rowBalls)
            {
                var follower = rowBall?.GetComponent<PathFollower>();
                follower?.Pause();
            }
        }

        public void Resume()
        {
            isPaused = false;
            foreach (var rowBall in rowBalls)
            {
                var follower = rowBall?.GetComponent<PathFollower>();
                follower?.Resume();
            }
        }

        public Transform GetSpawnPoint() => spawnPoint;
        public Transform[] GetEntryNodes() => entryNodes;

        private void OnDrawGizmos()
        {
            // Draw path in editor
            if (pathNodes == null || pathNodes.Length < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathNodes.Length - 1; i++)
            {
                if (pathNodes[i] != null && pathNodes[i + 1] != null)
                    Gizmos.DrawLine(pathNodes[i].position, pathNodes[i + 1].position);
            }

            // Draw entry nodes
            Gizmos.color = Color.green;
            if (entryNodes != null)
            {
                foreach (var entry in entryNodes)
                {
                    if (entry != null)
                        Gizmos.DrawWireSphere(entry.position, 0.3f);
                }
            }
        }
    }
}
```

### 3. PathFollower.cs

```csharp
namespace HyperCasualGame.Scripts.StackTheRing.Conveyor
{
    using DG.Tweening;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StackTheRing.Signals;
    using UnityEngine;

    public class PathFollower : MonoBehaviour
    {
        private Vector3[] pathPoints;
        private float speed;
        private int conveyorId;
        private SignalBus signalBus;
        private Transform[] entryNodes;

        private int currentPointIndex;
        private bool isPaused;
        private bool isInitialized;
        private Tween moveTween;

        // Entry tracking
        private bool[] triggeredEntries;
        private float entryTriggerDistance = 0.3f;

        public void Initialize(
            Vector3[] pathPoints,
            float speed,
            int conveyorId,
            SignalBus signalBus,
            Transform[] entryNodes)
        {
            this.pathPoints = pathPoints;
            this.speed = speed;
            this.conveyorId = conveyorId;
            this.signalBus = signalBus;
            this.entryNodes = entryNodes;

            currentPointIndex = 0;
            isPaused = false;
            isInitialized = true;
            triggeredEntries = new bool[entryNodes?.Length ?? 0];

            if (pathPoints != null && pathPoints.Length > 0)
            {
                transform.position = pathPoints[0];
                MoveToNextPoint();
            }
        }

        private void Update()
        {
            if (!isInitialized || isPaused) return;

            CheckEntryNodes();
        }

        private void CheckEntryNodes()
        {
            if (entryNodes == null) return;

            for (int i = 0; i < entryNodes.Length; i++)
            {
                if (triggeredEntries[i] || entryNodes[i] == null) continue;

                var distance = Vector3.Distance(transform.position, entryNodes[i].position);
                if (distance < entryTriggerDistance)
                {
                    triggeredEntries[i] = true;
                    signalBus.Fire(new RowBallReachEntrySignal(transform, conveyorId, i));
                }
            }
        }

        private void MoveToNextPoint()
        {
            if (pathPoints == null || currentPointIndex >= pathPoints.Length)
            {
                // Loop back to start
                currentPointIndex = 0;
                ResetEntryTriggers();
            }

            if (pathPoints.Length == 0) return;

            var targetPoint = pathPoints[currentPointIndex];
            var distance = Vector3.Distance(transform.position, targetPoint);
            var duration = distance / speed;

            moveTween?.Kill();
            moveTween = transform.DOMove(targetPoint, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    currentPointIndex++;
                    MoveToNextPoint();
                });
        }

        private void ResetEntryTriggers()
        {
            for (int i = 0; i < triggeredEntries.Length; i++)
            {
                triggeredEntries[i] = false;
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
            // Restart movement with new speed
            if (isInitialized && !isPaused)
            {
                MoveToNextPoint();
            }
        }

        public void Pause()
        {
            isPaused = true;
            moveTween?.Pause();
        }

        public void Resume()
        {
            isPaused = false;
            moveTween?.Play();
        }

        private void OnDestroy()
        {
            moveTween?.Kill();
        }
    }
}
```

## Verification

- [ ] All 3 files compile without errors
- [ ] ConveyorController draws path gizmos in editor
- [ ] PathFollower loops when reaching end of path
- [ ] RowBallReachEntrySignal fires when approaching entry nodes
- [ ] Pause/Resume works correctly

## Notes

- MVP uses simple linear path (DOMove point-to-point)
- Can upgrade to DOTween Path or Unity Splines for curved paths later
- Entry nodes trigger ball collection logic (handled in Phase 6)
- PathFollower resets entry triggers when looping to prevent double-fire
