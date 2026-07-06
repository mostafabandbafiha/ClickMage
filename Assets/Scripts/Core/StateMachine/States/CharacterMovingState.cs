using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterMovingState : IState<BaseCharacter>
{
    public void Enter(BaseCharacter owner)
    {
        owner.Animator?.PlayAnimation(AnimationKeys.Clips.Walk);
    }
    public void Tick(BaseCharacter owner, float deltaTime)
    {
        var agent = owner.Agent;
        if (!agent.enabled || !agent.isOnNavMesh) return;
        // Rotation
        var velocity = agent.velocity;
        velocity.y = 0;
        if (velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            owner.transform.rotation = Quaternion.Slerp(
                owner.transform.rotation, targetRot,
                deltaTime * owner.RotationSpeed);
        }
        // Self-transition: if we've stopped, go idle
        /*if (agent.velocity.sqrMagnitude <= 0.01f && !agent.pathPending)
            owner.StateMachine.ChangeState(new CharacterIdleState());*/
    }
    public void Exit(BaseCharacter owner) { }
}