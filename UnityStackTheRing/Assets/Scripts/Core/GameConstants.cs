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

        /// <summary>Ring stack config for landing effect</summary>
        public static class RingStackConfig
        {
            /// <summary>Height per ring in stack (world units)</summary>
            public const float RingHeight = 0.06f;

            /// <summary>Starting Y position for first ring</summary>
            public const float BaseStackY = 0.08f;

            /// <summary>Max visible rings before fading oldest</summary>
            public const int MaxVisibleRings = 10;

            /// <summary>Scale when stacked (relative to original)</summary>
            public const float RingScaleOnStack = 0.7f;

            /// <summary>Duration for fade out animation of old rings</summary>
            public const float FadeOutDuration = 0.3f;
        }

        /// <summary>Wobble animation config for ring landing</summary>
        public static class WobbleConfig
        {
            /// <summary>Max initial tilt angle (degrees)</summary>
            public const float InitialTiltAngle = 12f;

            /// <summary>Each swing reduces amplitude by this factor</summary>
            public const float DampingFactor = 0.55f;

            /// <summary>Number of wobble oscillations</summary>
            public const int OscillationCount = 4;

            /// <summary>Total wobble duration (seconds)</summary>
            public const float WobbleDuration = 0.45f;

            /// <summary>Small Y bounce height on landing</summary>
            public const float BounceHeight = 0.03f;
        }

        /// <summary>Sparkle VFX config for ring landing</summary>
        public static class SparkleConfig
        {
            /// <summary>Number of particles per burst</summary>
            public const int ParticleCount = 20;

            /// <summary>Particle lifetime (seconds)</summary>
            public const float ParticleLifetime = 0.35f;

            /// <summary>Particle burst speed</summary>
            public const float BurstSpeed = 1.8f;

            /// <summary>Particle scale</summary>
            public const float SparkleScale = 0.015f;
        }

        /// <summary>Queue conveyor config</summary>
        public static class QueueConveyorConfig
        {
            /// <summary>Minimum gap (in path distance) on ring to accept a new row from queue</summary>
            public const float MinGapForTransfer = 0.15f;

            /// <summary>Distance threshold to consider a row "at the transfer point"</summary>
            public const float TransferPointThreshold = 0.3f;

            /// <summary>Delay between consecutive row transfers (seconds)</summary>
            public const float TransferCooldown = 0.2f;

            /// <summary>Default queue conveyor speed</summary>
            public const float DefaultQueueSpeed = 1f;

            /// <summary>How close the front row must be to queue entry before transfer</summary>
            public const float EntryReadyThreshold = 0.02f;

            /// <summary>Extra clearance required after main entry before accepting a queued row</summary>
            public const float EntryInsertBuffer = 0.001f;

            /// <summary>Duration for queue compact slide</summary>
            public const float CompactDuration = 0.25f;

            /// <summary>Duration for queue-to-main handoff tween</summary>
            public const float HandoffDuration = 0.22f;

            /// <summary>Vertical arc used while handing off a row to the main conveyor</summary>
            public const float HandoffArcHeight = 0.12f;
        }

        public static Color GetColor(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Red => new Color(0.9059f, 0.2980f, 0.2353f),
                ColorType.Yellow => new Color(0.9451f, 0.7686f, 0.0588f),
                ColorType.Green => new Color(0.1804f, 0.8000f, 0.4431f),
                ColorType.Blue => new Color(0.2039f, 0.5961f, 0.8588f),
                ColorType.Purple => new Color(0.6078f, 0.3490f, 0.7137f),
                ColorType.Orange => new Color(0.9529f, 0.6118f, 0.0706f),
                ColorType.Cyan => new Color(0.1020f, 0.7373f, 0.6118f),
                ColorType.DarkGray => new Color(0.3804f, 0.4157f, 0.4196f),
                ColorType.Pink => new Color(0.9569f, 0.4275f, 0.6980f),
                ColorType.Brown => new Color(0.5451f, 0.2706f, 0.0745f),
                _ => Color.white
            };
        }
    }
}
