namespace HyperCasualGame.Scripts.Signals
{
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Ring;

    public class CollectorTappedSignal
    {
        public ColorType Color;
    }

    public class CollectorPlacedSignal
    {
        public int SlotIndex;
        public ColorType Color;
    }

    public class BallCollectedSignal
    {
        public int RowId;
        public int BallIndex;
        public ColorType Color;
    }

    public class BallAttractedSignal
    {
        public Ball Ball;
        public int SlotIndex;
    }

    public class BallStackedSignal
    {
        public Ball Ball;
        public int SlotIndex;
        public int CurrentStackCount;
    }

    public class StackClearedSignal
    {
        public int SlotIndex;
        public ColorType Color;
        public int BallsCleared;
    }

    public class RowBallCompletedLoopSignal
    {
        public RowBall RowBall;
        public int LoopCount;
    }

    public class AllRingsClearedSignal
    {
    }

    /// <summary>
    /// Fired when a row is transferred from queue conveyor to the main ring.
    /// </summary>
    public class QueueRowTransferredSignal
    {
        public int RowId;
        public int RemainingQueueRows;
    }

    /// <summary>
    /// Fired when all queue rows have been transferred to the ring.
    /// </summary>
    public class QueueEmptySignal
    {
    }

    public class LevelWinSignal
    {
        public int LevelNumber;
        public int Score;
    }

    public class LevelLoseSignal
    {
        public int LevelNumber;
    }

    public class LevelStartSignal
    {
        public int LevelNumber;
    }

    // Legacy signals for backward compatibility
    public class RingAttractedSignal
    {
        public Ball Ring;
        public int SlotIndex;
    }

    public class RingStackedSignal
    {
        public Ball Ring;
        public int SlotIndex;
        public int CurrentStackCount;
    }

    public class RingCompletedLoopSignal
    {
        public RowBall Ring;
        public int LoopCount;
    }
}
