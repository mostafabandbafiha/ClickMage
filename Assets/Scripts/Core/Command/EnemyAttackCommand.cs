// EnemyAttackCommand.cs
using ClickMage.Animation;
using ClickMage.Interface;
using ClickMage.Stats;
using UnityEngine;

public class EnemyAttackCommand : ICommand<BaseCharacter>
{
    private readonly Targetable _target;
    private readonly EnemyCharacter _enemy;
    private readonly GameObject _attackerGO;
    private float _attackTimer = 0f;
    private float _animTimer = 0f;
    private bool _isAttackAnimPlaying = false;
    private Collider _targetCollider;

    public bool IsComplete { get; private set; }

    public EnemyAttackCommand(Targetable target, EnemyCharacter enemy)
    {
        _target = target;
        _enemy = enemy;
        _attackerGO = enemy.gameObject;
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
        _enemy.SetCombatTarget(_target);

        character.StopMoving();
        character.StateMachine.ChangeState(new CombatAttackState());

        // Attack immediately on first contact
        PlayAttackAnimation(character);
        _attackTimer = 0f;
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {   
        if (character == null)
        {
            return;
        }
        float dist = GetDistanceToSurface(character.transform.position);
        if (_target == null || !_target.IsAlive || dist > _enemy.GetStatValue(CommonStats.AttackRange))
        {
            _enemy.DisengageCurrentTarget();
            _target?.Disengage(_attackerGO);
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        _attackTimer += deltaTime;

        if (_isAttackAnimPlaying)
        {
            _animTimer += deltaTime;
            float clipLength = character.Animator?.GetClipLength(AnimationKeys.Clips.Harvest) ?? 0.5f;

            if (_animTimer >= clipLength)
            {
                // Attack animation finished — go back to idle while waiting on cooldown
                _isAttackAnimPlaying = false;
                character.Animator?.PlayAnimation(AnimationKeys.Clips.Idle);
            }
        }

        // Cooldown reached — fire next attack
        if (_attackTimer >= _enemy.GetStatValue(CommonStats.AttackCooldown))
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
        _enemy.DisengageCurrentTarget();
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