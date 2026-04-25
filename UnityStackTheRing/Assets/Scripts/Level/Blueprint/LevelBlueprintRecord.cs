namespace HyperCasualGame.Scripts.Level.Blueprint
{
    using BlueprintFlow.BlueprintReader;

    [CsvHeaderKey(nameof(Level))]
    public class LevelBlueprintRecord
    {
        public int Level;
        public string LevelName;
    }
}
