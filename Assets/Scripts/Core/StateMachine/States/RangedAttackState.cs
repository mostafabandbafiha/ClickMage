// ArcherAttackState.cs
using ClickMage.Animation;
using ClickMage.StateMachine;

public class RangedAttackState : IState<BaseCharacter>
{   

    
    public void Enter(BaseCharacter owner)
    {   

        owner.StopMoving();
        // Initial attack animation — subsequent ones triggered by command cooldown
        //owner.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);
    }

    public void OnAttack(BaseCharacter character)
    {
        if (character is EnemyCharacter archer)
        {
            archer.OnAttack();
        }
    }

    public void Tick(BaseCharacter owner, float deltaTime) { }

    public void Exit(BaseCharacter owner) { }
}