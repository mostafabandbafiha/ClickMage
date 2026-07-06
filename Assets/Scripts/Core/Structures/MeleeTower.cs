using UnityEngine;

public class MeleeTower : Tower
{
    [Header("Melee")]
    [SerializeField] private Animator _bladeAnimator;
    [SerializeField] private Transform _bladeHitOrigin;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private Faction _targetFaction;

    private static readonly int SpinBool = Animator.StringToHash("IsSpinning");

    // ── State machine override ────────────────────────────────────────────
    // MeleeTower goes straight to MeleeAttackingState instead of
    // AcquiringTarget → Shooting. Idle still works the same way.
    protected override void Start()
    {
        // Let Tower.Start() set up the state machine, then we're done —
        // TowerIdleState already calls FindAndEngageTarget(), but for melee
        // we don't care about a single target, so we override FindAndEngageTarget
        // to just return any nearby enemy without locking onto it.
        // Initialize the state machine from Tower, then immediately
        // replace the initial state with the melee-specific idle
        base.Start();
        stateMachine.ChangeState(new MeleeIdleState());
    }

    // Called by TowerIdleState when it finds a target.
    // We intercept here to go to melee attacking instead of acquiring.
    public new Targetable FindAndEngageTarget()
    {
        if (!HasEnemiesInRange()) return null;

        // Return a dummy non-null so TowerIdleState transitions away.
        // We don't actually track a single target.
        currentTarget = GetAnyEnemyInRange();
        return currentTarget;
    }

    // TowerIdleState transitions to TowerAcquiringTargetState normally,
    // so we override the state it transitions TO by overriding Start to
    // replace the idle state with one that goes to MeleeAttackingState.
    // Simplest approach: give MeleeTower its own idle that skips acquiring.
    protected override void Update()
    {
        // Sync animator speed to FireRate every frame so changing
        // the FireRate stat at runtime immediately affects spin speed
        if (_bladeAnimator != null)
            _bladeAnimator.speed = FireRate;

        base.Update(); // ticks the state machine
    }

    // ── Animator control ──────────────────────────────────────────────────
    public void SetSpinning(bool on)
    {
        if (_bladeAnimator != null)
            _bladeAnimator.SetBool(SpinBool, on);
    }

    // ── Range queries (used by state and animation event) ─────────────────
    public bool HasEnemiesInRange()
    {
        return GetAnyEnemyInRange() != null;
    }

    private Targetable GetAnyEnemyInRange()
    {
        return TargetRegistry.Instance?.GetNearestInRange(
            _targetFaction,
            transform.position,
            Range);
    }

    // ── Animation Event — called by Animator on each blade pass ──────────
    // The animation event fires on every swing frame.
    // We ask the registry for everything in range and hit them all.
    public void OnBladeHit()
    {
        if (TargetRegistry.Instance == null) return;

        var targets = TargetRegistry.Instance.GetTargets(_targetFaction);
        float rangeSq = Range * Range;

        foreach (var target in targets)
        {
            if (target == null || !target.IsAlive) continue;

            float distSq = (target.Position - transform.position).sqrMagnitude;
            if (distSq > rangeSq) continue;

            target.TakeDamage(Damage, this);
        }
    }

    // ── Fire() is unused for melee — attacking is animation-driven ────────
    public override void Fire(Targetable target) { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(
            _bladeHitOrigin != null ? _bladeHitOrigin.position : transform.position,
            Range);
    }
#endif
}