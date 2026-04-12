namespace HyperCasualGame.Scripts.Bucket
{
    using System;
    using HyperCasualGame.Scripts.Core;

    [Serializable]
    public struct BucketConfig
    {
        public int IndexBucket;
        public int Row;
        public int Column;
        public ColorType Color;
        public int TargetBallCount;
    }
}
