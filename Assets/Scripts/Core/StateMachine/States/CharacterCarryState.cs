using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

/// <summary>
/// Active while the gatherer walks to the warehouse carrying an item.
/// Plays the carry-walk animation instead of the normal walk.
/// Movement itself is still driven by NavMeshAgent + MoveCommand.
/// </summary>
public class CharacterCarryState : IState<BaseCharacter>
{
    public void Enter(BaseCharacter character)
    {
        character.Animator?.PlayAnimation(AnimationKeys.Clips.CarryWalk);
        Debug.Log($"[CarryState] {character.name} walking to warehouse.");
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        if (character is GathererCharacter gatherer && !gatherer.IsCarrying)
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        var agent = character.Agent;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        var velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            character.transform.rotation = Quaternion.Slerp(
                character.transform.rotation,
                targetRot,
                deltaTime * character.RotationSpeed
            );
        }
        else if (!agent.pathPending && !agent.hasPath)
        {
            // Move finished — exit carry state so DepositCommand can run
            character.StateMachine.ChangeState(new CharacterIdleState());
        }
    }

    public void Exit(BaseCharacter character) { }
}