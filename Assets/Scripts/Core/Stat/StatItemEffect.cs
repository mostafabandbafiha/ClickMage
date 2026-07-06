// StatItemEffect.cs
using ClickMage.Stats;
using UnityEngine;

namespace ClickMage.Items
{
    [CreateAssetMenu(
        fileName = "NewStatEffect",
        menuName = "ClickMage/Items/Effects/StatItemEffect")]
    public class StatItemEffect : ScriptableObject, IItemEffect
    {
        [SerializeField] private string targetStatKey;
        [SerializeField] private float modifierValue;
        [SerializeField] private StatModifierType modifierType = StatModifierType.Add;

        // Asset name is the stable source ID - same string used for both Add and Remove
        private string SourceID => name;

        public void Apply(IStatHolder target)
        {
            if (target == null) return;
            target.AddModifier(
                targetStatKey,
                new StatModifier(SourceID, modifierValue, modifierType));
        }

        public void Remove(IStatHolder target)
        {
            if (target == null) return;
            target.RemoveModifiersFromSource(SourceID);
        }
    }
}
