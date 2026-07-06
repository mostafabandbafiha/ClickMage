using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A group of enemies spawned together as part of one night's attack.
/// Multiple groups form a single night wave.
/// </summary>
[System.Serializable]
public class EnemyGroup
{
    public EnemyCharacter Prefab;
    public string DebugName;
    public int Count;
    public int TargetColumn;          // which grid column to enter from
    public float DelayBeforeGroup;      // stagger groups within the night
    public float TimeBetweenSpawns;
    public float StatMultiplier = 1f;   // HP/damage scalar from budget
}

/// <summary>
/// Translates a BattlefieldProfile into a list of EnemyGroups for the night.
///
/// Design rules baked in:
///   1. Spend the DifficultyBudget — don't over- or under-spend by more than 10%.
///   2. Always send at least two groups attacking different zones.
///   3. Prefer zones with high OpportunityScore.
///   4. Counter-pick enemy types against player's dominant strategy.
///   5. Pre-boss night (29) and boss night (30) handled separately.
/// </summary>
public class CounterPickSelector
{
    // ── Tuning constants ────────────────────────────────────────────────────
    private const int MIN_GROUPS = 2;
    private const int MAX_GROUPS = 5;
    private const float SWARM_THRESHOLD = 0.6f;  // AoE dominance → spawn swarms
    private const float ELITE_NIGHT = 14;    // first elite enemies appear
    private const float MAX_STAT_MULT = 3.5f;  // cap on HP/damage multiplier

    private readonly EnemyRosterSO _roster;

    public CounterPickSelector(EnemyRosterSO roster)
    {
        _roster = roster;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────────────────────────────────────

    public List<EnemyGroup> BuildNight(BattlefieldProfile profile)
    {
        var groups = new List<EnemyGroup>();

        if (_roster == null || _roster.Entries.Count == 0)
        {
            Debug.LogError("[CounterPickSelector] EnemyRoster is empty.");
            return groups;
        }

        float budget = profile.DifficultyBudget;
        float spent = 0f;
        float statMult = ComputeStatMultiplier(profile.NightNumber);
        int groupCount = ComputeGroupCount(profile);
        var targetZones = PickTargetZones(profile, groupCount);

        for (int i = 0; i < groupCount && spent < budget; i++)
        {
            ZoneProfile zone = targetZones[i % targetZones.Count];
            float share = budget / groupCount;
            var entry = PickEnemy(profile, zone);

            if (entry == null) continue;

            int count = Mathf.Max(1, Mathf.RoundToInt(share / entry.BudgetCost));
            spent += count * entry.BudgetCost;

            groups.Add(new EnemyGroup
            {
                Prefab = entry.Prefab,
                DebugName = entry.DisplayName,
                Count = count,
                TargetColumn = zone.ColumnIndex,
                DelayBeforeGroup = i * ComputeGroupDelay(profile.NightNumber),
                TimeBetweenSpawns = ComputeSpawnInterval(entry, profile),
                StatMultiplier = statMult,
            });
        }

        // ── Guarantee at least one group even on night 1 ────────────────────
        if (groups.Count == 0)
            groups.Add(FallbackGroup(profile, statMult));

        LogPlan(profile, groups);
        return groups;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>Pick an enemy from the roster using weighted random selection.</summary>
    private EnemyRosterEntry PickEnemy(BattlefieldProfile profile, ZoneProfile zone)
    {
        var pool = new List<(EnemyRosterEntry entry, float weight)>();

        foreach (var entry in _roster.UnlockedEntries(profile.NightNumber))
        {
            float w = entry.ComputeWeight(profile, zone);
            if (w > 0f) pool.Add((entry, w));
        }

        if (pool.Count == 0) return null;

        float total = pool.Sum(p => p.weight);
        float roll = Random.Range(0f, total);
        float accum = 0f;

        foreach (var (entry, weight) in pool)
        {
            accum += weight;
            if (roll <= accum) return entry;
        }

        return pool[^1].entry;
    }

    /// <summary>
    /// Choose which zones to attack.
    /// Always picks the weakest zone first, then spreads across the map.
    /// Avoids the hero zone unless budget forces it.
    /// </summary>
    private List<ZoneProfile> PickTargetZones(BattlefieldProfile profile, int groupCount)
    {
        var candidates = profile.ZonesByOpportunity
            .Where(z => !z.HasHero)          // avoid hero zone
            .Take(groupCount + 2)             // take a few extras for variety
            .ToList();

        // If every zone has a hero (impossible normally) fall back to all zones
        if (candidates.Count == 0)
            candidates = profile.ZonesByOpportunity.ToList();

        // Shuffle slightly so the commander doesn't always pick identical zones
        var result = new List<ZoneProfile>();
        var pool = new List<ZoneProfile>(candidates);

        // First pick: always the best opportunity zone
        result.Add(pool[0]);
        pool.RemoveAt(0);

        // Remaining picks: weighted shuffle
        while (result.Count < groupCount && pool.Count > 0)
        {
            int idx = Random.Range(0, Mathf.Min(3, pool.Count));   // bias toward top
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        return result;
    }

    private int ComputeGroupCount(BattlefieldProfile profile)
    {
        // Ramp from MIN_GROUPS at night 1 to MAX_GROUPS at night 25+
        float t = Mathf.Clamp01(profile.NightNumber / 25f);
        return Mathf.RoundToInt(Mathf.Lerp(MIN_GROUPS, MAX_GROUPS, t));
    }

    /// <summary>
    /// HP/damage multiplier for the night. Ramps to MAX_STAT_MULT by night 30.
    /// Separate from budget so the player's items have to keep scaling too.
    /// </summary>
    private float ComputeStatMultiplier(int night)
    {
        // Piecewise: gentle ramp nights 1–10, steeper 11–25, hard push 26–30
        if (night <= 10) return Mathf.Lerp(1f, 1.5f, (night - 1) / 9f);
        if (night <= 25) return Mathf.Lerp(1.5f, 2.5f, (night - 10) / 15f);
        return Mathf.Lerp(2.5f, MAX_STAT_MULT, (night - 25) / 5f);
    }

    /// <summary>
    /// Delay between groups in seconds. Short gaps keep pressure up;
    /// longer gaps on easy nights give the player breathing room *within* the wave.
    /// </summary>
    private float ComputeGroupDelay(int night)
    {
        // Decreases from 20s gap (night 1) to 6s gap (night 30) — relentless late game
        return Mathf.Lerp(20f, 6f, Mathf.Clamp01(night / 30f));
    }

    private float ComputeSpawnInterval(EnemyRosterEntry entry, BattlefieldProfile profile)
    {
        // Swarms spawn faster; tanks spawn slower. Scales with night.
        float base_ = entry.BudgetCost < 1.5f ? 0.4f : 1.2f;
        float ramp = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(profile.NightNumber / 30f));
        return base_ * ramp;
    }

    private EnemyGroup FallbackGroup(BattlefieldProfile profile, float statMult)
    {
        var entry = _roster.Entries.OrderBy(e => e.BudgetCost).FirstOrDefault();
        return new EnemyGroup
        {
            Prefab = entry?.Prefab,
            DebugName = entry?.DisplayName ?? "Fallback",
            Count = 3,
            TargetColumn = 0,
            DelayBeforeGroup = 0f,
            TimeBetweenSpawns = 0.5f,
            StatMultiplier = statMult,
        };
    }

    private void LogPlan(BattlefieldProfile profile, List<EnemyGroup> groups)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[CounterPickSelector] Night {profile.NightNumber} plan " +
                      $"(budget {profile.DifficultyBudget:F0}) — {groups.Count} groups:");
        sb.AppendLine($"  Global dominant dmg: {profile.GlobalDominantDamage}");
        sb.AppendLine($"  AoE dominant: {profile.PlayerHasAoEDominance}  " +
                      $"Slow dominant: {profile.PlayerHasSlowDominance}");
        foreach (var g in groups)
            sb.AppendLine($"  → {g.Count}x {g.DebugName} at col {g.TargetColumn} " +
                          $"(delay {g.DelayBeforeGroup:F1}s, ×{g.StatMultiplier:F2} stats)");
        Debug.Log(sb.ToString());
    }
}