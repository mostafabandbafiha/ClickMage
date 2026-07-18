using UnityEngine;

public abstract class CombatCharacter : BaseCharacter
{
    [SerializeField] private Faction _targetFaction;

    // Was: public Targetable CurrentTarget { get; set; }
    // Private backing field + guarded setter below — nothing outside this
    // class can silently overwrite it and leak an engagement slot anymore.
    public Targetable CurrentTarget { get; private set; }

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

    public virtual void OnAttack() { }

    /// <summary>
    /// The ONLY way CurrentTarget should ever change. Guarantees that switching
    /// from one target to another always releases the previously-claimed engager
    /// slot first — three different call sites (AttackCommand, ScoredAdvanceNode,
    /// AcquireCombatTargetNode) used to assign CurrentTarget directly, and none of
    /// them checked whether a different target was already claimed. That silently
    /// leaked a slot on the old target every time a character switched targets
    /// mid-engagement (not on death — death cleanup was already handled by
    /// OnDestroy — but on ordinary retargeting, e.g. hero BT re-picking a closer
    /// enemy while still engaged with the first one).
    /// </summary>
    public void SetCombatTarget(Targetable newTarget)
    {
        if (CurrentTarget == newTarget) return;

        if (CurrentTarget != null)
            CurrentTarget.Disengage(gameObject);

        CurrentTarget = newTarget;
    }

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
        DisengageCurrentTarget();
    }
}