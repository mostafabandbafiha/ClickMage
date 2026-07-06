// AttackCommand.cs — generic attack-loop command (was EnemyAttackCommand).
// Works for goblins, heroes, anything that's a CombatCharacter.
using ClickMage.Animation;
using ClickMage.Stats;
using UnityEngine;
public class AttackCommand : ICommand<BaseCharacter>
{
    private readonly Targetable _target;
    private readonly CombatCharacter _attacker;
    private readonly GameObject _attackerGO;
    private float _attackTimer = 0f;
    private float _animTimer = 0f;
    private bool _isAttackAnimPlaying = false;
    private Collider _targetCollider;
    public bool IsComplete { get; private set; }
    public AttackCommand(Targetable target, CombatCharacter attacker)
    {
        _target = target;
        _attacker = attacker;
        _attackerGO = attacker.gameObject;
    }
    public void Start(BaseCharacter character)
    {
        if (_target == null || !_target.IsAlive)
        {
            _target?.Disengage(_attackerGO);
            IsComplete = true;
            return;
        }
        _targetCollider = _target.GetComponent<Collider>();
        _attacker.CurrentTarget = _target;
        character.StopMoving();
        character.StateMachine.ChangeState(new CombatAttackState());
        // Attack immediately on first contact
        PlayAttackAnimation(character);
        _attackTimer = 0f;
    }
    public void Tick(BaseCharacter character, float deltaTime)
    {
        if (character == null) return;

        // Check validity BEFORE touching transform/collider
        if (_target == null || !_target.IsAlive)
        {
            _attacker.CurrentTarget = null;
            if (_target != null) _target.Disengage(_attackerGO); // avoid Unity fake-null with ?.
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        float dist = GetDistanceToSurface(character.transform.position);
        if (dist > _attacker.GetStatValue(CommonStats.AttackRange))
        {
            _attacker.CurrentTarget = null;
            _target.Disengage(_attackerGO);
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        if (_attackTimer >= _attacker.GetStatValue(CommonStats.AttackCooldown))
        {
            _attackTimer = 0f;
            PlayAttackAnimation(character);
        }
    }
    private void PlayAttackAnimation(BaseCharacter character)
    {
        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);
        _isAttackAnimPlaying = true;
        _animTimer = 0f;
    }
    public void Cancel()
    {
        _attacker.CurrentTarget = null;
        _target?.Disengage(_attackerGO);
        IsComplete = true;
    }
    private float GetDistanceToSurface(Vector3 from)
    {
        if (_targetCollider == null)
            return Vector3.Distance(from, _target.transform.position);
        Vector3 closestPoint = _targetCollider.ClosestPoint(from);
        return Vector3.Distance(from, closestPoint);
    }
}