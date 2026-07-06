using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runs at dusk and produces a BattlefieldProfile by walking every grid column
/// and reading TowerStatReader (which reads from each tower's StatHolder).
///
/// Also maintains a ZoneSurvivalMemory across nights so the commander learns
/// which corridors are soft and keeps hammering them.
/// </summary>
public class ThreatScanner : MonoBehaviour
{
    // ── Dependencies ──────────────────────────────────────────────────────────
    private GridData _grid;
    private Transform _heroTransform;

    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Tuning")]
    [SerializeField]
    [Range(0f, 1f)]
    private float _memoryWeight = 0.4f;   // how much last-night survival influences opportunity

    [SerializeField]
    private float _heroPenalty = 0.5f;   // hero presence reduces zone attractiveness

    [SerializeField]
    private float _damagedBonus = 0.3f;   // damaged towers boost zone attractiveness

    // ── Survival memory ───────────────────────────────────────────────────────
    // col → exponential-moving-average of seconds enemies lived there
    private readonly Dictionary<int, float> _survivalMemory = new();

    // ── Injection API ─────────────────────────────────────────────────────────
    public void Inject(GridData grid) => _grid = grid;
    public void InjectHero(Transform hero) => _heroTransform = hero;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call at dusk. Returns a fresh BattlefieldProfile ready for CounterPickSelector.
    /// </summary>
    public BattlefieldProfile Scan(int nightNumber, float difficultyBudget)
    {
        if (_grid == null)
        {
            Debug.LogError("[ThreatScanner] GridData not injected.");
            return new BattlefieldProfile { NightNumber = nightNumber, DifficultyBudget = difficultyBudget };
        }

        var profile = new BattlefieldProfile
        {
            NightNumber = nightNumber,
            DifficultyBudget = difficultyBudget,
        };

        int heroCol = GetHeroColumn();

        for (int col = 0; col < _grid.Cols; col++)
        {
            var zone = ScanColumn(col, heroCol);
            profile.Zones.Add(zone);
            profile.TotalPlayerDPS += zone.TotalDPS;
        }

        // ── Global flags ──────────────────────────────────────────────────────
        int totalTowers = profile.Zones.Sum(z => (int)z.TowerCount);

        if (totalTowers > 0)
        {
            int aoeTowers = profile.Zones.Count(z => z.HasAoE);
            int slowTowers = profile.Zones.Count(z => z.HasSlow);
            profile.PlayerHasAoEDominance = aoeTowers / (float)totalTowers > 0.5f;
            profile.PlayerHasSlowDominance = slowTowers / (float)totalTowers > 0.4f;
        }

        profile.GlobalDominantDamage = ComputeGlobalDominantDamage(profile.Zones);

        profile.ZonesByOpportunity = profile.Zones
            .OrderByDescending(z => z.OpportunityScore)
            .ToList();

        profile.WeakestZone = profile.ZonesByOpportunity.FirstOrDefault();
        profile.StrongestZone = profile.ZonesByOpportunity.LastOrDefault();

        return profile;
    }

    /// <summary>
    /// Feed back last night's per-column survival data.
    /// key = grid column, value = average seconds enemies lived there.
    /// Called by SmartEnemyCommander at dawn.
    /// </summary>
    public void RecordNightResults(Dictionary<int, float> colAvgSurvival)
    {
        foreach (var kv in colAvgSurvival)
        {
            if (_survivalMemory.TryGetValue(kv.Key, out float prev))
                _survivalMemory[kv.Key] = Mathf.Lerp(prev, kv.Value, 0.6f);   // recent nights weighted more
            else
                _survivalMemory[kv.Key] = kv.Value;
        }
    }

    // ── Column scan ───────────────────────────────────────────────────────────

    private ZoneProfile ScanColumn(int col, int heroCol)
    {
        var zone = new ZoneProfile { ColumnIndex = col };

        float totalHP = 0f;
        float totalMaxHP = 0f;
        float totalRange = 0f;
        var dmgVotes = new Dictionary<DamageTypeMask, float>(); // mask → total DPS weight
        int count = 0;

        for (int row = 0; row < _grid.Rows; row++)
        {
            var cell = _grid.GetCell(col, row);
            if (cell?.Structure == null) continue;

            // Read via TowerStatReader — which reads from the StatHolder
            var reader = cell.Structure.GetComponent<TowerStatReader>();
            if (reader == null) continue;

            count++;
            zone.TotalDPS += reader.TotalDPS;
            zone.HasAoE |= reader.HasAoE || reader.HasChain;
            zone.HasSlow |= reader.HasSlow;
            zone.HasPoison |= reader.HasPoison;
            zone.ArmorReduction += reader.ArmorPiercing;
            totalRange += reader.Range;
            totalHP += reader.CurrentHP;
            totalMaxHP += reader.MaxHP;

            // Accumulate damage type votes weighted by DPS
            AccumulateDamageVotes(dmgVotes, reader.ActiveDamageTypes, reader.TotalDPS);
        }

        zone.TowerCount = count;
        zone.AverageDPS = count > 0 ? zone.TotalDPS / count : 0f;
        zone.RangeScore = count > 0 ? totalRange / count : 0f;
        zone.AverageTowerHP = totalMaxHP > 0f ? totalHP / totalMaxHP : 1f;
        zone.HasHero = col == heroCol;
        zone.DominantDamageType = dmgVotes.Count > 0
            ? dmgVotes.OrderByDescending(kv => kv.Value).First().Key
            : DamageTypeMask.None;

        _survivalMemory.TryGetValue(col, out float historic);
        zone.HistoricSurvival = historic;

        zone.OpportunityScore = ComputeOpportunity(zone);
        return zone;
    }

    // ── Opportunity formula ───────────────────────────────────────────────────

    private float ComputeOpportunity(ZoneProfile z)
    {
        // Defense score: higher = harder for enemies to survive here
        float defense = z.TotalDPS;
        if (z.HasAoE) defense += 30f;
        if (z.HasSlow) defense += 20f;
        defense += z.ArmorReduction * 2f;
        defense += z.RangeScore * 5f;

        // Inverse sigmoid: 0 defense → ~1.0 opportunity; high defense → approaches 0
        float opportunity = 1f / (1f + defense * 0.01f);

        // Damaged towers are juicy targets
        if (z.TowerCount > 0 && z.AverageTowerHP < 0.5f)
            opportunity += _damagedBonus;

        // Historic survival: enemies lasted long here → keep attacking
        opportunity += _memoryWeight * Mathf.Clamp01(z.HistoricSurvival / 20f);

        // Hero is a strong deterrent
        if (z.HasHero) opportunity -= _heroPenalty;

        return Mathf.Max(0f, opportunity);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AccumulateDamageVotes(
        Dictionary<DamageTypeMask, float> votes,
        DamageTypeMask mask,
        float weight)
    {
        foreach (DamageTypeMask flag in System.Enum.GetValues(typeof(DamageTypeMask)))
        {
            if (flag == DamageTypeMask.None) continue;
            if ((mask & flag) != 0)
            {
                votes.TryGetValue(flag, out float v);
                votes[flag] = v + weight;
            }
        }
    }

    private static DamageTypeMask ComputeGlobalDominantDamage(List<ZoneProfile> zones)
    {
        var totals = new Dictionary<DamageTypeMask, float>();
        foreach (var z in zones)
        {
            if (z.DominantDamageType == DamageTypeMask.None) continue;
            totals.TryGetValue(z.DominantDamageType, out float v);
            totals[z.DominantDamageType] = v + z.TotalDPS;
        }
        return totals.Count > 0
            ? totals.OrderByDescending(kv => kv.Value).First().Key
            : DamageTypeMask.Physical;
    }

    private int GetHeroColumn()
    {
        if (_heroTransform == null || _grid == null) return -1;
        _grid.WorldToGrid(_heroTransform.position, out int col, out _);
        return col;
    }
}