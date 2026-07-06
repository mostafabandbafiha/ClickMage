// CombatCharacter.cs � shared base for anything that can target, seek, and attack.
// EnemyCharacter and HeroCharacter both derive from this so the attack pipeline
// (MoveToTargetCommand, AttackCommand, CombatAttackState, AttackSeekBehaviorNode)
// is written once and works for both factions.
using UnityEngine;

public abstract class CombatCharacter : BaseCharacter
{
    [SerializeField] private Faction _targetFaction;

    public Targetable CurrentTarget { get; set; }
    public Faction TargetFaction => _targetFaction;

    protected override void Awake()
    {
        base.Awake();
    }

    public Targetable FindNearestTarget()
    {
        if (TargetRegistry.Instance == null) return null;
        return TargetRegistry.Instance.GetNearest(_targetFaction, transform.position);
    }

    /// <summary>Called by CombatAttackState when the attack animation hit-frame fires.</summary>
    public virtual void OnAttack() { }

    /// <summary>Releases this attacker's claimed engager slot on its current target, if any.</summary>
    public void DisengageCurrentTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.Disengage(gameObject);
            CurrentTarget = null;
        }
    }

    protected virtual void OnDestroy()
    {
        // Mirrors Tower.OnDestroy(): without this, a character that dies mid-chase
        // (MoveToTargetCommand, before AttackCommand.Start() ever runs) or mid-attack
        // never releases the engager slot it claimed via TryEngage() — nothing else
        // in the death path (Destroy(gameObject) alone) cancels the active command.
        // Those ghost claims accumulate on towers/heroes until MaxCapacity is hit,
        // at which point no new enemy can ever engage that target again, even though
        // the real attacker count is far lower. Cancelling the command queue here
        // routes through each command's own Cancel(), which disengages properly.
        DisengageCurrentTarget();
    }
}