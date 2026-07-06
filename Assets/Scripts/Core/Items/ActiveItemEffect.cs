// ActiveItemEffect.cs
using System.Collections.Generic;
using ClickMage.Stats;
using UnityEngine;

namespace ClickMage.Items
{
    /// <summary>
    /// ScriptableObject that defines WHAT an active effect does.
    /// All runtime state lives in ItemSlot - this asset is stateless.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewActiveEffect",
        menuName = "ClickMage/Items/Effects/ActiveItemEffect")]
    public class ActiveItemEffect : ScriptableObject, IActiveItemEffect
    {
        [Header("Activation")]
        [SerializeField] private float baseCooldown = 10f;
        [SerializeField] private int maxCharges = -1; // -1 = unlimited

        [Header("Stat Modifiers Applied on Activation")]
        [SerializeField] private string effectSourceID;
        [SerializeField] private List<StatEffectEntry> activationEffects = new List<StatEffectEntry>();

        [Header("Feedback")]
        [SerializeField] private GameObject activationVFX;

        public float BaseCooldown => baseCooldown;
        public int MaxCharges => maxCharges;

        public bool CanActivate(IStatHolder target, float cooldownRemaining, int currentCharges)
        {
            if (target == null) return false;
            if (cooldownRemaining > 0f) return false;
            if (maxCharges >= 0 && currentCharges <= 0) return false;
            return true;
        }

        public void Activate(IStatHolder target)
        {
            if (target == null) return;

            // Apply each stat modifier - source ID lets us remove them later
            string source = string.IsNullOrEmpty(effectSourceID) ? name : effectSourceID;

            foreach (var entry in activationEffects)
            {
                target.AddModifier(
                    entry.StatKey,
                    new StatModifier(source, entry.Value, entry.ModifierType));
            }

            // Spawn VFX if we have a position context
            // VFX is spawned by caller if needed - kept simple here
        }

        /// <summary>
        /// Removes any lingering modifiers this effect applied (for timed buffs).
        /// </summary>
        public void RemoveEffects(IStatHolder target)
        {
            string source = string.IsNullOrEmpty(effectSourceID) ? name : effectSourceID;
            target.RemoveModifiersFromSource(source);
        }

        [System.Serializable]
        public struct StatEffectEntry
        {
            public string StatKey;
            public float Value;
            public StatModifierType ModifierType;
        }
    }
}
