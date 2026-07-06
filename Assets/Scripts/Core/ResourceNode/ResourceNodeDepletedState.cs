// ResourceNodeDepletedState.cs
using ClickMage.StateMachine;

public class ResourceNodeDepletedState : IState<ResourceNode>
{
    public void Enter(ResourceNode owner)
    {
        owner.OnDepleted();
    }

    public void Tick(ResourceNode owner, float deltaTime)
    {
        owner.TickRegen(deltaTime);
    }

    public void Exit(ResourceNode owner)
    {
        // hook point – play regrowth sound, VFX, etc.
    }
}
