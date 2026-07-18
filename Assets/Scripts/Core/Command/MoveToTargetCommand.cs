// MoveToTargetCommand.cs — generic for any CombatCharacter (Enemy or Hero)
using ClickMage.Interface;
using ClickMage.Stats;
using UnityEngine;

public class MoveToTargetCommand : ICommand<BaseCharacter>
{
    private const float StuckGiveUpSeconds = 3f;
    private const float StuckProgressThreshold = 0.05f;

    private readonly Targetable _target;
    private readonly CombatCharacter _attacker;
    private Collider _targetCollider;
    private float _stuckTimer;
    private float _bestRemainingDistance = float.MaxValue;
    public bool IsComplete { get; private set; }

    public MoveToTargetCommand(Targetable target, CombatCharacter attacker)
    {
        _target = target;
        _attacker = attacker;
    }

    public void Start(BaseCharacter character)
    {
        character.ClearBlocked();
        if (_target == null || !_target.IsAlive)
        {
            IsComplete = true;
            return;
        }
        _targetCollider = _target.GetComponent<Collider>();
        character.SetDestination(GetDestination(character));
        character.StateMachine.ChangeState(new CharacterMovingState());
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        if (_target == null || !_target.IsAlive)
        {
            if (_attacker.CurrentTarget == _target)
                _attacker.DisengageCurrentTarget();
            IsComplete = true;
            character.StopMoving();
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        // Give up if the target has moved beyond this attacker's aggro radius
        // (e.g. a Hero that retreated) instead of chasing forever. Only meaningful
        // for targets that can actually move — a stationary Block/Castle can't
        // flee, so applying this to them just abandoned engagements picked up near
        // the edge of ScoredAdvanceNode's forward cone (which can reach further
        // than AggroRadius) before the attacker ever got a chance to close in.
        bool targetCanFlee = _target.GetComponent<BaseCharacter>() != null;
        if (targetCanFlee)
        {
            float aggroRadius = (_attacker as EnemyCharacter)?.AggroRadius ?? float.MaxValue;
            float distToTarget = Vector3.Distance(character.transform.position, _target.transform.position);
            if (distToTarget > aggroRadius)
            {
                GiveUp(character, blocked: false);
                return;
            }
        }

        character.Agent.speed = character.GetStatValue(CommonStats.MoveSpeed);
        character.SetDestination(GetDestination(character));

        float distToSurface = GetDistanceToSurface(character.transform.position);
        float attackRange = _attacker.GetStatValue(CommonStats.AttackRange);

        // Give up if we're not making real progress toward the target — e.g. it's
        // behind a wall we can't reach. Without this an enemy will sit there
        // trying forever. Only measured once the path is resolved (not pending).
        if (!character.Agent.pathPending)
        {
            if (character.Agent.remainingDistance < _bestRemainingDistance - StuckProgressThreshold)
            {
                _bestRemainingDistance = character.Agent.remainingDistance;
                _stuckTimer = 0f;
            }
            else
            {
                _stuckTimer += deltaTime;
                if (_stuckTimer >= StuckGiveUpSeconds)
                {
                    GiveUp(character, blocked: true);
                    return;
                }
            }
        }

        // Check distance to collider surface, not center
        if (distToSurface <= attackRange)
        {
            IsComplete = true;
            character.StopMoving();
            character.StateMachine.ChangeState(new CharacterIdleState());
        }
    }

    public void Cancel()
    {
        _target?.Disengage(_attacker.gameObject);
        if (_attacker != null && _attacker.CurrentTarget == _target)
            _attacker.DisengageCurrentTarget();
        IsComplete = true;
    }

    private void GiveUp(BaseCharacter character, bool blocked)
    {
        _target.Disengage(_attacker.gameObject);
        if (_attacker.CurrentTarget == _target)
            _attacker.DisengageCurrentTarget();
        IsComplete = true;
        character.StopMoving();
        character.StateMachine.ChangeState(new CharacterIdleState());
        if (blocked) character.MarkBlocked();
    }

    private Vector3 GetDestination(BaseCharacter character)
    {
        if (_targetCollider == null)
            return _target.transform.position;
        return _targetCollider.ClosestPoint(character.transform.position);
    }

    private float GetDistanceToSurface(Vector3 from)
    {
        if (_targetCollider == null)
            return Vector3.Distance(from, _target.transform.position);
        Vector3 closestPoint = _targetCollider.ClosestPoint(from);
        return Vector3.Distance(from, closestPoint);
    }
}