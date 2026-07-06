// TowerShootingState.cs — no subscription management
using ClickMage.StateMachine;

public class TowerShootingState : IState<Tower>
{
    public void Enter(Tower owner) { }

    public void Tick(Tower owner, float deltaTime)
    {
        if (!owner.IsTargetInRange(owner.CurrentTarget))
        {
            owner.StateMachine.ChangeState(new TowerIdleState());
            return;
        }

        owner.RotateTowardsTarget(owner.CurrentTarget);

        if (owner.CanFire())
            owner.Fire(owner.CurrentTarget);
    }

    public void Exit(Tower owner) { }
}