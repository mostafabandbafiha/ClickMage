// TowerAcquiringTargetState.cs — no subscription management
using UnityEngine;
using ClickMage.StateMachine;

public class TowerAcquiringTargetState : IState<Tower>
{
    private const float AimThreshold = 5f;

    public void Enter(Tower owner) { }

    public void Tick(Tower owner, float deltaTime)
    {
        if (!owner.IsTargetInRange(owner.CurrentTarget))
        {
            owner.StateMachine.ChangeState(new TowerIdleState());
            return;
        }

        owner.RotateTowardsTarget(owner.CurrentTarget);

        if (IsAimedAtTarget(owner))
            owner.StateMachine.ChangeState(new TowerShootingState());
    }

    public void Exit(Tower owner) { }

    private bool IsAimedAtTarget(Tower owner)
    {
        if (owner.CurrentTarget == null) return false;
        Vector3 dir = (owner.CurrentTarget.Position - owner.transform.position).normalized;
        float angle = Vector3.Angle(owner.FirePoint.forward, dir);
        return angle < AimThreshold;
    }
}