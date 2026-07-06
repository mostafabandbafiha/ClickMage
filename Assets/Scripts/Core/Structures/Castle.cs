// Castle.cs
// The core structure enemies fall back to attacking when nothing closer is
// available. Requires an EntityTargetable component on the same GameObject,
// set to Faction.Player. CombatCharacter.FindNearestTarget() has no range
// limit (TargetRegistry.GetNearest), so once the Castle is alive and
// registered, AttackSeekBehaviorNode (and friends) will always find *some*
// target to march toward — enemies never freeze with "nothing to do" as long
// as the Castle stands.
using ClickMage.Entities;
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class Castle : BaseEntity
{
    public static Castle Instance { get; private set; }

    [Header("Stats")]
    [SerializeField] private BaseStat health;
    [SerializeField] private BaseStat maxHealth;

    public float Health => GetStatValue(CommonStats.Health);
    public float MaxHealth => GetStatValue(CommonStats.MaxHealth);

    /// <summary>Fired once, when the Castle's EntityTargetable reports OnDeath.
    /// Hook your lose-condition / game-over flow here.</summary>
    public event System.Action OnCastleFell;

    protected override void Awake()
    {
        base.Awake();
    }

    protected virtual void Start()
    {
        // Deliberately not in Awake(): BuildModeController's SpawnGhost() instantiates
        // this exact prefab as a drag preview and disables its scripts synchronously
        // right after Instantiate() — but Awake() already runs DURING Instantiate(),
        // before that disable happens. Unity skips Start() entirely for a component
        // disabled before its first active frame, so putting the singleton claim here
        // means a ghost preview never claims Instance — only a real, left-enabled,
        // actually-placed Castle does, on its first frame.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        if (health != null) list.Add(health);
        if (maxHealth != null) list.Add(maxHealth);
        return list;
    }

    /// <summary>Called by EntityTargetable.OnDeath(). Not meant to be called directly.</summary>
    public void NotifyFell() => OnCastleFell?.Invoke();
}