namespace HyperCasualGame.Scripts.Services
{
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.Utilities.UserData;
    using HyperCasualGame.Scripts.Models;

    public class LocalDataController
    {
        private readonly IHandleUserDataServices userDataServices;
        private UserLocalData userLocalData;

        public LocalDataController(IHandleUserDataServices userDataServices)
        {
            this.userDataServices = userDataServices;
        }

        public async UniTask<UserLocalData> GetUserLocalData()
        {
            this.userLocalData ??= await this.userDataServices.Load<UserLocalData>();
            return this.userLocalData;
        }

        public async UniTask<int> GetCurrentLevel()
        {
            return (await this.GetUserLocalData()).CurrentLevel;
        }

        public async UniTask<int> GetHighestUnlockedLevel()
        {
            return (await this.GetUserLocalData()).HighestUnlockedLevel;
        }

        public async UniTask SetCurrentLevel(int currentLevel, bool saveImmediately = true)
        {
            var data = await this.GetUserLocalData();
            data.CurrentLevel = currentLevel;
            await this.userDataServices.Save(data, saveImmediately);
        }

        public async UniTask SetHighestUnlockedLevel(int highestUnlockedLevel, bool saveImmediately = true)
        {
            var data = await this.GetUserLocalData();
            data.HighestUnlockedLevel = highestUnlockedLevel;
            await this.userDataServices.Save(data, saveImmediately);
        }

        public async UniTask Save()
        {
            var data = await this.GetUserLocalData();
            await this.userDataServices.Save(data, true);
        }
    }
}
