namespace HyperCasualGame.Scripts.Level.Blueprint
{
    using System;
    using System.Linq;
    using BlueprintFlow.BlueprintReader;

    [BlueprintReader("LevelBlueprint")]
    public class LevelBlueprintReader : GenericBlueprintReaderByRow<int, LevelBlueprintRecord>
    {
        public bool ContainsLevel(int level)
        {
            return this.ContainsKey(level);
        }

        public int GetMinLevel()
        {
            return this.Count == 0 ? 1 : this.Keys.Min();
        }

        public int GetMaxLevel()
        {
            return this.Count == 0 ? 1 : this.Keys.Max();
        }

        public int NormalizeLevel(int requestedLevel)
        {
            if (this.Count == 0)
            {
                return 1;
            }

            if (requestedLevel < this.GetMinLevel())
            {
                return this.GetMinLevel();
            }

            if (requestedLevel > this.GetMaxLevel())
            {
                return this.GetMinLevel();
            }

            if (this.ContainsLevel(requestedLevel))
            {
                return requestedLevel;
            }

            var orderedLevels = this.Keys.OrderBy(level => level).ToArray();
            var nextAvailableLevel = orderedLevels.FirstOrDefault(level => level >= requestedLevel);
            return nextAvailableLevel == 0 ? this.GetMinLevel() : nextAvailableLevel;
        }

        public LevelBlueprintRecord GetRecord(int level)
        {
            return this.GetDataById(level);
        }

        public int GetNextLevel(int currentLevel)
        {
            if (this.Count == 0)
            {
                return 1;
            }

            var normalizedCurrentLevel = this.NormalizeLevel(currentLevel);
            var orderedLevels = this.Keys.OrderBy(level => level).ToArray();
            var currentIndex = Array.IndexOf(orderedLevels, normalizedCurrentLevel);
            if (currentIndex < 0 || currentIndex >= orderedLevels.Length - 1)
            {
                return orderedLevels[0];
            }

            return orderedLevels[currentIndex + 1];
        }
    }
}
