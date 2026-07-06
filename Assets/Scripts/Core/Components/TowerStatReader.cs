using ClickMage.Entities;
using ClickMage.Stats;
using UnityEngine;

/// <summary>
/// Thin read-only adapter that sits on every tower and exposes combat-relevant
/// stats to ThreatScanner — reading directly from the tower's existing StatHolder.
///
/// Does NOT own any stats. Does NOT apply any modifiers.
/// The StatHolder + StatItemEffect system already handles everything via item equip/unequip.
///
/// Setup: add this component to your tower prefab alongside your tower's BaseEntity subclass.
/// No further configuration needed — it finds the StatHolder automatically.
/// </summary>
[RequireComponent(typeof(BaseEntity))]  // swap BaseEntity for your concrete tower base if needed
public class TowerStatReader : MonoBehaviour
{
    // ── Cached reference ──────────────────────────────────────────────────────
    private BaseEntity _entity;

    private void Awake()
    {
        _entity = GetComponent<BaseEntity>();
        if (_entity == null)
            Debug.LogError($"[TowerStatReader] No BaseEntity found on {name}. " +
                           "Attach TowerStatReader to a tower that inherits BaseEntity.");
    }

    // ── Convenience readers (used by ThreatScanner) ───────────────────────────

    public float Damage => Stat(CommonStats.Damage);
    public float AttackSpeed => Mathf.Max(0.01f, Stat(CommonStats.AttackSpeed));
    public float Range => Stat(CommonStats.AttackRange);
    public float MaxHP => Stat(CommonStats.MaxHealth);
    public float CurrentHP => Stat(CommonStats.Health);
    public float Armor => Stat(CommonStats.Armor);
    public float ArmorPiercing => Stat(CommonStats.ArmorPiercing);
    public float SlowAmount => Stat(CommonStats.SlowAmount);   // 0–1
    public float FireDamage => Stat(CommonStats.FireDamage);
    public float FrostDamage => Stat(CommonStats.FrostDamage);
    public float LightningDamage => Stat(CommonStats.LightningDamage);
    public float BleedDamage => Stat(CommonStats.BleedDamage);

    // ── Derived values ────────────────────────────────────────────────────────

    /// <summary>Physical damage per second (base damage × attack speed).</summary>
    public float PhysicalDPS => Damage * AttackSpeed;

    /// <summary>Total estimated DPS including elemental damage ticks.</summary>
    public float TotalDPS => PhysicalDPS + FireDamage + FrostDamage + LightningDamage + BleedDamage;

    /// <summary>HP ratio 0–1. Below 0.5 signals a damaged tower worth targeting.</summary>
    public float HPRatio => MaxHP > 0f ? Mathf.Clamp01(CurrentHP / MaxHP) : 0f;

    /// <summary>True when a Frost item gives meaningful slow (> 10%).</summary>
    public bool HasSlow => SlowAmount > 0.1f;

    /// <summary>True when the tower deals any elemental DoT (fire / bleed).</summary>
    public bool HasPoison => (FireDamage > 0f || BleedDamage > 0f)
                          && Stat(CommonStats.HasPoison) > 0f;

    /// <summary>True when any AoE item is equipped (HasAoE stat > 0).</summary>
    public bool HasAoE => Stat(CommonStats.AreaRadius) > 0f;

    /// <summary>True when the tower chains (Storm Capacitor / Overcharge Cell).</summary>
    public bool HasChain => Stat(CommonStats.ChainCount) > 0f;

    /// <summary>Builds a DamageTypeMask from which elemental stats are non-zero.</summary>
    public DamageTypeMask ActiveDamageTypes
    {
        get
        {
            var mask = DamageTypeMask.Physical;   // towers always deal at least some physical
            if (FireDamage > 0f) mask |= DamageTypeMask.Fire;
            if (FrostDamage > 0f) mask |= DamageTypeMask.Frost;
            if (LightningDamage > 0f) mask |= DamageTypeMask.Lightning;
            if (BleedDamage > 0f) mask |= DamageTypeMask.Bleed;
            if (HasChain) mask |= DamageTypeMask.Magic;   // chain = magic type
            return mask;
        }
    }

    // ── Private helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Safely reads a stat value. Returns 0 if the stat doesn't exist on this tower
    /// (not all towers have all stats — e.g. a Wall has no AttackSpeed).
    /// </summary>
    private float Stat(string key)
    {
        if (_entity == null) return 0f;
        return _entity.HasStat(key) ? _entity.GetStatValue(key) : 0f;
    }
}