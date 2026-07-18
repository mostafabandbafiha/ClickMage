// TargetRegistry.cs — skip invisible targets in both queries
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class TargetRegistry : MonoBehaviour
{
    public static TargetRegistry Instance { get; private set; }
    private readonly Dictionary<Faction, HashSet<Targetable>> _registry = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(Targetable t)
    {
        if (!_registry.ContainsKey(t.Faction))
            _registry[t.Faction] = new HashSet<Targetable>();
        _registry[t.Faction].Add(t);
    }

    public void Unregister(Targetable t)
    {
        if (_registry.TryGetValue(t.Faction, out var set))
            set.Remove(t);
    }

    public IReadOnlyCollection<Targetable> GetTargets(Faction faction)
    {
        return _registry.TryGetValue(faction, out var set)
            ? set
            : System.Array.Empty<Targetable>();
    }

    public Targetable GetNearest(Faction faction, Vector3 worldPos)
    {
        if (!_registry.TryGetValue(faction, out var set)) return null;
        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        foreach (var t in set)
        {
            if (t == null || !t.IsAlive) continue;
            if (IsInvisible(t)) continue;
            float sq = (t.transform.position - worldPos).sqrMagnitude;
            if (sq < nearestSq) { nearestSq = sq; nearest = t; }
        }
        return nearest;
    }

    public Targetable GetNearestInRange(Faction faction, Vector3 worldPos, float range)
    {
        if (!_registry.TryGetValue(faction, out var set)) return null;

        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        float rangeSq = range * range;

        foreach (var t in set)
        {
            if (t == null || !t.IsAlive) continue;
            if (IsInvisible(t)) continue;

            float sq = (t.transform.position - worldPos).sqrMagnitude;
            if (sq > rangeSq) continue;
            if (sq < nearestSq)
            {
                nearestSq = sq;
                nearest = t;
            }
        }

        return nearest;
    }

    public Targetable GetNearestEngageable(Faction faction, Vector3 worldPos)
    {
        if (!_registry.TryGetValue(faction, out var set)) return null;
        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        foreach (var t in set)
        {
            if (t == null || !t.IsAlive) continue;
            if (IsInvisible(t)) continue;
            if (!t.HasCapacity) continue; // skip full targets

            float sq = (t.transform.position - worldPos).sqrMagnitude;
            if (sq < nearestSq) { nearestSq = sq; nearest = t; }
        }
        return nearest;
    }

    public Targetable GetNearestEngageableInRange(Faction faction, Vector3 worldPos, float range)
    {
        if (!_registry.TryGetValue(faction, out var set)) return null;
        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        float rangeSq = range * range;
        foreach (var t in set)
        {
            if (t == null || !t.IsAlive) continue;
            if (IsInvisible(t)) continue;
            if (!t.HasCapacity) continue;

            float sq = (t.transform.position - worldPos).sqrMagnitude;
            if (sq > rangeSq) continue;
            if (sq < nearestSq) { nearestSq = sq; nearest = t; }
        }
        return nearest;
    }

    public bool HasLivingStructures(Faction faction)
    {
        if (!_registry.TryGetValue(faction, out var set)) return false;
        foreach (var t in set)
        {
            if (t == null || !t.IsAlive) continue;
            if (t.GetComponent<CombatCharacter>() != null) continue; // heroes/characters don't count
            return true;
        }
        return false;
    }

    public bool HasLivingTargets(Faction faction)
    {
        if (!_registry.TryGetValue(faction, out var set)) return false;
        foreach (var t in set)
            if (t != null && t.IsAlive) return true;
        return false;
    }

    private bool IsInvisible(Targetable t)
    {
        if (t is EntityTargetable et)
            return et.GetStatValue(CommonStats.Invisibility) > 0f;
        return false;
    }
}