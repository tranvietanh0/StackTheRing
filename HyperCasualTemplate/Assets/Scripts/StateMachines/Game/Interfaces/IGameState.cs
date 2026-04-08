namespace HyperCasualGame.Scripts.StateMachines.Game.Interfaces
{
    using UITemplate.Scripts.Others.StateMachine.Interface;

    public interface IGameState : IState
    {
    }

    public interface IGameState<T> : IGameState
    {
        T Model { get; set; }
    }
}