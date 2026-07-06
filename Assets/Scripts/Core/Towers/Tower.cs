using ClickMage.Entities;
using ClickMage.StateMachine;
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tower : BaseEntity
{
    [SerializeField] protected TowerData towerData;
    [SerializeField] private Faction _targetFaction;

    [Header("Tower Stats")]
    [SerializeField] private BaseStat range;
    [SerializeField] private BaseStat damage;
    [SerializeField] private BaseStat fireRate;
    [SerializeField] private BaseStat health;
    [SerializeField] private BaseStat maxHealth;

    [Header("Ref")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _turret;
    

    protected StateMachine<Tower> stateMachine;
    protected Targetable currentTarget;
    protected float lastFireTime;

    public float Range => GetStatValue(CommonStats.Range);
    public float Damage => GetStatValue(CommonStats.Damage);
    public float FireRate => GetStatValue(CommonStats.FireRate);
    public float Health => GetStatValue(CommonStats.Health);
    public float MaxHealth => GetStatValue(CommonStats.MaxHealth);
    public float RotationSpeed => towerData.rotationSpeed;

    public Transform FirePoint { get; protected set; }
    public Targetable CurrentTarget => currentTarget;
    public StateMachine<Tower> StateMachine => stateMachine;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    protected override void Awake()
    {
        stateMachine = new StateMachine<Tower>(this);
        FirePoint = _firePoint;
        statAssets = BuildStatAssetList();
        base.Awake();
    }

    protected virtual void Start()
    {
        stateMachine.ChangeState(new TowerIdleState());
    }

    protected virtual void Update()
    {
        stateMachine.Tick(Time.deltaTime);
    }

    protected virtual void OnDestroy()
    {
        // Clean up engagement when tower is destroyed
        DisengageCurrentTarget();
    }

    // ── Targeting ─────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the nearest alive target that has available capacity
    /// and successfully engages it. Returns null if none found.
    /// </summary>
    public Targetable FindAndEngageTarget()
    {
        if (TargetRegistry.Instance == null) return null;

        Targetable nearest = TargetRegistry.Instance.GetNearestInRange(
            _targetFaction,
            transform.position,
            Range);

        if (nearest == null) return null;

        nearest.OnDied += HandleTargetDied;
        currentTarget = nearest;
        return nearest;
    }

    public bool IsTargetInRange(Targetable target)
    {
        if (target == null || !target.IsAlive) return false;
        return Vector3.Distance(transform.position, target.Position) <= Range;
    }


    /// <summary>
    /// Cleanly disengage from current target.
    /// Unsubscribes from OnDied and frees the capacity slot.
    /// </summary>

    public void DisengageCurrentTarget()
    {
        if (currentTarget == null) return;
        currentTarget.OnDied -= HandleTargetDied;
        currentTarget = null;
    }

    // ── Event handler ─────────────────────────────────────────────────────
    private void HandleTargetDied(Targetable target)
    {
        target.OnDied -= HandleTargetDied;
        currentTarget = null;
        stateMachine.ChangeState(new TowerIdleState());
    }

    public void HandleDayNightChanged(TimeOfDay time)
    {
        if (time == TimeOfDay.Day) Revive();
    }

    private void Revive()
    {
        SetStatBaseValue(CommonStats.Health, MaxHealth);

        // Reset IsAlive so the registry sees it again
        var targetable = GetComponent<EntityTargetable>();
        if (targetable != null) targetable.ResetAlive();

        StateMachine.ChangeState(new TowerIdleState());
    }
    // ── Combat ────────────────────────────────────────────────────────────
    public void RotateTowardsTarget(Targetable target)
    {
        if (target == null) return;

        Vector3 direction = (target.Position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _turret.rotation = Quaternion.Slerp(
                _turret.rotation,
                targetRotation,
                RotationSpeed * Time.deltaTime
            );
        }
    }

    public bool CanFire()
    {
        return Time.time >= lastFireTime + (1f / FireRate);
    }

    public virtual void Fire(Targetable target)
    {
        if (target == null || !target.IsAlive) return;

        lastFireTime = Time.time;

        if (towerData.projectilePrefab != null)
        {
            GameObject projectileObj = Instantiate(
                towerData.projectilePrefab,
                _firePoint.position,
                _firePoint.rotation
            );

            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, Damage, this);
        }

        if (towerData.shootSound != null)
            AudioSource.PlayClipAtPoint(towerData.shootSound, transform.position);
    }

    // ── Kept for backwards compatibility with tower states ────────────────
    public void ClearTarget()
    {
        DisengageCurrentTarget();
    }

    // ── Private helpers ───────────────────────────────────────────────────
    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        TryAdd(list, health);
        TryAdd(list, maxHealth);
        TryAdd(list, range);
        TryAdd(list, damage);
        TryAdd(list, fireRate);
        return list;
    }

    private static void TryAdd(List<BaseStat> list, BaseStat stat)
    {
        if (stat != null) list.Add(stat);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Tower State")]
    private void DebugTowerState()
    {
        Debug.Log($"FireRate: {FireRate}");
        Debug.Log($"CanFire: {CanFire()}");
        Debug.Log($"FirePoint: {FirePoint}");
        Debug.Log($"ProjectilePrefab: {towerData?.projectilePrefab}");
        Debug.Log($"CurrentTarget: {currentTarget}");
        Debug.Log($"State: {stateMachine.CurrentState}");
    }
#endif
}
