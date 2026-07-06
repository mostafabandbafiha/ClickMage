using System.Collections.Generic;
using UnityEngine;

public class RestPoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxOccupants = 2;

    // Runtime occupant tracking
    private List<BaseCharacter> _occupants = new();

    public bool HasFreeSpot() => _occupants.Count < maxOccupants;
    public IReadOnlyList<BaseCharacter> Occupants => _occupants;

    /// <summary>Try to claim a spot. Returns true if successful.</summary>
    public bool TryClaim(BaseCharacter character)
    {
        if (!HasFreeSpot()) return false;
        if (_occupants.Contains(character)) return true; // already claimed
        _occupants.Add(character);
        return true;
    }

    /// <summary>Release a spot when character leaves.</summary>
    public void Release(BaseCharacter character)
    {
        _occupants.Remove(character);
    }

    /// <summary>Get the world position where the character should stand/sit.</summary>
    public Vector3 GetSitPosition(BaseCharacter character)
    {
        // Spread occupants slightly so they don't overlap
        int index = _occupants.IndexOf(character);
        if (index < 0) return transform.position;

        // Offset each occupant sideways from the rest point
        Vector3 offset = transform.right * (index - (maxOccupants - 1) * 0.5f) * 1.2f;
        return transform.position + offset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = HasFreeSpot() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "console.infoicon", true);
    }
}