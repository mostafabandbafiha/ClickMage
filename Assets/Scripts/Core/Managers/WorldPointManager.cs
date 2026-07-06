// WorldPointManager.cs
// Single registry for all WorldPoints, replacing the separate
// BehaviorPointManager (wander/stand markers) and RestPointManager (claimable seats).
using System.Collections.Generic;
using UnityEngine;

public class WorldPointManager : MonoBehaviour
{
    public static WorldPointManager Instance { get; private set; }

    private readonly List<WorldPoint> _allPoints = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPoint(WorldPoint point)
    {
        if (!_allPoints.Contains(point))
            _allPoints.Add(point);
    }

    public void UnregisterPoint(WorldPoint point)
    {
        _allPoints.Remove(point);
    }

    /// <summary>Random point within maxDistance of nearPosition, optionally filtered by type.
    /// Does NOT check occupancy — used for open/wander destinations where multiple
    /// characters can head to the same marker.</summary>
    public WorldPoint GetRandomPoint(Vector3 nearPosition, float maxDistance = 50f, WorldPointType? type = null)
    {
        var candidates = new List<WorldPoint>();

        foreach (var point in _allPoints)
        {
            if (point == null) continue;
            if (type.HasValue && point.PointType != type.Value) continue;

            float dist = Vector3.Distance(nearPosition, point.Position);
            if (dist <= maxDistance)
                candidates.Add(point);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    /// <summary>Nearest point within maxDistance that still has a free (claimable) spot,
    /// optionally filtered by type. Used for rest/seating destinations.</summary>
    public WorldPoint GetNearestAvailable(Vector3 position, float maxDistance = float.MaxValue, WorldPointType? type = null)
    {
        WorldPoint best = null;
        float minDist = float.MaxValue;

        foreach (var point in _allPoints)
        {
            if (point == null || !point.HasFreeSpot()) continue;
            if (type.HasValue && point.PointType != type.Value) continue;

            float dist = Vector3.Distance(position, point.Position);
            if (dist > maxDistance) continue;
            if (dist < minDist)
            {
                minDist = dist;
                best = point;
            }
        }

        return best;
    }

    /// <summary>All registered points, for debugging or UI.</summary>
    public IReadOnlyList<WorldPoint> AllPoints => _allPoints;
}