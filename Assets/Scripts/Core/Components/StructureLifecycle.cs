using ClickMage.Entities;
using ClickMage.Stats;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EntityTargetable))]
public class StructureLifecycle : MonoBehaviour
{
    [Header("Revival")]
    [Tooltip("Fraction of MaxHealth restored at dawn if this structure was destroyed. 0.3 = 30%.")]
    [SerializeField, Range(0f, 1f)] private float reviveHealthFraction = 0.3f;

    [Header("Regen")]
    [Tooltip("Seconds after last hit before regen resumes.")]
    [SerializeField] private float regenDelay = 3f;
    // regenOnlyDuringDay removed — regen now always ticks (while alive). If you want a
    // night penalty later, make it a rate multiplier here instead of a hard gate.

    [Header("Destroyed Visual")]
    [SerializeField] private GameObject normalVisualRoot;
    [SerializeField] private GameObject destroyedVisualPrefab;
    [SerializeField] private Collider[] collidersToDisableWhenDestroyed;

    private BaseEntity _entity;
    private EntityTargetable _targetable;
    private GameObject _destroyedVisualInstance;
    private float _regenDelayTimer;
    private bool _isDestroyed;

    private void Awake()
    {
        _entity = GetComponent<BaseEntity>();
        _targetable = GetComponent<EntityTargetable>();
    }

    private void OnEnable()
    {
        if (_targetable != null)
        {
            _targetable.OnDamageTaken += HandleDamageTaken;
            _targetable.OnDied += HandleDied;
        }
        SubscribeToDayNight();
    }

    private void OnDisable()
    {
        if (_targetable != null)
        {
            _targetable.OnDamageTaken -= HandleDamageTaken;
            _targetable.OnDied -= HandleDied;
        }
        UnsubscribeFromDayNight();
    }

    private void SubscribeToDayNight()
    {
        if (DayNightCycleManager.Instance == null) { StartCoroutine(RetrySub()); return; }
        DayNightCycleManager.Instance.OnTimeOfDayChanged += HandleTimeOfDayChanged;
    }

    private IEnumerator RetrySub()
    {
        yield return null;
        SubscribeToDayNight();
    }

    private void UnsubscribeFromDayNight()
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
    }

    // Regen now runs in LateUpdate, guaranteeing it evaluates AFTER every combat
    // script's Update() has already applied its damage for this frame. This doesn't
    // fix a "race" (Unity has none between Update calls), but it removes any
    // ordering ambiguity as more systems start touching Health.
    private void LateUpdate()
    {
        if (_isDestroyed) return; // rubble doesn't regen
        TickRegen(Time.deltaTime);
    }

    // ── Damage / Regen ──────────────────────────────────────────────────────

    private void HandleDamageTaken(float amount, BaseEntity attacker, DamageType type)
    {
        _regenDelayTimer = regenDelay;
    }

    private void TickRegen(float deltaTime)
    {
        if (_entity == null) return;
        if (!_entity.HasStat(CommonStats.RegenRate)) return;

        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= deltaTime;
            return;
        }

        float maxHp = _entity.GetStatValueSafe(CommonStats.MaxHealth);
        float curHp = _entity.GetStatValueSafe(CommonStats.Health);
        if (maxHp <= 0f || curHp >= maxHp) return;

        float rate = _entity.GetStatValue(CommonStats.RegenRate);
        if (rate <= 0f) return;

        // Clamp both ends: never regen above max, never below 0 either
        // (defensive — curHp should already be >= 0 once destroyed-gating below is correct).
        float newHp = Mathf.Clamp(curHp + rate * deltaTime, 0f, maxHp);
        _entity.SetStatBaseValue(CommonStats.Health, newHp);
    }

    // ── Death → destroyed state (NOT Destroy()) ─────────────────────────────

    private void HandleDied(Targetable target)
    {
        EnterDestroyedState();
    }

    private void EnterDestroyedState()
    {
        _isDestroyed = true;

        // Explicitly floor HP at 0 here regardless of what raw value TakeDamage stored.
        // This is what actually guarantees "stays at 0 until revive" — it doesn't rely
        // on every damage source (DOTs, reflect, future scripts) clamping correctly on
        // their own.
        if (_entity != null && _entity.HasStat(CommonStats.Health))
            _entity.SetStatBaseValue(CommonStats.Health, 0f);

        if (normalVisualRoot != null) normalVisualRoot.SetActive(false);
        if (destroyedVisualPrefab != null)
            _destroyedVisualInstance = Instantiate(destroyedVisualPrefab, transform.position, transform.rotation, transform);

        foreach (var col in collidersToDisableWhenDestroyed)
            if (col != null) col.enabled = false;

        BuildModeController.Instance.BuildNavMesh();
    }

    // ── Dawn revival ─────────────────────────────────────────────────────────

    private void HandleTimeOfDayChanged(TimeOfDay time)
    {
        if (time != TimeOfDay.Day) return;
        if (!_isDestroyed) return; // survivors just keep regenerating via LateUpdate()

        float maxHp = _entity.GetStatValueSafe(CommonStats.MaxHealth);
        if (maxHp <= 0f) return;

        _entity.SetStatBaseValue(CommonStats.Health, maxHp * reviveHealthFraction);
        ExitDestroyedState();
    }

    private void ExitDestroyedState()
    {
        _isDestroyed = false;

        if (normalVisualRoot != null) normalVisualRoot.SetActive(true);
        if (_destroyedVisualInstance != null) { Destroy(_destroyedVisualInstance); _destroyedVisualInstance = null; }

        foreach (var col in collidersToDisableWhenDestroyed)
            if (col != null) col.enabled = true;

        _targetable.ResetAlive();
        _regenDelayTimer = 0f;
    }

    public bool IsDestroyed => _isDestroyed;
}