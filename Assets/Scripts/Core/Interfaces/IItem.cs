using System.Collections.Generic;
using UnityEngine;

namespace ClickMage.Items
{
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum SlotType { Any, Tool, Upgrade, Material, Special }

    public interface IItem
    {
        string ItemID { get; }
        string DisplayName { get; }
        string Description { get; }
        ItemRarity Rarity { get; }
        SlotType RequiredSlotType { get; }
        Sprite Icon { get; }

        IReadOnlyList<IItemEffect> PassiveEffects { get; }

        /// <summary>Null if item has no active ability.</summary>
        IActiveItemEffect ActiveEffect { get; }
    }


}
