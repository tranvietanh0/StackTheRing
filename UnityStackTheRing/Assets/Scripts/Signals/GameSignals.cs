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

    public class RingAttractedSignal
    {
        public Ring Ring;
        public int SlotIndex;
    }

    public class RingStackedSignal
    {
        public Ring Ring;
        public int SlotIndex;
        public int CurrentStackCount;
    }

    public class StackClearedSignal
    {
        public int SlotIndex;
        public ColorType Color;
        public int RingsCleared;
    }

    public class RingCompletedLoopSignal
    {
        public Ring Ring;
        public int LoopCount;
    }

    public class AllRingsClearedSignal
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
}
