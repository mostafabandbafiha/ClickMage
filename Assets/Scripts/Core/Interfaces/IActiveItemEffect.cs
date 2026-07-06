// IActiveItemEffect.cs
using ClickMage.Stats;

namespace ClickMage.Items
{
    /// <summary>
    /// Definition of an active effect.
    /// Runtime state (cooldown, charges) is owned by ItemSlot - NOT here.
    /// </summary>
    public interface IActiveItemEffect
    {
        float BaseCooldown { get; }

        /// <summary>-1 = unlimited</summary>
        int MaxCharges { get; }

        bool CanActivate(IStatHolder target, float cooldownRemaining, int currentCharges);
        void Activate(IStatHolder target);

    }
}
