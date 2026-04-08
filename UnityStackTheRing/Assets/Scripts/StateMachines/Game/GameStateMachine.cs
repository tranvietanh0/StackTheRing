namespace HyperCasualGame.Scripts.StateMachines.Game
{
    using System.Collections.Generic;
    using System.Linq;
    using GameFoundationCore.Scripts.DI;
    using GameFoundationCore.Scripts.Signals;
    using HyperCasualGame.Scripts.StateMachines.Game.Interfaces;
    using HyperCasualGame.Scripts.StateMachines.Game.States;
    using UITemplate.Scripts.Others.StateMachine.Controller;
    using UITemplate.Scripts.Others.StateMachine.Interface;
    using UniT.Logging;

    public class GameStateMachine : StateMachine, IInitializable
    {
        public GameStateMachine(
            List<IGameState> listState,
            ILoggerManager   loggerManager,
            SignalBus        signalBus
        ) : base(listState.Select(x => x as IState).ToList(), loggerManager, signalBus)
        {
            foreach (var gameState in listState)
            {
                if (gameState is IHaveStateMachine haveStateMachine) haveStateMachine.StateMachine = this;
            }
        }

        public void Initialize()
        {
            this.TransitionTo<GameHomeState>();
        }
    }
}