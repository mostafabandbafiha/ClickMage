// TowerDestructedState.cs
using ClickMage.StateMachine;

public class TowerDestructedState : IState<Tower>
{
    public void Enter(Tower owner)
    {
        owner.ClearTarget();
    }

    public void Tick(Tower owner, float deltaTime) { }

    public void Exit(Tower owner) { }
}