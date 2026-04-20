namespace HyperCasualGame.Scripts.Scenes.Main
{
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundationCore.Scripts.DI.VContainer;
    using GameFoundationCore.Scripts.Signals;
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
        [SerializeField] private Transform levelRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            // Register VContainerAdapter for scene-scoped IInitializable/ITickable types
            builder.Register<VContainerAdapter>(Lifetime.Scoped).AsImplementedInterfaces();

            this.RegisterSignals(builder);
            this.RegisterServices(builder);
            this.RegisterStateMachine(builder);
            this.SetupLevelLoading(builder);
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
            // Register LevelManager with levelRoot parameter
            builder.Register<LevelManager>(Lifetime.Singleton)
                .WithParameter("levelRoot", this.levelRoot)
                .AsImplementedInterfaces();

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

        private void SetupLevelLoading(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                var levelManager = container.Resolve<ILevelManager>() as LevelManager;
                if (levelManager == null) return;

                // Set up inject callback for LevelController
                levelManager.SetInjectCallback(controller =>
                {
                    controller.Inject(
                        container.Resolve<SignalBus>(),
                        container.Resolve<ILevelManager>(),
                        container.Resolve<GameStateMachine>(),
                        container.Resolve<UniT.Logging.ILoggerManager>(),
                        container.Resolve<CollectAreaBucketService>()
                    );
                });

                // Load first level
                this.LoadFirstLevel(levelManager).Forget();
            });
        }

        private async UniTask LoadFirstLevel(ILevelManager levelManager)
        {
            // Small delay to ensure all services are ready
            await UniTask.Yield();

            var controller = await levelManager.LoadLevel(16);
            if (controller == null)
            {
                Debug.LogError("[MainSceneScope] Failed to load first level!");
            }
        }
    }
}