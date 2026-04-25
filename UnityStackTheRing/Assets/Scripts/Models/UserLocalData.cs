namespace HyperCasualGame.Scripts.Models
{
    using GameFoundationCore.Scripts.Models.Interfaces;

    public class UserLocalData : ILocalData
    {
        public int CurrentLevel { get; set; }
        public int HighestUnlockedLevel { get; set; }

        public void Init()
        {
            this.CurrentLevel = 1;
            this.HighestUnlockedLevel = 1;
        }

        public void OnDataLoaded()
        {
            if (this.CurrentLevel <= 0)
            {
                this.CurrentLevel = 1;
            }

            if (this.HighestUnlockedLevel <= 0)
            {
                this.HighestUnlockedLevel = 1;
            }
        }
    }
}