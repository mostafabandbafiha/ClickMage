using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and manages SlotViews for an Inventory component.
/// Attach to the panel's root GameObject.
/// Set InventoryOwner in inspector or call Bind() at runtime.
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Inventory _inventoryOwner;    // drag player here
    [SerializeField] private Transform _slotContainer;     // parent for SlotViews
    [SerializeField] private SlotView _slotViewPrefab;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private readonly List<SlotView> _views = new();
    private Inventory _bound;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (_inventoryOwner != null) Bind(_inventoryOwner);
    }

    // ── Public ────────────────────────────────────────────────────────────────
    public void Bind(Inventory inventory)
    {
        // Unbind previous
        if (_bound != null) _bound.OnInventoryChanged -= RefreshAll;

        _bound = inventory;
        BuildViews();

        if (_bound != null) _bound.OnInventoryChanged += RefreshAll;
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private void BuildViews()
    {
        foreach (var v in _views) Destroy(v.gameObject);
        _views.Clear();

        if (_bound == null) return;

        for (int i = 0; i < _bound.Slots.Count; i++)
        {
            var view = Instantiate(_slotViewPrefab, _slotContainer);
            view.Bind(_bound.Slots[i], _bound, i); // pass owner + index now
            _views.Add(view);
        }
    }

    private void RefreshAll()
    {
        foreach (var v in _views) v.Refresh();
    }
}
