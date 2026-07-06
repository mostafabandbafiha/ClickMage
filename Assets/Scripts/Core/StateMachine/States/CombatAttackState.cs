// CombatAttackState.cs — generic attack state (was EnemyAttackState).
// BaseCharacter.GiveCommand checks for this type to interrupt mid-attack
// when the player issues a new command (relevant for heroes; harmless no-op for enemies).
using ClickMage.StateMachine;
public class CombatAttackState : IState<BaseCharacter>
{
    public void Enter(BaseCharacter owner)
    {
        owner.StopMoving();
    }
    public void OnAttack(BaseCharacter character)
    {
        if (character is CombatCharacter combatant)
        {
            combatant.OnAttack();
        }
    }
    public void Tick(BaseCharacter owner, float deltaTime) { }
    public void Exit(BaseCharacter owner) { }
}