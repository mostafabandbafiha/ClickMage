// BaseStat.cs
using System.Collections.Generic;
using UnityEngine;

namespace ClickMage.Stats
{
    [CreateAssetMenu(fileName = "NewStat", menuName = "ClickMage/Stats/BaseStat")]
    public class BaseStat : ScriptableObject
    {
        [SerializeField] private string statKey;
        [SerializeField] private float baseValue;

        private readonly List<StatModifier> modifiers = new List<StatModifier>();

        public string StatKey => statKey;
        public float BaseValue
        {
            get => baseValue;
            set => baseValue = value;
        }

        public float GetValue()
        {
            float final = baseValue;
            float additive = 0f;
            float multiplicative = 1f;

            foreach (var mod in modifiers)
            {
                switch (mod.Type)
                {
                    case StatModifierType.Add:
                        additive += mod.Value;
                        break;
                    case StatModifierType.Multiply:
                        // Stacks multiplicatively: 1.1 * 1.2 = 1.32 (not 1.3)
                        multiplicative *= mod.Value;
                        break;
                    case StatModifierType.Override:
                        final = mod.Value;
                        additive = 0f;
                        multiplicative = 1f;
                        break;
                }
            }

            return (final + additive) * multiplicative;
        }

        public void AddModifier(StatModifier modifier) =>
            modifiers.Add(modifier);

        public void RemoveModifier(StatModifier modifier) =>
            modifiers.RemoveAll(m =>
                m.Source == modifier.Source &&
                m.Type == modifier.Type);

        public void RemoveModifiersFromSource(string source) =>
            modifiers.RemoveAll(m => m.Source == source);

        public void ClearModifiers() => modifiers.Clear();

        public static BaseStat CreateRuntime(string key, float baseVal = 0f)
        {
            var stat = CreateInstance<BaseStat>();
            stat.statKey = key;
            stat.baseValue = baseVal;
            return stat;
        }

        public BaseStat Clone()
        {
            var clone = CreateInstance<BaseStat>();
            clone.statKey = statKey;
            clone.baseValue = baseValue;
            // Modifiers are NOT cloned - runtime state only
            return clone;
        }
    }
}
