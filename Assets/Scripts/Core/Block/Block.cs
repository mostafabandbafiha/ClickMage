using ClickMage.Entities;
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public abstract class Block : BaseEntity
{
    [Header("Block Definition")]
    [SerializeField] private StructureDefinition structureDefinition;

    [Header("Block Stats")]
    [SerializeField] private BaseStat health;
    [SerializeField] private BaseStat maxHealth;
    [SerializeField] private BaseStat defense;
    [SerializeField] private BaseStat maxEngagers;

    public StructureDefinition Definition => structureDefinition;
    public float Health => GetStatValue(CommonStats.Health);
    public float MaxHealth => GetStatValue(CommonStats.MaxHealth);
    public float Defense => GetStatValue(CommonStats.Defense);
    public bool IsAlive => Health > 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Called by EntityTargetable when this block dies.
    /// Override to spawn rubble, drop loot, etc.
    /// </summary>
    public virtual void OnBlockDestroyed()
    {
        // Without this, GridData kept showing this cell as occupied (with a stale
        // Structure reference) forever after the Block died — anything reading the
        // grid to decide where it can walk/what's in its way would be wrong.
        //BuildModeController.Instance?.FreeStructureFootprint(gameObject, OnFreeFootPrint);
        //Destroy(gameObject);
    }

    public void OnFreeFootPrint()
    {
        Destroy(gameObject);
    }

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        TryAdd(list, health);
        TryAdd(list, maxHealth);
        TryAdd(list, defense);
        TryAdd(list, maxEngagers);
        return list;
    }

    private static void TryAdd(List<BaseStat> list, BaseStat stat)
    {
        if (stat != null) list.Add(stat);
    }
}