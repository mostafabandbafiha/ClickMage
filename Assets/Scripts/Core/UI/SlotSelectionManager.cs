using UnityEngine;
using deVoid.Utils;

/// <summary>Broadcast when the player selects a slot with shift+click.</summary>
public class SlotSelectedSignal : ASignal<SlotView> { }

/// <summary>Broadcast to clear any active selection.</summary>
public class SlotDeselectedSignal : ASignal { }

/// <summary>
/// Singleton that owns the "selected slot + chosen quantity" state.
/// Lives for the lifetime of the UI — attach to your UIRoot or Canvas.
/// </summary>
public class SlotSelectionManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static SlotSelectionManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    public SlotView SelectedSlot { get; private set; }
    public int SelectedAmount { get; private set; }
    public bool HasSelection => SelectedSlot != null && SelectedAmount > 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        Signals.Get<SlotSelectedSignal>().AddListener(OnSlotSelected);
        Signals.Get<SlotDeselectedSignal>().AddListener(ClearSelection);
    }

    private void OnDisable()
    {
        Signals.Get<SlotSelectedSignal>().RemoveListener(OnSlotSelected);
        Signals.Get<SlotDeselectedSignal>().RemoveListener(ClearSelection);
    }

    // ── Signal Handlers ───────────────────────────────────────────────────────

    private void OnSlotSelected(SlotView view)
    {
        // Clicking the same slot again just increments amount
        if (SelectedSlot == view)
        {
            TryIncrementAmount();
            return;
        }

        // New slot — deselect previous visually, then select this one
        DeselectCurrent();

        SelectedSlot = view;
        SelectedAmount = 1;

        view.SetSelected(true, SelectedAmount);
    }

    /// <summary>
    /// Called when player shift+clicks the ALREADY selected slot again.
    /// Each extra shift+click adds 1, capped at the slot's actual amount.
    /// </summary>
    public void TryIncrementAmount()
    {
        if (SelectedSlot == null) return;

        int max = SelectedSlot.Slot?.Amount ?? 0;
        if (SelectedAmount < max)
        {
            SelectedAmount++;
            SelectedSlot.SetSelected(true, SelectedAmount);
        }
    }

    /// <summary>
    /// Consume the selection (called by DragDropController after lifting items).
    /// </summary>
    public (SlotView slot, int amount) ConsumeSelection()
    {
        var result = (SelectedSlot, SelectedAmount);
        ClearSelection();
        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public void ClearSelection()
    {
        DeselectCurrent();
        SelectedSlot = null;
        SelectedAmount = 0;
    }

    private void DeselectCurrent()
    {
        SelectedSlot?.SetSelected(false, 0);
    }
}
