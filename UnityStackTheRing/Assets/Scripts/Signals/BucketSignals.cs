namespace HyperCasualGame.Scripts.Signals
{
    using HyperCasualGame.Scripts.Core;

    /// <summary>
    /// Fired when player taps an eligible bucket in the grid
    /// </summary>
    public class BucketTappedSignal
    {
        public int BucketIndex;
        public ColorType Color;
    }

    /// <summary>
    /// Fired when a bucket completes jump animation to CollectArea
    /// </summary>
    public class BucketJumpedToAreaSignal
    {
        public int BucketIndex;
        public int AreaIndex;
        public ColorType Color;
    }

    /// <summary>
    /// Fired when a bucket is fully filled and completes its collection animation
    /// </summary>
    public class BucketCompletedSignal
    {
        public ColorType Color;
        public int BucketIndex;
    }

    /// <summary>
    /// Fired when a RowBall reaches an entry point on the conveyor
    /// </summary>
    public class RowBallReachEntrySignal
    {
        public int RowId;
        public int EntryIndex;
        public string ConveyorId;
    }
}
