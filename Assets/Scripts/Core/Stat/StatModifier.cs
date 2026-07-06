using System;
using UnityEngine;

namespace ClickMage.Stats
{
    public enum StatModifierType { Add, Multiply, Override }

    public readonly struct StatModifier
    {
        public readonly string Source;
        public readonly float Value;
        public readonly StatModifierType Type;

        public StatModifier(string source, float value, StatModifierType type = StatModifierType.Add)
        {
            Source = source;
            Value = value;
            Type = type;
        }
    }

}
