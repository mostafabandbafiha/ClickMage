using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RestPointManager : MonoBehaviour
{
    public static RestPointManager Instance { get; private set; }

    [Header("Rest Points")]
    [SerializeField] private List<RestPoint> _restPoints = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>Returns the nearest available rest point to a position, or null if none.
    /// Pass maxDistance to restrict the search (e.g. to a character's activity radius)
    /// so units don't trek across the map to rest.</summary>
    public RestPoint GetNearestAvailable(Vector3 position, float maxDistance = float.MaxValue)
    {
        RestPoint best = null;
        float minDist = float.MaxValue;

        foreach (var rp in _restPoints)
        {
            if (rp == null || !rp.HasFreeSpot()) continue;
            float dist = Vector3.Distance(position, rp.transform.position);
            if (dist > maxDistance) continue;
            if (dist < minDist)
            {
                minDist = dist;
                best = rp;
            }
        }

        return best;
    }

    /// <summary>All rest points, for debugging or UI.</summary>
    public IReadOnlyList<RestPoint> AllRestPoints => _restPoints;
}