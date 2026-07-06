// HarvestAnimationEventReceiver.cs
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private BaseCharacter _character;

    private void Awake()
    {
        _character = GetComponentInParent<BaseCharacter>();
    }

    // Called by Animation Event at the "hit" frame
    public void OnHarvestHit()
    {
        if (_character.StateMachine.CurrentState is CharacterHarvestingState harvestingState)
            harvestingState.DealHarvestHit(_character);
    }

    // Called by Animation Event at the last frame of the swing
    public void OnHarvestSwingComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterHarvestingState harvestingState)
            harvestingState.OnSwingComplete(_character);
    }

    public void OnSitDownComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterRestingState restingState)
            restingState.OnSitDownComplete(_character);
    }

    public void OnStandUpComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterRestingState restingState)
            restingState.OnStandUpComplete(_character);

        if (_character.StateMachine.CurrentState is CharacterDepositState depositState)
            depositState.OnStandUpComplete(_character);
    }

    public void OnReleaseHit()
    {
        if (_character.StateMachine.CurrentState is CharacterDepositState depositState)
            depositState.OnReleaseHit(_character);
    }

    public void OnPlaceComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterDepositState depositState)
            depositState.OnPlaceComplete(_character);
    }

    public void OnBendComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterPickupState pickupState)
            pickupState.OnBendComplete(_character);
    }

    public void OnGrabHit()
    {
        if (_character.StateMachine.CurrentState is CharacterPickupState pickupState)
            pickupState.OnGrabHit(_character);
    }

    public void OnGrabComplete()
    {
        if (_character.StateMachine.CurrentState is CharacterPickupState pickupState)
            pickupState.OnGrabComplete(_character);
    }

    public void OnAttack()
    {
        if (_character.StateMachine.CurrentState is RangedAttackState attackState)
        {
            attackState.OnAttack(_character);
            return;
        }

        if (_character.StateMachine.CurrentState is CombatAttackState meleeAttackState)
        {
            meleeAttackState.OnAttack(_character);
            return;
        }

        if (_character.StateMachine.CurrentState is HeroAttackState heroAttackState)
        {
            heroAttackState.OnAttack(_character);
            return;
        }

        return;
    }

    public void OnAttackComplete()
    {
        if (_character.StateMachine.CurrentState is HeroAttackState heroAttackState)
            heroAttackState.OnAttackComplete(_character);
    }

}
