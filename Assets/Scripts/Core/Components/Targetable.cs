using ClickMage.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Faction { Player, Enemy, Neutral }

/// <summary>
/// Base for anything that can be targeted and attacked.
/// Auto-registers/unregisters with TargetRegistry so enemies can find it
/// regardless of whether it was placed in the editor or spawned at runtime.
/// </summary>
public abstract class Targetable : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 4;
    [SerializeField] private Faction faction = Faction.Neutral;

    private readonly HashSet<GameObject> _engagers = new();
    private readonly Dictionary<GameObject, Vector3> _claimedPositions = new();

    public Vector3 Position => transform.position;
    public bool IsAlive { get; protected set; } = true;
    public bool HasCapacity => _engagers.Count < MaxCapacity;
    public virtual int MaxCapacity => maxCapacity;
    public IReadOnlyCollection<GameObject> Engagers => _engagers;
    public IEnumerable<Vector3> ClaimedPositions => _claimedPositions.Values;

    public Faction Faction
    {
        get => faction;
        set
        {
            // Re-register under the new faction if already alive.
            if (IsAlive && TargetRegistry.Instance != null)
            {
                TargetRegistry.Instance.Unregister(this);
                faction = value;
                TargetRegistry.Instance.Register(this);
            }
            else
            {
                faction = value;
            }
        }
    }

    public event System.Action<float, BaseEntity, DamageType> OnDamageTaken;

    protected void RaiseDamageTaken(float amount, BaseEntity attacker, DamageType type) =>
        OnDamageTaken?.Invoke(amount, attacker, type);

    public event Action<Targetable> OnDied;

    // ── Auto-registration ─────────────────────────────────────────────────

    protected virtual void OnEnable()
    {
        // OnEnable fires both on first activation and after SetActive(true),
        // so it covers editor-placed objects, spawned objects, and re-activated objects.
        if (TargetRegistry.Instance != null)
            TargetRegistry.Instance.Register(this);
    }

    protected virtual void OnDisable()
    {
        if (TargetRegistry.Instance != null)
            TargetRegistry.Instance.Unregister(this);
    }

    // ── Engagement ────────────────────────────────────────────────────────

    public bool TryEngage(GameObject attacker)
    {
        if (!IsAlive) return false;
        if (_engagers.Contains(attacker)) return true;
        if (!HasCapacity) return false;
        _engagers.Add(attacker);
        return true;
    }

    public void Disengage(GameObject attacker)
    {
        _engagers.Remove(attacker);
        _claimedPositions.Remove(attacker);
    }

    // ── Position claiming ─────────────────────────────────────────────────

    public void ClaimPosition(GameObject attacker, Vector3 position)
    {
        if (_engagers.Contains(attacker))
            _claimedPositions[attacker] = position;
    }

    // ── Damage ────────────────────────────────────────────────────────────
    public abstract void TakeDamage(float damage, BaseEntity attacker = null, DamageType type = DamageType.Normal);

    // ── Death ─────────────────────────────────────────────────────────────
    protected virtual void OnDeath()
    {
        if (!IsAlive) return;
        IsAlive = false;
        OnDied?.Invoke(this);
        _engagers.Clear();
        _claimedPositions.Clear();
        // Unregister immediately so enemies stop targeting this.
        if (TargetRegistry.Instance != null)
            TargetRegistry.Instance.Unregister(this);
    }

    protected void SetMaxCapacity(int capacity) => maxCapacity = capacity;
}