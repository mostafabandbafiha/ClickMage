using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Snapshot of a grid column's combat strength, rebuilt every dusk.
/// "Column" maps directly to GridData columns (0 = enemy entry, Cols-1 = player base).
/// Rows are aggregated so the commander thinks in vertical slices, not individual cells.
/// </summary>
[System.Serializable]
public class ZoneProfile
{
    // ── Identity ────────────────────────────────────────────────────────────
    public int ColumnIndex;          // which grid column this represents

    // ── Player-side threat (how dangerous is this zone TO enemies) ──────────
    public float TotalDPS;           // sum of all tower damage outputs in column
    public float AverageDPS;         // TotalDPS / tower count
    public bool HasAoE;             // any tower with splash / chain effect
    public bool HasSlow;            // any tower with Frost Lens / Frost Bolts
    public bool HasPoison;          // any tower with Serrated Edge / Flame Oil DoT
    public float ArmorReduction;     // sum of Piercing Tip armor-shred values
    public float TowerCount;         // living towers in column
    public float AverageTowerHP;     // avg HP ratio — low means damaged / weak
    public float RangeScore;         // avg effective attack range of towers
    public bool HasHero;            // player hero currently standing in this column

    // ── Commander-side opportunity (how attractive is this zone TO enemies) ─
    public float OpportunityScore;   // computed: high = good attack corridor
    public float HistoricSurvival;   // avg seconds enemies lived in this zone last night

    // ── Damage-type coverage (what the player is leaning on here) ───────────
    public DamageTypeMask DominantDamageType;

    public override string ToString() =>
        $"Zone[{ColumnIndex}] DPS={TotalDPS:F0} AoE={HasAoE} Slow={HasSlow} " +
        $"Towers={TowerCount} Opportunity={OpportunityScore:F2}";
}

/// <summary>
/// Aggregate snapshot of the entire battlefield, produced by ThreatScanner.
/// </summary>
public class BattlefieldProfile
{
    public List<ZoneProfile> Zones = new();
    public float TotalPlayerDPS;
    public bool PlayerHasAoEDominance;   // >50% of towers have AoE
    public bool PlayerHasSlowDominance;  // >40% of towers have slow
    public DamageTypeMask GlobalDominantDamage;    // what the player leans on globally
    public int NightNumber;
    public float DifficultyBudget;        // scales enemy stat multipliers
    public ZoneProfile WeakestZone;             // lowest opportunity-adjusted defense
    public ZoneProfile StrongestZone;           // highest defense — commander avoids this

    /// <summary>Zones sorted by OpportunityScore descending (best attack corridors first).</summary>
    public List<ZoneProfile> ZonesByOpportunity = new();
}

/// <summary>
/// Bitmask of damage flavours the commander tracks.
/// Matches items in the item table: Fire, Frost, Lightning, Physical, Bleed.
/// </summary>
[System.Flags]
public enum DamageTypeMask
{
    None = 0,
    Physical = 1 << 0,
    Fire = 1 << 1,
    Frost = 1 << 2,
    Lightning = 1 << 3,
    Bleed = 1 << 4,
    Magic = 1 << 5,
}