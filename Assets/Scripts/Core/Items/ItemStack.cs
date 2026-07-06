using System;
using UnityEngine;

/// <summary>
/// Lightweight value type: an ItemData reference + how many.
/// Used everywhere — inventory slots, drag payloads, drop spawns.
/// Never null: use ItemStack.Empty to represent "nothing".
/// </summary>
[Serializable]
public struct ItemStack : IEquatable<ItemStack>
{
    public static readonly ItemStack Empty = new ItemStack(null, 0);

    public ItemData Data;
    public int Amount;

    public ItemStack(ItemData data, int amount)
    {
        Data = data;
        Amount = (data == null) ? 0 : Mathf.Clamp(amount, 0, data.MaxStackSize);
    }

    public bool IsEmpty => Data == null || Amount <= 0;
    public bool IsFull => !IsEmpty && Amount >= Data.MaxStackSize;
    public int SpaceLeft => IsEmpty ? 0 : Data.MaxStackSize - Amount;

    /// <summary>
    /// Try to merge 'other' into this stack.
    /// Returns the leftover that did not fit.
    /// </summary>
    public ItemStack MergeWith(ItemStack other)
    {
        if (other.IsEmpty) return ItemStack.Empty;
        if (IsEmpty) return other;          // caller should replace, not merge
        if (other.Data != Data) return other;          // different item — nothing merged

        int canTake = Mathf.Min(SpaceLeft, other.Amount);
        Amount += canTake;
        int leftover = other.Amount - canTake;
        return leftover > 0 ? new ItemStack(other.Data, leftover) : ItemStack.Empty;
    }

    /// <summary>
    /// Split off 'count' items into a new stack, reducing this one.
    /// Returns the split-off stack (or Empty if not possible).
    /// </summary>
    public ItemStack Split(int count)
    {
        if (IsEmpty || count <= 0) return ItemStack.Empty;
        count = Mathf.Min(count, Amount);
        Amount -= count;
        return new ItemStack(Data, count);
    }

    public bool Equals(ItemStack other) => Data == other.Data && Amount == other.Amount;
    public override bool Equals(object obj) => obj is ItemStack s && Equals(s);
    public override int GetHashCode() => HashCode.Combine(Data, Amount);
    public override string ToString() => IsEmpty ? "[Empty]" : $"[{Data.DisplayName} ×{Amount}]";
}
