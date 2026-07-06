// StatusEffectHandler.cs
using System.Collections.Generic;
using UnityEngine;
using ClickMage.Entities;
using ClickMage.Stats;

public class StatusEffectHandler : MonoBehaviour
{
    private readonly Dictionary<string, StatusEffectInstance> _active = new();
    private Targetable _targetable;
    private BaseEntity _entity; // for stat-modifier effects like armor shred

    private void Awake()
    {
        _targetable = GetComponent<Targetable>();
        _entity = GetComponent<BaseEntity>();
    }

    public void Apply(StatusEffectInstance incoming)
    {
        string key = incoming.EffectId;

        if (_active.TryGetValue(key, out var existing))
        {
            existing.TimeRemaining = incoming.TimeRemaining;

            if (existing.Stacks < existing.MaxStacks)
            {
                existing.Stacks++;
                if (existing.IsStatModifierEffect)
                    ReapplyStatModifier(existing);
            }
        }
        else
        {
            _active[key] = incoming;
            if (incoming.IsStatModifierEffect)
                ReapplyStatModifier(incoming);
        }
    }

    private void ReapplyStatModifier(StatusEffectInstance fx)
    {
        if (_entity == null) return;
        _entity.RemoveStatModifiersFromSource(fx.SourceId);
        _entity.AddStatModifier(
            fx.ModifiedStatKey,
            new StatModifier(fx.SourceId, fx.ModifierValuePerStack * fx.Stacks, StatModifierType.Add));
    }

    private void Update()
    {
        if (_active.Count == 0) return;

        List<string> toRemove = null;

        foreach (var kvp in _active)
        {
            var fx = kvp.Value;
            fx.TimeRemaining -= Time.deltaTime;

            if (!fx.IsStatModifierEffect)
            {
                fx.TickTimer -= Time.deltaTime;
                if (fx.TickTimer <= 0f)
                {
                    fx.TickTimer += fx.TickInterval;
                    var dmgType = fx.EffectId == "fire" ? DamageType.Fire
                                : fx.EffectId == "bleed" ? DamageType.Bleed
                                : DamageType.Normal;
                    _targetable?.TakeDamage(fx.DamagePerTick, null, dmgType);
                }
            }

            if (fx.TimeRemaining <= 0f)
            {
                (toRemove ??= new List<string>()).Add(kvp.Key);
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                var fx = _active[key];
                if (fx.IsStatModifierEffect)
                    _entity?.RemoveStatModifiersFromSource(fx.SourceId);
                _active.Remove(key);
            }
        }
    }

    public void ClearAll()
    {
        if (_active.Count == 0) return;
        foreach (var fx in _active.Values)
            if (fx.IsStatModifierEffect) _entity?.RemoveStatModifiersFromSource(fx.SourceId);
        _active.Clear();
    }
}