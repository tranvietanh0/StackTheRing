namespace HyperCasualGame.Scripts.Scenes.Main
{
    using System.Linq;
    using GameFoundationCore.Scripts.DI.VContainer;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.Core;
    using HyperCasualGame.Scripts.Level;
    using HyperCasualGame.Scripts.Services;
    using HyperCasualGame.Scripts.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using UniT.Extensions;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public class MainSceneScope : SceneScope
    {
        [SerializeField] private GameManager gameManager;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register VContainerAdapter for scene-scoped IInitializable/ITickable types
            builder.Register<VContainerAdapter>(Lifetime.Scoped).AsImplementedInterfaces();

            this.RegisterSignals(builder);
            this.RegisterServices(builder);
            this.RegisterStateMachine(builder);
            this.RegisterComponents(builder);
        }

        private void RegisterSignals(IContainerBuilder builder)
        {
            // Legacy signals (can be removed after full migration)
            builder.DeclareSignal<CollectorTappedSignal>();
            builder.DeclareSignal<CollectorPlacedSignal>();
            builder.DeclareSignal<RingAttractedSignal>();
            builder.DeclareSignal<RingStackedSignal>();
            builder.DeclareSignal<StackClearedSignal>();
            builder.DeclareSignal<RingCompletedLoopSignal>();
            builder.DeclareSignal<AllRingsClearedSignal>();
            builder.DeclareSignal<LevelWinSignal>();
            builder.DeclareSignal<LevelLoseSignal>();
            builder.DeclareSignal<LevelStartSignal>();

            // Bucket/CollectArea signals
            builder.DeclareSignal<BucketTappedSignal>();
            builder.DeclareSignal<BucketJumpedToAreaSignal>();
            builder.DeclareSignal<BucketCompletedSignal>();
            builder.DeclareSignal<RowBallReachEntrySignal>();
            builder.DeclareSignal<BallCollectedSignal>();
            builder.DeclareSignal<RowBallCompletedLoopSignal>();
        }

        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<LevelManager>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<CollectAreaBucketService>(Lifetime.Scoped);
        }

        private void RegisterStateMachine(IContainerBuilder builder)
        {
            builder.Register<GameStateMachine>(Lifetime.Singleton)
                .WithParameter(container => typeof(IGameState).GetDerivedTypes()
                    .Select(type => (IGameState)container.Instantiate(type))
                    .ToList())
                .AsInterfacesAndSelf();
        }

        private void RegisterComponents(IContainerBuilder builder)
        {
            builder.RegisterComponent(this.gameManager).AsImplementedInterfaces();
            builder.RegisterBuildCallback(container =>
            {
                // Manually inject dependencies into GameManager
                this.gameManager.Inject(
                    container.Resolve<SignalBus>(),
                    container.Resolve<ILevelManager>(),
                    container.Resolve<GameStateMachine>(),
                    container.Resolve<UniT.Logging.ILoggerManager>(),
                    container.Resolve<CollectAreaBucketService>()
                );
            });
        }
    }
}