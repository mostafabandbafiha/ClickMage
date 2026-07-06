using ClickMage.Entities;
using ClickMage.Items;
using UnityEngine;


public class SelectableComponent : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Slot Owner")]
    [Tooltip("The entity that owns the equipment slots. Must implement ISlottable.")]
    [SerializeField] private MonoBehaviour slotOwnerObject;

    [Header("Building")]
    [Tooltip("Assign if this entity is a placeable structure.")]
    [SerializeField] private StructureDefinition structureDefinition;

    [SerializeField] private Inventory inventory;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Inventory Inventory => inventory;
    public StructureDefinition StructureDefinition => structureDefinition;
    public bool IsStructure => structureDefinition != null;

    public ISlottable SlotOwner { get; private set; }
    public SelectableComponent CurrentSelected { get; private set; }

    [Header("Selection Visual (optional)")]
    [SerializeField] private GameObject selectionHighlight;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        // Auto-assign slotOwner if not set in inspector
        if (slotOwnerObject == null)
            slotOwnerObject = GetComponent<BaseEntity>();

        if (slotOwnerObject == null)
        {
            Debug.LogWarning($"[SelectableComponent] '{gameObject.name}' has no slotOwner assigned.", this);
        }
        else
        {
            SlotOwner = slotOwnerObject as ISlottable;

            if (SlotOwner == null)
                Debug.LogWarning(
                    $"[SelectableComponent] '{gameObject.name}': '{slotOwnerObject.GetType().Name}'" +
                    $" does not implement ISlottable.", this);
        }

        // Inventory comes from BaseEntity - no need for GetComponent here
        // It will already exist because BaseEntity.Awake() runs first on same GameObject
        if (inventory == null)
            inventory = GetComponent<Inventory>();
    }

    public void OnSelected()
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(true);
    }

    public void OnDeselected()
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(false);
    }

    // ── Inventory Drop API (called by DragDropController) ──────────────────

    /// <summary>True if this entity's inventory exists and has at least one empty slot.</summary>
    public bool HasEmptySlot()
    {
        if (inventory == null) return false;

        foreach (var slot in inventory.Slots)
            if (slot.IsEmpty) return true;

        return false;
    }

    /// <summary>
    /// Attempt to move incoming stack into this entity's inventory.
    /// Returns whatever could NOT fit (empty stack = fully accepted).
    /// </summary>
    public ItemStack TryReceiveItem(ItemStack incoming)
    {
        if (inventory == null)
        {
            Debug.LogWarning($"[SelectableComponent] '{gameObject.name}' has no Inventory assigned.");
            return incoming;   // nothing accepted
        }

        return inventory.AddItem(incoming);
    }
}
