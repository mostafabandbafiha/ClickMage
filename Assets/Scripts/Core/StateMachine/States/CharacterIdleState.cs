// CharacterIdleState.cs - UPDATED (simplified)
using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterIdleState : IState<BaseCharacter>
{
    public void Enter(BaseCharacter owner)
    {
        //owner.StopMoving();
        owner.Animator?.PlayAnimation(AnimationKeys.Clips.Idle);
    }

    public void Tick(BaseCharacter owner, float deltaTime)
    {
        // Behavior tree handles autonomous decisions now
        // This state just plays the idle animation
    }

    public void Exit(BaseCharacter owner) { }
}
