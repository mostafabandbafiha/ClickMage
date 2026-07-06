// WorldPoint.cs
// Unified spatial marker for behavior tree destinations. Replaces the old
// BehaviorPoint (open wander/stand markers) and RestPoint (claimable seats)
// with one component: an "open" point (maxOccupants = 0) behaves like the old
// BehaviorPoint, a "claimable" point (maxOccupants > 0) behaves like the old
// RestPoint, including per-occupant seat offsets and social lookup.
using System.Collections.Generic;
using UnityEngine;

public enum WorldPointType
{
    Wander,
    Sit,
    Stand,
    Rest
}

public class WorldPoint : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private WorldPointType pointType = WorldPointType.Wander;

    [Header("Wander/marker settings")]
    [Tooltip("Used by open points (maxOccupants = 0): the random offset radius a character will pick within when heading here.")]
    [SerializeField] private float radius = 1f;

    [Header("Occupancy")]
    [Tooltip("0 = open marker, unlimited simultaneous use (old BehaviorPoint). >0 = claimable slot count (old RestPoint).")]
    [SerializeField] private int maxOccupants = 0;

    // Runtime occupant tracking — only meaningful when maxOccupants > 0.
    private readonly List<BaseCharacter> _occupants = new();

    public WorldPointType PointType => pointType;
    public Vector3 Position => transform.position;
    public float Radius => radius;
    public bool IsClaimable => maxOccupants > 0;
    public IReadOnlyList<BaseCharacter> Occupants => _occupants;

    /// <summary>Open points (maxOccupants == 0) are always "free" — they're not exclusive.</summary>
    public bool HasFreeSpot() => !IsClaimable || _occupants.Count < maxOccupants;

    /// <summary>Try to claim a spot. Always succeeds for open (non-claimable) points.</summary>
    public bool TryClaim(BaseCharacter character)
    {
        if (!IsClaimable) return true;
        if (!HasFreeSpot()) return false;
        if (_occupants.Contains(character)) return true; // already claimed
        _occupants.Add(character);
        return true;
    }

    /// <summary>Release a claimed spot when the character leaves. No-op for open points.</summary>
    public void Release(BaseCharacter character)
    {
        if (!IsClaimable) return;
        _occupants.Remove(character);
    }

    /// <summary>World position where the character should stand/sit. Spreads claimed
    /// occupants sideways so they don't overlap; falls back to a random offset within
    /// Radius for open points that don't track occupants.</summary>
    public Vector3 GetSitPosition(BaseCharacter character)
    {
        if (!IsClaimable)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            return transform.position + new Vector3(offset.x, 0, offset.y);
        }

        int index = _occupants.IndexOf(character);
        if (index < 0) return transform.position;

        Vector3 seatOffset = transform.right * (index - (maxOccupants - 1) * 0.5f) * 1.2f;
        return transform.position + seatOffset;
    }

    private void Start()
    {
        WorldPointManager.Instance?.RegisterPoint(this);
    }

    private void OnDestroy()
    {
        WorldPointManager.Instance?.UnregisterPoint(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = pointType switch
        {
            WorldPointType.Sit => Color.blue,
            WorldPointType.Stand => Color.green,
            WorldPointType.Rest => Color.red,
            _ => Color.yellow
        };
        Gizmos.DrawWireSphere(transform.position, IsClaimable ? 1f : radius);
        if (IsClaimable)
            Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "console.infoicon", true);
    }
}