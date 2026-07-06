// EntityTargetable.cs — expose stat read for the registry (only addition)
using ClickMage.Entities;
using ClickMage.Stats;
using System.Collections;
using UnityEngine;

public interface IImpactReactive
{
    void OnImpact();
}

public class EntityTargetable : Targetable, IImpactReactive
{
    private BaseEntity _entity;

    [Header("Impact Pulse")]
    private float pulseHeight = 3f;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _originalLocalPos;
    private Coroutine _pulseRoutine;

    private void Awake()
    {
        _entity = GetComponent<BaseEntity>();
        if (_entity == null)
            Debug.LogError($"[EntityTargetable] No BaseEntity found on {name}");

        _originalLocalPos = transform.localPosition;
    }

    private void OnEnable() => TargetRegistry.Instance?.Register(this);

    public override int MaxCapacity
    {
        get
        {
            if (Faction == Faction.Enemy) return int.MaxValue;

            if (_entity != null && _entity.HasStat(CommonStats.MaxEngagers))
                return Mathf.RoundToInt(_entity.GetStatValue(CommonStats.MaxEngagers));

            return base.MaxCapacity;
        }
    }

    // NEW — generic stat read for registry checks (invisibility, etc.)
    public float GetStatValue(string statKey)
    {
        if (_entity == null || !_entity.HasStat(statKey)) return 0f;
        return _entity.GetStatValue(statKey);
    }


    // EntityTargetable.cs
    public override void TakeDamage(float damage, BaseEntity attacker = null, DamageType type = DamageType.Normal)
    {
        if (!IsAlive || _entity == null) return;

        float mitigatedDamage = ApplyArmorMitigation(damage);

        if (attacker != null)
            ApplyReflect(mitigatedDamage, attacker);

        float hp = _entity.GetStatValue(CommonStats.Health) - mitigatedDamage;
        _entity.SetStatBaseValue(CommonStats.Health, hp);

        RaiseDamageTaken(mitigatedDamage, attacker, type);

        if (attacker != null)
        {
            ApplyOnHitDots(attacker);
            ApplyArmorShred(attacker);
            ApplyLifesteal(mitigatedDamage, attacker);
        }

        if (hp <= 0f) OnDeath();
    }

    private float ApplyArmorMitigation(float rawDamage)
    {
        if (!_entity.HasStat(CommonStats.Armor)) return rawDamage;

        float armor = _entity.GetStatValue(CommonStats.Armor);
        if (armor <= 0f) return rawDamage;

        // Standard diminishing-returns formula: 100 armor = 50% reduction, 200 = 66%, etc.
        float reduction = armor / (armor + 100f);
        return rawDamage * (1f - reduction);
    }

    private StatusEffectHandler GetOrAddHandler()
    {
        var handler = GetComponent<StatusEffectHandler>();
        if (handler == null) handler = gameObject.AddComponent<StatusEffectHandler>();
        return handler;
    }

    private void ApplyOnHitDots(BaseEntity attacker)
    {
        TryApplyDot(attacker, "fire", CommonStats.FireOnHitDamage, CommonStats.FireOnHitDuration, CommonStats.FireOnHitTick);
        TryApplyDot(attacker, "bleed", CommonStats.BleedOnHitDamage, CommonStats.BleedOnHitDuration, CommonStats.BleedOnHitTick);
    }

    private void TryApplyDot(BaseEntity attacker, string effectId, string dmgKey, string durKey, string tickKey)
    {
        if (!attacker.HasStat(dmgKey)) return;
        float dmg = attacker.GetStatValue(dmgKey);
        if (dmg <= 0f) return;

        GetOrAddHandler().Apply(new StatusEffectInstance
        {
            EffectId = effectId,
            SourceId = effectId,
            DamagePerTick = dmg,
            TickInterval = attacker.HasStat(tickKey) ? attacker.GetStatValue(tickKey) : 1f,
            TimeRemaining = attacker.HasStat(durKey) ? attacker.GetStatValue(durKey) : 3f,
            MaxStacks = 1
        });
    }

    private void ApplyArmorShred(BaseEntity attacker)
    {
        if (!attacker.HasStat(CommonStats.ArmorShredPerHit)) return;
        float shred = attacker.GetStatValue(CommonStats.ArmorShredPerHit);
        if (shred <= 0f) return;

        int maxStacks = attacker.HasStat(CommonStats.ArmorShredMaxStacks)
            ? Mathf.RoundToInt(attacker.GetStatValue(CommonStats.ArmorShredMaxStacks)) : 1;
        float duration = attacker.HasStat(CommonStats.ArmorShredDuration)
            ? attacker.GetStatValue(CommonStats.ArmorShredDuration) : 3f;

        GetOrAddHandler().Apply(new StatusEffectInstance
        {
            EffectId = "armor_shred",
            SourceId = "armor_shred",
            ModifiedStatKey = CommonStats.Armor,
            ModifierValuePerStack = -shred,
            MaxStacks = maxStacks,
            TimeRemaining = duration
        });
    }

    private void ApplyReflect(float incomingDamage, BaseEntity attacker)
    {
        if (!_entity.HasStat(CommonStats.ReflectPercent)) return;
        float pct = _entity.GetStatValue(CommonStats.ReflectPercent);
        if (pct <= 0f) return;

        var attackerTargetable = attacker.GetComponent<Targetable>();
        if (attackerTargetable != null && attackerTargetable.IsAlive)
            attackerTargetable.TakeDamage(incomingDamage * pct, null);
    }

    private void ApplyLifesteal(float damageDealt, BaseEntity attacker)
    {
        if (!attacker.HasStat(CommonStats.LifestealPercent)) return;
        float pct = attacker.GetStatValue(CommonStats.LifestealPercent);
        if (pct <= 0f) return;

        float heal = damageDealt * pct;
        float hp = attacker.GetStatValue(CommonStats.Health) + heal;
        attacker.SetStatBaseValue(CommonStats.Health, hp);
    }


    public void ResetAlive()
    {
        IsAlive = true;

        // Death unregisters this from TargetRegistry (see Targetable.OnDeath). The
        // GameObject is never disabled/re-enabled on revive, so OnEnable never fires
        // again to re-register it — without this call the entity stays alive but
        // permanently invisible to TargetRegistry queries (GetNearest/GetNearestInRange).
        TargetRegistry.Instance?.Register(this);
    }

    protected override void OnDeath()
    {
        if (!IsAlive) return;
        if (_entity is EnemyCharacter enemy)
            enemy.Die(EnemyCharacter.DeathCause.KilledByPlayer);
        else if (_entity is Block block)
            block.OnBlockDestroyed();
        else if (_entity is Tower tower)
            tower.StateMachine.ChangeState(new TowerDiedState());
        else if (_entity is Castle castle)
            castle.NotifyFell();

        base.OnDeath();
    }

    public void OnImpact()
    {
        if (_pulseRoutine != null)
            StopCoroutine(_pulseRoutine);

        _pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float half = pulseDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float p = pulseCurve.Evaluate(t / half);
            transform.localPosition = _originalLocalPos + Vector3.up * (pulseHeight * p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = pulseCurve.Evaluate(t / half);
            transform.localPosition = _originalLocalPos + Vector3.up * (pulseHeight * (1f - p));
            yield return null;
        }

        transform.localPosition = _originalLocalPos;
        _pulseRoutine = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
            transform.localPosition = _originalLocalPos;
        }
    }

}