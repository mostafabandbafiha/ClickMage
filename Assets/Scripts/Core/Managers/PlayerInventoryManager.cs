using UnityEngine;
using deVoid.Utils;

// ─── Signal Definitions ───────────────────────────────────────────────────────

public class ItemCollectedSignal : ASignal<ItemStack> { }
public class InventoryChangedSignal : ASignal<Inventory, int> { }

public struct ItemDroppedToWorldData
{
    public ItemStack Stack;
    public Vector3 WorldPosition;
    public Inventory SourceInventory;
    public int SlotIndex;
    public float DropRadius;
}

public class ItemDroppedToWorldSignal : ASignal<ItemDroppedToWorldData> { }

public struct ItemTransferredToEntityData
{
    public ItemStack Transferred;
    public ItemStack Leftover;
    public SelectableComponent TargetEntity;
}

public class ItemTransferredToEntitySignal : ASignal<ItemTransferredToEntityData> { }

// ─── PlayerInventoryManager ───────────────────────────────────────────────────

public class PlayerInventoryManager : MonoBehaviour
{
    [SerializeField] private Inventory playerInventory;

    private void Awake()
    {
        Signals.Get<ItemCollectedSignal>().AddListener(OnItemCollected);
        // No longer need to listen to ItemDroppedToWorldSignal here
        // Items are removed from inventory when the drag begins
    }

    private void OnDestroy()
    {
        Signals.Get<ItemCollectedSignal>().RemoveListener(OnItemCollected);
    }

    private void OnItemCollected(ItemStack stack)
    {
        playerInventory.AddItem(stack);
    }


}