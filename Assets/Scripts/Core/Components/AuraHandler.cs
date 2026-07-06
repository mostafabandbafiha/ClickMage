// AuraHandler.cs — component added automatically by BaseEntity if needed
using ClickMage.Entities;
using ClickMage.Items;
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class AuraHandler : MonoBehaviour
{
    private Inventory _inventory;
    private string _sourceID;

    // Tracks which entities are currently buffed by which effect
    private readonly Dictionary<AuraItemEffect, HashSet<IStatHolder>> _buffed = new();

    private float _tickTimer = 0f;
    private const float TickInterval = 1f;

    public void Initialize(Inventory inventory, string sourceID)
    {
        _inventory = inventory;
        _sourceID = sourceID;
    }

    private void Update()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer < TickInterval) return;
        _tickTimer = 0f;

        TickAllAuras();
    }

    private void TickAllAuras()
    {
        if (_inventory == null || TargetRegistry.Instance == null) return;

        foreach (var slot in _inventory.Slots)
        {
            if (slot.IsEmpty) continue;

            foreach (var passive in slot.Item.PassiveEffects)
            {
                if (passive is not AuraItemEffect aura) continue;
                TickAura(aura);
            }
        }
    }

    private void TickAura(AuraItemEffect aura)
    {
        if (!_buffed.ContainsKey(aura))
            _buffed[aura] = new HashSet<IStatHolder>();

        float radiusSq = aura.Radius * aura.Radius;
        var currentlyBuffed = _buffed[aura];
        var inRange = new HashSet<IStatHolder>();

        foreach (var t in TargetRegistry.Instance.GetTargets(aura.TargetFaction))
        {
            if (t == null || !t.IsAlive) continue;

            float distSq = (t.transform.position - transform.position).sqrMagnitude;
            if (distSq > radiusSq) continue;

            var statHolder = t.GetComponent<BaseEntity>();
            if (statHolder == null) continue;

            inRange.Add(statHolder.StatHolder);
        }

        // Apply to newly in-range
        foreach (var holder in inRange)
        {
            if (currentlyBuffed.Contains(holder)) continue;
            ApplyAura(aura, holder);
            currentlyBuffed.Add(holder);
        }

        // Remove from out-of-range
        var toRemove = new List<IStatHolder>();
        foreach (var holder in currentlyBuffed)
        {
            if (holder == null || !inRange.Contains(holder))
                toRemove.Add(holder);
        }

        foreach (var holder in toRemove)
        {
            RemoveAura(holder);
            currentlyBuffed.Remove(holder);
        }
    }

    private void ApplyAura(AuraItemEffect aura, IStatHolder holder)
    {
        foreach (var effect in aura.Effects)
            holder.AddModifier(effect.StatKey,
                new StatModifier(_sourceID, effect.Value, effect.ModifierType));
    }

    private void RemoveAura(IStatHolder holder)
    {
        if (holder == null) return;
        holder.RemoveModifiersFromSource(_sourceID);
    }

    public void RemoveAllAuras()
    {
        foreach (var buffedSet in _buffed.Values)
            foreach (var holder in buffedSet)
                RemoveAura(holder);

        _buffed.Clear();
    }

    private void OnDisable()
    {
        RemoveAllAuras();
    }
}