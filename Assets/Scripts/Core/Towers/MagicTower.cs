using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class MagicTower : Tower
{
    [Header("Magic Tower Stats")]
    [SerializeField] private BaseStat _chainCountStat;
    [SerializeField] private BaseStat _chainRangeStat;
    [SerializeField] private BaseStat _chainDamageMultiplierStat;

    // How many additional targets the bolt chains to (beyond primary)
    public int ChainCount => HasStat(CommonStats.ChainCount)
        ? Mathf.RoundToInt(GetStatValue(CommonStats.ChainCount)) : 1;

    // How far the chain can jump between targets
    public float ChainRange => HasStat(CommonStats.ChainRange)
        ? GetStatValue(CommonStats.ChainRange) : 5f;

    // Damage multiplier per chain hop (0.5 = 50% damage per hop)
    public float ChainDamageMultiplier => HasStat(CommonStats.ChainDamageMultiplier)
        ? GetStatValue(CommonStats.ChainDamageMultiplier) : 0.6f;

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        if (_chainCountStat != null) list.Add(_chainCountStat);
        if (_chainRangeStat != null) list.Add(_chainRangeStat);
        if (_chainDamageMultiplierStat != null) list.Add(_chainDamageMultiplierStat);
        return list;
    }

    public override void Fire(Targetable target)
    {
        if (target == null || !target.IsAlive) return;

        lastFireTime = Time.time;

        List<Targetable> chainTargets = BuildChainTargetList(target);

        if (towerData.projectilePrefab != null)
        {
            GameObject obj = Instantiate(
                towerData.projectilePrefab,
                FirePoint.position,
                FirePoint.rotation);

            LightningProjectile lightning = obj.GetComponent<LightningProjectile>();
            if (lightning != null)
            {
                lightning.Initialize(
                    FirePoint.position,
                    chainTargets,
                    Damage,
                    ChainDamageMultiplier,
                    this);
            }
        }

        if (towerData.shootSound != null)
            AudioSource.PlayClipAtPoint(towerData.shootSound, transform.position);
    }
    /// <summary>
    /// Builds the ordered list of targets the bolt will chain through.
    /// Starts from primary, then greedily picks nearest unchained target.
    /// </summary>
    private List<Targetable> BuildChainTargetList(Targetable primary)
    {
        var result = new List<Targetable> { primary };
        var used = new HashSet<Targetable> { primary };

        for (int i = 0; i < ChainCount; i++)
        {
            var last = result[result.Count - 1];
            var next = FindNearestUnchained(last, used);
            if (next == null) break;

            result.Add(next);
            used.Add(next);
        }

        return result;
    }

    private Targetable FindNearestUnchained(Targetable from, HashSet<Targetable> used)
    {
        var hits = Physics.OverlapSphere(from.Position, ChainRange);

        Targetable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var t = hit.GetComponent<Targetable>();
            if (t == null || !t.IsAlive || used.Contains(t)) continue;

            float dist = Vector3.Distance(from.Position, t.Position);
            if (dist < nearestDist)
            {
                nearest = t;
                nearestDist = dist;
            }
        }

        return nearest;
    }
}