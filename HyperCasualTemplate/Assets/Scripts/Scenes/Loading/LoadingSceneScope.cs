namespace HyperCasualGame.Scripts.Scenes.Loading
{
    using GameFoundationCore.Scripts.DI.VContainer;
    using GameFoundationCore.Scripts.UIModule.Utilities;
    using HyperCasualGame.Scripts.Scenes.Screen;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public class LoadingSceneScope : SceneScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.InitScreenManually<LoadingScreenPresenter>(autoBindData: true);
        }
    }
}