namespace HyperCasualGame.Scripts.Core
{
    using UnityEngine;

    /// <summary>
    /// Game constants matching Cocos reference GAME_CONSTANTS
    /// </summary>
    public static class GameConstants
    {
        public const int MaxSlots = 4;
        public const int DefaultStackLimit = 8;
        public const float DefaultConveyorSpeed = 1f;

        public static class Tags
        {
            public const string Ring = "Ring";
            public const string Slot = "Slot";
            public const string Collector = "Collector";
        }

        public static class Layers
        {
            public const string Ring = "Ring";
            public const string UI = "UI";
        }

        /// <summary>Distance thresholds from Cocos GAME_CONSTANTS.DISTANCE_THRESHOLDS</summary>
        public static class DistanceThresholds
        {
            /// <summary>Minimum distance between balls to trigger collision/stop</summary>
            public const float BallCollision = 3.5f;

            /// <summary>Desired spacing between rowballs (world units)</summary>
            public const float BallSpacing = 0.06f;

            /// <summary>Distance threshold for fill point detection</summary>
            public const float FillPoint = 0.3f;

            /// <summary>Distance threshold for entry point trigger</summary>
            public const float EntryTrigger = 0.5f;

            /// <summary>Distance to reset fill point tracking</summary>
            public const float FillReset = 1.0f;

            /// <summary>Minimum distance for area clear check</summary>
            public const float AreaClear = 3.0f;
        }

        /// <summary>Conveyor speeds from Cocos GAME_CONSTANTS.CONVEYOR_SPEEDS</summary>
        public static class ConveyorSpeeds
        {
            public const float Main = 1f;
            public const float Secondary = 2f;
            public const float Snake = 2f;
        }

        /// <summary>Row ball config from Cocos GAME_CONSTANTS.ROW_BALL_CONFIG</summary>
        public static class RowBallConfig
        {
            /// <summary>Maximum balls per row</summary>
            public const int MaxBalls = 5;

            /// <summary>Ball positions along Z-axis (local space, perpendicular to path)</summary>
            public static readonly float[] ZPositions = { 0.16f, 0.08f, 0f, -0.08f, -0.16f };

            /// <summary>Delay between each ball jump when filling</summary>
            public const float BallJumpDelay = 0.05f;
        }

        /// <summary>Ball config from Cocos GameConfig.BALL</summary>
        public static class BallConfig
        {
            public const float JumpHeight = 1f;
            public const float JumpDuration = 0.2f;
        }

        /// <summary>Bucket config from Cocos GameConfig.BUCKET</summary>
        public static class BucketConfig
        {
            public const float DefaultJumpHeight = 0f;
            public const float DefaultJumpDuration = 0.2f;
            public const float DefaultJumpEndRotationX = 0f;
            public const float DefaultJumpEndRotationY = 0f;
            public const float DefaultJumpEndRotationZ = 0f;

            public const float CollectionMoveUpOffset = 3f;
            public const float CollectionMoveUpDuration = 0.5f;
            public const float CollectionRotationEnd = 360f;
            public const float CollectionRotationDuration = 0.5f;
            public const float CollectionScaleEnd = 0f;
            public const float CollectionScaleDuration = 0.5f;

            public const float ShakeScaleBump = 0.03f;
            public const float ShakeDuration = 0.1f;

            public const bool ParentToCollectArea = true;
            public const bool ResetPositionAfterLanding = true;
        }

        /// <summary>CollectArea config from Cocos GameConfig.COLLECT_AREA</summary>
        public static class CollectAreaConfig
        {
            public const float Spacing = 1.15f;
            public const int DefaultAreaCount = 4;
        }

        public static Color GetColor(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Red => new Color(0.9f, 0.2f, 0.2f),
                ColorType.Yellow => new Color(0.95f, 0.8f, 0.2f),
                ColorType.Green => new Color(0.2f, 0.8f, 0.3f),
                ColorType.Blue => new Color(0.2f, 0.5f, 0.9f),
                _ => Color.white
            };
        }
    }
}
