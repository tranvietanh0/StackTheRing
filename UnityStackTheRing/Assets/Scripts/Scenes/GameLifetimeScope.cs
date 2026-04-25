namespace HyperCasualGame.Scripts.Scenes
{
    using GameFoundationCore.Scripts;
    using GameFoundationCore.Scripts.DI.VContainer;
    using GameFoundationCore.Scripts.Models;
    using HyperCasualGame.Scripts.Services;
    using UITemplate.Scripts;
    using UniT.Extensions;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterResource<GDKConfig>("Configs/GDKConfig", Lifetime.Singleton);
            builder.RegisterGameFoundation(this.transform);
            builder.RegisterUITemplate();
            builder.Register<LocalDataController>(Lifetime.Singleton);
        }
    }
}