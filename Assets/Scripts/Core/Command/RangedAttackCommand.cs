// ArcherAttackCommand.cs
using ClickMage.Animation;
using ClickMage.Interface;
using ClickMage.Stats;
using UnityEngine;

public class RangedAttackCommand : ICommand<BaseCharacter>
{
    private readonly Targetable _target;
    private readonly EnemyCharacter _enemy;
    private readonly GameObject _attackerGO;
    private Collider _targetCollider;
    private float _attackTimer = 0f;
    private float _animTimer = 0f;
    private bool _isAttackAnimPlaying = false;

    public bool IsComplete { get; private set; }

    public RangedAttackCommand(Targetable target, EnemyCharacter enemy)
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

        if (!_target.TryEngage(_attackerGO))
        {
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        _enemy.SetCombatTarget(_target);
        _targetCollider = _target.GetComponent<Collider>();
        _attackTimer = 0f;
        character.StopMoving();
        character.StateMachine.ChangeState(new RangedAttackState());
        PlayAttackAnimation(character);
    }


    public void Tick(BaseCharacter character, float deltaTime)
    {
        if (_target == null || !_target.IsAlive)
        {
            _enemy.DisengageCurrentTarget();
            _target?.Disengage(_attackerGO);
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        float dist = GetDistanceToSurface(character.transform.position);
        if (dist > _enemy.GetStatValue(CommonStats.AttackRange) * 1.2f)
        {
            _enemy.DisengageCurrentTarget();
            _target.Disengage(_attackerGO);
            IsComplete = true;
            character.StateMachine.ChangeState(new CharacterIdleState());
            character.QueueCommand(new SeekAgainCommand());
            return;
        }

        FaceTarget(character);

        _attackTimer += deltaTime;

        if (_isAttackAnimPlaying)
        {
            _animTimer += deltaTime;
            float clipLength = character.Animator?.GetClipLength(AnimationKeys.Clips.Harvest) ?? 0.5f;

            if (_animTimer >= clipLength)
            {
                _isAttackAnimPlaying = false;
                character.Animator?.PlayAnimation(AnimationKeys.Clips.Idle);
            }
        }

        if (_attackTimer >= _enemy.GetStatValue(CommonStats.AttackCooldown))
        {
            _attackTimer = 0f;
            PlayAttackAnimation(character);
        }
    }

    private void PlayAttackAnimation(BaseCharacter character)
    {
        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest, true);
        _isAttackAnimPlaying = true;
        _animTimer = 0f;
    }

    public void Cancel()
    {
        _enemy.DisengageCurrentTarget();
        _target?.Disengage(_attackerGO);
        IsComplete = true;
    }

    private void FaceTarget(BaseCharacter character)
    {
        Vector3 dir = (_target.transform.position - character.transform.position);
        dir.y = 0;
        if (dir == Vector3.zero) return;
        character.transform.rotation = Quaternion.Slerp(
            character.transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * character.RotationSpeed);
    }

    private float GetDistanceToSurface(Vector3 from)
    {
        if (_targetCollider == null)
            return Vector3.Distance(from, _target.transform.position);
        Vector3 closestPoint = _targetCollider.ClosestPoint(from);
        return Vector3.Distance(from, closestPoint);
    }
}