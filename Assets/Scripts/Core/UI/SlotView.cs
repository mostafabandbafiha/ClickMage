using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using deVoid.Utils;
using RTLTMPro;

/// <summary>
/// Visual representation of one ItemSlot.
/// Handles shift+click selection and drag initiation from a selection.
/// </summary>
public class SlotView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler, IPointerClickHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Visuals")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private RTLTextMeshPro _amountText;
    [SerializeField] private Image _highlightImage;   // selection ring/tint
    [SerializeField] private RTLTextMeshPro _selectedCountText; // shows "x2" etc.

    // ── Bound Data ────────────────────────────────────────────────────────────
    public ItemSlot Slot { get; private set; }
    public Inventory Owner { get; private set; }
    public int SlotIndex { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {

        SetSelected(false, 0);
    }

    // ── Binding ───────────────────────────────────────────────────────────────
    public void Bind(ItemSlot slot, Inventory owner, int index)
    {
        if (Slot != null) Slot.OnChanged -= OnSlotChanged;

        Slot = slot;
        Owner = owner;
        SlotIndex = index;

        Slot.OnChanged += OnSlotChanged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Slot != null) Slot.OnChanged -= OnSlotChanged;
    }

    private void OnSlotChanged(ItemSlot _) => Refresh();

    // ── Refresh ───────────────────────────────────────────────────────────────
    public void Refresh()
    {
        bool hasItem = Slot != null && !Slot.IsEmpty;

        _iconImage.enabled = hasItem;
        _iconImage.sprite = hasItem ? Slot.Item.Icon : null;

        bool showAmount = hasItem && Slot.Amount > 1;
        _amountText.text = showAmount ? Slot.Amount.ToString() : string.Empty;
        _amountText.enabled = showAmount;

        // If our slot was emptied externally, clear selection
        if (!hasItem && SlotSelectionManager.Instance != null &&
            SlotSelectionManager.Instance.SelectedSlot == this)
        {
            Signals.Get<SlotDeselectedSignal>().Dispatch();
        }
    }

    // ── Selection Visual ──────────────────────────────────────────────────────

    /// <summary>Called by SelectionManager — drives highlight and count badge.</summary>
    public void SetSelected(bool selected, int count)
    {
        if (_highlightImage != null)
            _highlightImage.enabled = selected;

        if (_selectedCountText != null)
        {
            // Show badge only when selected AND carrying more than 1
            bool showBadge = selected && count > 1;
            _selectedCountText.enabled = showBadge;
            _selectedCountText.text = showBadge ? $"×{count}" : string.Empty;
        }
    }

    // ── Pointer Click (Shift+Click = Select / Increment) ──────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Slot == null || Slot.IsEmpty) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) ||
                         Input.GetKey(KeyCode.RightShift);

        if (!shiftHeld)
        {
            // Plain click — clear any pending selection
            Signals.Get<SlotDeselectedSignal>().Dispatch();
            return;
        }

        // Shift+click → select (or increment if already this slot)
        Signals.Get<SlotSelectedSignal>().Dispatch(this);
    }

    // ── Drag Handlers ─────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var mgr = SlotSelectionManager.Instance;

        if (mgr != null && mgr.HasSelection && mgr.SelectedSlot == this)
        {
            // Drag the selected quantity
            DragDropController.Instance?.BeginSlotDragWithAmount(this, mgr.SelectedAmount);
            mgr.ClearSelection(); // selection consumed by drag start
        }
        else if (mgr == null || !mgr.HasSelection)
        {
            // No selection active — fall back to drag single item
            DragDropController.Instance?.BeginSlotDrag(this);
        }
        // If another slot is selected, do nothing (can't drag unselected slot)
    }

    public void OnDrag(PointerEventData eventData) { /* handled by DragDropController */ }

    public void OnEndDrag(PointerEventData eventData) { /* handled by DragDropController */ }

    public void OnDrop(PointerEventData eventData)
    {
        DragDropController.Instance?.DropOnSlot(this);
    }
}
