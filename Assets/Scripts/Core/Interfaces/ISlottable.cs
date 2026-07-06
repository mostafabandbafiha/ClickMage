
using System.Collections.Generic;

namespace ClickMage.Items
{
    public interface ISlottable
    {
        int MaxSlots { get; }
        IReadOnlyList<IItemSlot> Slots { get; }

        bool CanEquipItem(IItem item);
        bool EquipItem(IItem item, int slotIndex);
        ItemStack UnequipItem(int slotIndex);
    }
}
