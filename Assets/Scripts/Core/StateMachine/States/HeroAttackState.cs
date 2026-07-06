// HeroAttackState.cs
// Exact clone of CharacterHarvestingState with target instead of node.

using ClickMage.Animation;
using ClickMage.StateMachine;
using ClickMage.Stats;
using UnityEngine;

public class HeroAttackState : IState<BaseCharacter>
{
    private readonly Targetable _target;
    private HeroCharacter _hero;
    private bool _isReady;

    private const float RotationSpeed = 720f;

    public HeroAttackState(Targetable target)
    {
        _target = target;
    }

    public void Enter(BaseCharacter character)
    {
        _isReady = false;
        _hero = character as HeroCharacter;

        if (_hero == null || _target == null || !_target.IsAlive)
        {
            _hero = null;
            return;
        }

        character.Agent.ResetPath();
        SnapFaceTarget(character);
        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);
        _isReady = true;
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        if (!_isReady) return;

        if (_target == null || !_target.IsAlive)
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        float attackRange = _hero.HasStat(CommonStats.AttackRange)
            ? _hero.GetStatValue(CommonStats.AttackRange) : 2f;

        float dist = Vector3.Distance(character.transform.position, _target.transform.position);
        if (dist > attackRange + 0.5f)
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        SmoothFaceTarget(character, deltaTime);
    }

    public void Exit(BaseCharacter character)
    {
        _isReady = false;
        _hero = null;
    }

    // ── Called by Animation Event (hit frame) ─────────────────────────────

    public void OnAttack(BaseCharacter character)
    {
        if (_hero == null || _target == null || !_target.IsAlive) return;

        float damage = _hero.HasStat(CommonStats.Damage)
            ? _hero.GetStatValue(CommonStats.Damage) : 10f;

        _target.TakeDamage(damage, _hero);

        Debug.Log($"[HeroAttackState] {character.name} hit {_target.name} for {damage}");
    }

    // ── Called by Animation Event (swing end frame) ───────────────────────

    public void OnAttackComplete(BaseCharacter character)
    {
        if (_target == null || !_target.IsAlive)
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);
    }

    // ── Rotation helpers ──────────────────────────────────────────────────

    private void SnapFaceTarget(BaseCharacter character)
    {
        Vector3 dir = GetDirectionToTarget(character);
        if (dir == Vector3.zero) return;
        character.transform.rotation = Quaternion.LookRotation(dir);
    }

    private void SmoothFaceTarget(BaseCharacter character, float deltaTime)
    {
        Vector3 dir = GetDirectionToTarget(character);
        if (dir == Vector3.zero) return;
        character.transform.rotation = Quaternion.RotateTowards(
            character.transform.rotation,
            Quaternion.LookRotation(dir),
            RotationSpeed * deltaTime);
    }

    private Vector3 GetDirectionToTarget(BaseCharacter character)
    {
        Vector3 dir = _target.transform.position - character.transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude < 0.001f ? Vector3.zero : dir.normalized;
    }
}