using System;
using System.Collections.Generic;
using UnityEngine;

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

        // Pass 1: merge into existing stacks of same type
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.Item == incoming.Data)
            {
                incoming = slot.Add(incoming);
                if (incoming.IsEmpty) return ItemStack.Empty;
            }
        }

        // Pass 2: fill empty slots
        foreach (var slot in _slots)
        {
            if (slot.IsEmpty)
            {
                incoming = slot.Add(incoming);
                if (incoming.IsEmpty) return ItemStack.Empty;
            }
        }

        OnChanged?.Invoke();
        return incoming; // whatever couldn't fit
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

    /// <summary>Swap the contents of two slots (works across inventories).</summary>
    public static void SwapSlots(ItemSlot a, ItemSlot b)
    {
        var tmp = a.Stack;
        a.Set(b.Stack);
        b.Set(tmp);
    }

    /// <summary>
    /// Move up to 'amount' items from slot src into slot dst (handles merging).
    /// Returns leftover ItemStack.
    /// </summary>
    public static ItemStack MoveSlotToSlot(ItemSlot src, ItemSlot dst, int amount)
    {
        if (src == null || dst == null || src.IsEmpty) return ItemStack.Empty;
        if (src == dst) return ItemStack.Empty;

        var taking = src.Remove(amount);
        var leftover = dst.Add(taking);

        // Put leftover back into src
        if (!leftover.IsEmpty) src.Add(leftover);

        return leftover;
    }
}
