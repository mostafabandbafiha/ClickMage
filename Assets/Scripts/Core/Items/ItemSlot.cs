using System;
using UnityEngine;

/// <summary>
/// One physical slot in any container (player inventory, entity equipment, chest, etc.).
/// Holds one ItemStack. Raises events so UI can react without polling.
/// </summary>
[Serializable]
public class ItemSlot
{
    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired whenever the contents of this slot change.</summary>
    public event Action<ItemSlot> OnChanged;

    // ── State ─────────────────────────────────────────────────────────────────
    [SerializeField] private ItemStack _stack = ItemStack.Empty;

    public ItemStack Stack => _stack;
    public bool IsEmpty => _stack.IsEmpty;
    public ItemData Item => _stack.Data;
    public int Amount => _stack.Amount;

    // ── Slot Index (set by owner Inventory for easy lookup) ───────────────────
    public int SlotIndex { get; internal set; } = -1;

    // ── Write API ─────────────────────────────────────────────────────────────

    /// <summary>Forcefully replace contents. Returns whatever was there before.</summary>
    public ItemStack Set(ItemStack newStack)
    {
        var previous = _stack;
        _stack = newStack;
        OnChanged?.Invoke(this);
        return previous;
    }

    /// <summary>Clear the slot. Returns what was removed.</summary>
    public ItemStack Clear()
    {
        return Set(ItemStack.Empty);
    }

    /// <summary>
    /// Add items to this slot.
    /// Returns a stack of whatever did NOT fit (Empty if everything fit).
    /// </summary>
    public ItemStack Add(ItemStack incoming, int maxStack)
    {
        if (incoming.IsEmpty) return ItemStack.Empty;

        // Slot is empty — take as much as fits
        if (IsEmpty)
        {
            int take = Mathf.Min(incoming.Amount, incoming.Data.MaxStackSize);
            _stack = new ItemStack(incoming.Data, take);
            int leftover = incoming.Amount - take;
            OnChanged?.Invoke(this);
            return leftover > 0 ? new ItemStack(incoming.Data, leftover) : ItemStack.Empty;
        }

        // Slot has a different item — cannot merge
        if (_stack.Data != incoming.Data) return incoming;

        // Merge
        var remainder = _stack.MergeWith(incoming);
        OnChanged?.Invoke(this);
        return remainder;
    }

    /// <summary>
    /// Remove up to 'count' items.
    /// Returns the ItemStack that was actually removed.
    /// </summary>
    public ItemStack Remove(int count)
    {
        if (IsEmpty || count <= 0) return ItemStack.Empty;

        count = Mathf.Min(count, _stack.Amount);
        var removed = new ItemStack(_stack.Data, count);

        int remaining = _stack.Amount - count;
        // Always reconstruct — never mutate a struct field directly
        _stack = remaining > 0
            ? new ItemStack(_stack.Data, remaining)
            : ItemStack.Empty;

        OnChanged?.Invoke(this);
        return removed;
    }

    /// <summary>Removes and returns the entire stack in one call.</summary>
    public ItemStack RemoveAll()
    {
        return Remove(Amount);
    }

    public override string ToString() => $"Slot[{SlotIndex}] {_stack}";
}
