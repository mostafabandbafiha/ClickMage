// SummonState.cs — plays summon animation, event triggers actual summon
using ClickMage.Animation;
using ClickMage.StateMachine;

public class SummonState : IState<BaseCharacter>
{
    private float _animTimer = 0f;
    private bool _summoned = false;

    public void Enter(BaseCharacter owner)
    {
        owner.StopMoving();
        owner.Animator?.PlayAnimation(AnimationKeys.Clips.Summon);
        _animTimer = 0f;
        _summoned = false;
    }

    public void Tick(BaseCharacter owner, float deltaTime)
    {
        _animTimer += deltaTime;

        // Trigger summon at midpoint of animation (before clip ends)
        float clipLength = owner.Animator?.GetClipLength(AnimationKeys.Clips.Summon) ?? 1f;

        if (!_summoned && _animTimer >= clipLength * 0.5f)
        {
            _summoned = true;
            if (owner is AbyssalHorror horror)
                horror.Summon();
        }

        // Return to idle when animation finishes
        if (_animTimer >= clipLength)
        {
            // Mark command complete so queue moves on → SeekAgainCommand → BT ticks
            owner.StateMachine.ChangeState(new CharacterIdleState());
        }
    }

    public void Exit(BaseCharacter owner) { }
}