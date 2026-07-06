using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One entry in the roster — maps an EnemyCharacter prefab to its tactical role
/// and the conditions under which the commander prefers to spawn it.
/// Configure entirely in the Inspector; no code changes needed to add new enemies.
/// </summary>
[System.Serializable]
public class EnemyRosterEntry
{
    [Tooltip("The enemy prefab to spawn")]
    public EnemyCharacter Prefab;

    [Tooltip("Human-readable name for debug logs")]
    public string DisplayName = "Unknown Enemy";

    [Header("Unlock")]
    [Tooltip("Earliest night this enemy can appear")]
    public int UnlockNight = 1;

    [Tooltip("Base budget cost — commander spends DifficultyBudget points to field this enemy")]
    public float BudgetCost = 1f;

    [Header("Base Stats (displayed only — actual values live on the prefab)")]
    public float BaseHP;
    public float BaseArmor;
    public bool IsRanged;
    public bool HasEvasion;          // Shadow Runner stealth
    public bool IsSeige;             // Siege Golem building-bonus

    [Header("Resistances this enemy has")]
    public DamageTypeMask Resistances;

    [Header("Counter-pick weights — when does the commander prefer this enemy?")]
    [Tooltip("+weight when player has AoE dominance (use tanks/singles to waste splash)")]
    public float WeightVsAoE;

    [Tooltip("+weight when player has slow dominance (use debuff-immune or berserker types)")]
    public float WeightVsSlow;

    [Tooltip("+weight when player leans on Physical damage (resistant enemy)")]
    public float WeightVsPhysical;

    [Tooltip("+weight when player leans on Fire damage")]
    public float WeightVsFire;

    [Tooltip("+weight when player leans on Frost damage")]
    public float WeightVsFrost;

    [Tooltip("+weight when player leans on Lightning damage")]
    public float WeightVsLightning;

    [Tooltip("+weight when player leans on Bleed damage")]
    public float WeightVsBleed;

    [Tooltip("+weight when player has many damaged towers (siege/sapper types)")]
    public float WeightVsDamagedTowers;

    [Tooltip("+weight when hero is nearby (sends enemies to a different zone instead)")]
    public float WeightAwayFromHero;

    [Tooltip("General late-game weight bonus (ramps after night 15)")]
    public float LateGameBonus;

    /// <summary>
    /// Compute this entry's spawn weight given the current battlefield profile.
    /// Higher = more likely to be picked. Returns 0 if not yet unlocked.
    /// </summary>
    public float ComputeWeight(BattlefieldProfile profile, ZoneProfile targetZone)
    {
        if (profile.NightNumber < UnlockNight) return 0f;

        float w = 1f;   // base weight

        // AoE counter-pick
        if (profile.PlayerHasAoEDominance) w += WeightVsAoE;
        if (profile.PlayerHasSlowDominance) w += WeightVsSlow;

        // Damage-type counter-pick (global dominant)
        w += DamageTypeWeight(profile.GlobalDominantDamage);

        // Per-zone: damaged towers are a sapper's dream
        if (targetZone != null && targetZone.AverageTowerHP < 0.5f)
            w += WeightVsDamagedTowers;

        // Late-game ramp (starts contributing after night 15)
        float lateRamp = Mathf.Clamp01((profile.NightNumber - 15f) / 15f);
        w += LateGameBonus * lateRamp;

        return Mathf.Max(0f, w);
    }

    private float DamageTypeWeight(DamageTypeMask dominant) => dominant switch
    {
        DamageTypeMask.Physical => WeightVsPhysical,
        DamageTypeMask.Fire => WeightVsFire,
        DamageTypeMask.Frost => WeightVsFrost,
        DamageTypeMask.Lightning => WeightVsLightning,
        DamageTypeMask.Bleed => WeightVsBleed,
        _ => 0f,
    };
}

/// <summary>
/// ScriptableObject asset — create one via Assets → Create → Enemy Commander → Enemy Roster.
/// Assign all EnemyRosterEntries here. The CounterPickSelector reads this at dusk.
/// </summary>
[CreateAssetMenu(menuName = "Enemy Commander/Enemy Roster", fileName = "EnemyRoster")]
public class EnemyRosterSO : ScriptableObject
{
    public List<EnemyRosterEntry> Entries = new();

    /// <summary>All entries unlocked for the given night number.</summary>
    public IEnumerable<EnemyRosterEntry> UnlockedEntries(int nightNumber)
    {
        foreach (var e in Entries)
            if (e.UnlockNight <= nightNumber)
                yield return e;
    }
}