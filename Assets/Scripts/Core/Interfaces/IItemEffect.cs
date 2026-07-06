// IItemEffect.cs
using ClickMage.Stats;

namespace ClickMage.Items
{
    /// <summary>
    /// Passive effect - applied on equip, removed on unequip.
    /// Implemented as ScriptableObject assets.
    /// </summary>
    public interface IItemEffect
    {
        void Apply(IStatHolder target);
        void Remove(IStatHolder target);
    }
}
