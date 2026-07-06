// AuraItemEffect.cs
using ClickMage.Stats;
using ClickMage.Items;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAuraEffect", menuName = "ClickMage/Items/Effects/AuraItemEffect")]
public class AuraItemEffect : StatItemEffect
{
    [Header("Aura")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private Faction targetFaction = Faction.Enemy;
    [SerializeField] private List<AuraEffect> effects = new();

    public float Radius => radius;
    public Faction TargetFaction => targetFaction;
    public IReadOnlyList<AuraEffect> Effects => effects;

    // Called on equip — nothing to apply to self
    public void Apply(IStatHolder target) { }

    // Called on unequip — nothing to remove from self
    public void Remove(IStatHolder target) { }
}