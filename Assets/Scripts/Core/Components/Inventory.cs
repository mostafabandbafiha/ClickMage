using System;
using System.Collections.Generic;
using UnityEngine;

public enum OwnerType
{
    Player,
    NPC,
    Factory,
    Storage,   // chests, generic containers
}

/// <summary>
/// General-purpose fixed-size inventory component.
/// Attach to: Player, Chest, Entity, anything that stores items.
/// </summary>
public class Inventory : MonoBehaviour
{   
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Settings")]
    [Min(1)] public int SlotCount = 20;

    [Header("Starting Items (optional)")]
    [SerializeField] private List<ItemStack> _startingItems = new();

    [Header("Owner")]
    [SerializeField] private OwnerType _ownerType = OwnerType.Player;
    public OwnerType Owner => _ownerType;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<ItemSlot> OnSlotChanged;
    public event Action OnInventoryChanged;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<ItemSlot> _slots = new();

    public IReadOnlyList<ItemSlot> Slots => _slots;

    public event System.Action OnChanged;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Only auto-init if not driven externally (e.g. by BaseEntity)
        if (_slots.Count == 0)
            InitSlots(SlotCount);
    }

    /// <summary>
    /// Public re-initialise. Call after changing SlotCount at runtime.
    /// WARNING: destroys existing contents.
    /// </summary>
    public void InitSlots()
    {
        InitSlots(SlotCount);
    }

    // rename the existing private method signature to match:
    private void InitSlots(int count)
    {
        _slots.Clear();
        for (int i = 0; i < count; i++)
        {
            var slot = new ItemSlot { SlotIndex = i };
            slot.OnChanged += s => { OnSlotChanged?.Invoke(s); OnInventoryChanged?.Invoke(); };
            _slots.Add(slot);
        }

        // Apply inspector-assigned starting items
        foreach (var stack in _startingItems)
        {
            if (stack.IsEmpty) continue;
            AddItem(stack);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Add a stack anywhere it fits (merges with existing stacks first, then fills empty slots).
    /// Returns leftover that did not fit.
    /// </summary>
    public ItemStack AddItem(ItemStack incoming)
    {
        if (incoming.IsEmpty) return ItemStack.Empty;
        int cap = incoming.Data.GetMaxStack(_ownerType);

        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.Item == incoming.Data)
            {
                incoming = slot.Add(incoming, cap);
                if (incoming.IsEmpty) return ItemStack.Empty;
            }
        }

        foreach (var slot in _slots)
        {
            if (slot.IsEmpty)
            {
                incoming = slot.Add(incoming, cap);
                if (incoming.IsEmpty) return ItemStack.Empty;
            }
        }

        OnChanged?.Invoke();
        return incoming;
    }

    public ItemStack AddToSlot(ItemSlot slot, ItemStack incoming)
    {
        if (slot == null || incoming.IsEmpty) return incoming;

        int cap = incoming.Data.GetMaxStack(_ownerType);
        var leftover = slot.Add(incoming, cap);

        OnChanged?.Invoke();
        return leftover;
    }

    /// <summary>
    /// Remove a specific amount of an item from anywhere in the inventory.
    /// Returns how many were actually removed.
    /// </summary>
    public int RemoveItem(ItemData item, int count)
    {
        int remaining = count;
        foreach (var slot in _slots)
        {
            if (remaining <= 0) break;
            if (slot.IsEmpty || slot.Item != item) continue;

            var removed = slot.Remove(remaining);
            remaining -= removed.Amount;
        }
        OnChanged?.Invoke();
        return count - remaining;
    }

    /// <summary>Total count of a given item across all slots.</summary>
    public int CountItem(ItemData item)
    {
        int total = 0;
        foreach (var slot in _slots)
            if (!slot.IsEmpty && slot.Item == item)
                total += slot.Amount;
        return total;
    }

    public bool HasItem(ItemData item, int amount = 1) => CountItem(item) >= amount;

    public ItemSlot GetSlot(int index) =>
        (index >= 0 && index < _slots.Count) ? _slots[index] : null;


}
