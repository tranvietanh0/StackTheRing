namespace HyperCasualGame.Scripts.Core
{
    using UnityEngine;

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
