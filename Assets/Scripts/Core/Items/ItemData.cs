using ClickMage.Items;
using System;
using System.Collections.Generic;
using UnityEngine;


// ItemTag.cs
public enum ItemTag
{
    None,
    Fruit,
    Ore,
    Wood,
    Food,
    Ingredient,
    Equipment,
}

[System.Flags]
public enum DamageType
{
    Normal = 0,
    Fire = 1 << 0,
    Frost = 1 << 1,
    Lightning = 1 << 2,
    Bleed = 1 << 3,
    Poison = 1 << 4,
    Reflect = 1 << 5,
}

[Serializable]
public struct StackRule
{
    public OwnerType Owner;
    public int MaxStack;      // use -1 to mean "infinite"
}

/// <summary>
/// ScriptableObject definition for every item in the game.
/// Create via: Assets > Create > Inventory > Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────
    [Header("Identity")]
    [SerializeField] private string itemID;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField] private ItemRarity rarity = ItemRarity.Common;
    [SerializeField] private SlotType requiredSlotType = SlotType.Any; // kept for filter use
    [SerializeField] private Sprite icon;

    // ── Effects ───────────────────────────────────────────────────────────
    [Header("Effects")]
    [SerializeField] private List<StatItemEffect> passiveEffects = new();
    [SerializeField] private ActiveItemEffect activeEffect;

    // ── Inventory / World (NEW) ────────────────────────────────────────────
    [Header("Inventory")]
    [SerializeField] private int maxStackSize = 99;
    [SerializeField] private GameObject worldPrefab;

    [Header("Gathering")]
    [SerializeField] private List<ItemTag> _tags = new();

    [Header("World Visual")]
    [SerializeField] private GameObject _worldVisualPrefab;


    [Header("Stacking")]
    [SerializeField] private int _defaultMaxStack = 99;
    [SerializeField] private List<StackRule> _stackRules = new();


    // ── IItem interface ───────────────────────────────────────────────────
    public string ItemID => itemID;
    public string DisplayName => displayName;
    public string Description => description;
    public ItemRarity Rarity => rarity;
    public SlotType RequiredSlotType => requiredSlotType;
    public Sprite Icon => icon;
    public IReadOnlyList<IItemEffect> PassiveEffects => passiveEffects;
    public IActiveItemEffect ActiveEffect => activeEffect;

    // ── New properties ────────────────────────────────────────────────────
    public int MaxStackSize => maxStackSize;
    public GameObject WorldPrefab => worldPrefab;
    public IReadOnlyList<ItemTag> Tags => _tags;

    public bool HasTag(ItemTag tag) => _tags.Contains(tag);
    public GameObject WorldVisualPrefab => _worldVisualPrefab;

    /// <summary>Resolves the max stack size for this item when held by a given owner type.</summary>
    public int GetMaxStack(OwnerType owner)
    {
        foreach (var rule in _stackRules)
            if (rule.Owner == owner)
                return rule.MaxStack < 0 ? int.MaxValue : rule.MaxStack;

        return Mathf.Max(1, _defaultMaxStack);
    }

    /// <summary>For UI/tooltips — full breakdown of how this item stacks per owner type.</summary>
    public IEnumerable<(OwnerType Owner, int Max)> GetAllStackRules()
    {
        foreach (OwnerType owner in Enum.GetValues(typeof(OwnerType)))
            yield return (owner, GetMaxStack(owner));
    }

}
