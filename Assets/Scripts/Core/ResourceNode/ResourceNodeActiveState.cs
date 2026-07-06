// ResourceNodeActiveState.cs
using ClickMage.StateMachine;

public class ResourceNodeActiveState : IState<ResourceNode>
{
    public void Enter(ResourceNode owner)
    {
        // hook point – play rustle sound, VFX, etc.
    }

    public void Tick(ResourceNode owner, float deltaTime)
    {
        owner.TickRegen(deltaTime);
    }

    public void Exit(ResourceNode owner)
    {
        // nothing needed
    }
}
