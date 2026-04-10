namespace HyperCasualGame.Scripts.Ring
{
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Signals;
    using UnityEngine;

    /// <summary>
    /// Container for a row of balls. Matches Cocos RowBall class.
    /// </summary>
    public class RowBall : MonoBehaviour
    {
        [SerializeField] private Ball ballPrefab;
        [SerializeField] private Transform spawnRoot;

        private static int globalRowIdCounter;

        private int rowId;
        private readonly Ball[] slots = new Ball[GameConstants.RowBallConfig.MaxBalls];
        private SignalBus signalBus;

        public int RowId => this.rowId;

        public void Initialize(RowBallConfig config, Ball prefab, SignalBus signalBus)
        {
            this.rowId = globalRowIdCounter++;
            this.ballPrefab = prefab;
            this.signalBus = signalBus;

            // Subscribe to ball collected signal
            this.signalBus.Subscribe<BallCollectedSignal>(this.OnBallCollected);

            this.ClearAllSlots();

            // Spawn balls for each color in config
            for (var i = 0; i < config.BallColors.Length && i < GameConstants.RowBallConfig.MaxBalls; i++)
            {
                this.SpawnBallInSlot(i, config.BallColors[i]);
            }
        }

        private void OnDestroy()
        {
            this.signalBus?.Unsubscribe<BallCollectedSignal>(this.OnBallCollected);
            this.ClearAllSlots();
        }

        private void OnBallCollected(BallCollectedSignal signal)
        {
            if (signal.RowId == this.rowId)
            {
                this.slots[signal.BallIndex] = null;
            }
        }

        public void SpawnBallInSlot(int index, ColorType color)
        {
            if (this.ballPrefab == null || index >= GameConstants.RowBallConfig.MaxBalls)
            {
                return;
            }

            var parent = this.spawnRoot != null ? this.spawnRoot : this.transform;
            var ballGO = Instantiate(this.ballPrefab, parent);
            var ball = ballGO.GetComponent<Ball>();

            if (ball != null)
            {
                var zPos = GameConstants.RowBallConfig.ZPositions[index];
                ball.Initialize(this.rowId, index, color, new Vector3(0, 0, zPos), this.signalBus);
                this.slots[index] = ball;
            }
        }

        public int GetBallCount()
        {
            return this.slots.Count(s => s != null);
        }

        public bool IsEmpty()
        {
            return this.GetBallCount() == 0;
        }

        public List<Ball> GetActiveBalls()
        {
            return this.slots.Where(s => s != null).ToList();
        }

        public Ball GetBallAt(int index)
        {
            if (index < 0 || index >= GameConstants.RowBallConfig.MaxBalls)
            {
                return null;
            }

            return this.slots[index];
        }

        public int GetEmptySlotCount()
        {
            return this.slots.Count(s => s == null);
        }

        public List<int> GetEmptySlotIndices()
        {
            var indices = new List<int>();
            for (var i = 0; i < this.slots.Length; i++)
            {
                if (this.slots[i] == null)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public bool AddBallToSlot(Ball ball, int slotIndex)
        {
            if (this.GetBallCount() >= GameConstants.RowBallConfig.MaxBalls)
            {
                return false;
            }

            if (slotIndex < 0 || slotIndex >= GameConstants.RowBallConfig.MaxBalls)
            {
                return false;
            }

            if (this.slots[slotIndex] != null)
            {
                return false;
            }

            var parent = this.spawnRoot != null ? this.spawnRoot : this.transform;
            ball.transform.SetParent(parent, true);

            var zPos = GameConstants.RowBallConfig.ZPositions[slotIndex];
            ball.transform.localPosition = new Vector3(0, 0, zPos);

            ball.RowId = this.rowId;
            ball.BallIndex = slotIndex;

            this.slots[slotIndex] = ball;

            return true;
        }

        public Ball RemoveBallAt(int index)
        {
            if (index < 0 || index >= GameConstants.RowBallConfig.MaxBalls)
            {
                return null;
            }

            var ball = this.slots[index];
            if (ball == null)
            {
                return null;
            }

            this.slots[index] = null;
            return ball;
        }

        private void ClearAllSlots()
        {
            foreach (var ball in this.slots)
            {
                if (ball != null && ball.gameObject != null)
                {
                    Destroy(ball.gameObject);
                }
            }

            for (var i = 0; i < this.slots.Length; i++)
            {
                this.slots[i] = null;
            }
        }

        public void RefreshLayout()
        {
            for (var i = 0; i < this.slots.Length; i++)
            {
                var ball = this.slots[i];
                if (ball == null || ball.gameObject == null)
                {
                    continue;
                }

                var zPos = GameConstants.RowBallConfig.ZPositions[i];
                ball.transform.localPosition = new Vector3(0, 0, zPos);
            }
        }
    }

    public struct RowBallConfig
    {
        public Vector3 SpawnPosition;
        public ColorType[] BallColors;
        public int RowId;
    }
}
