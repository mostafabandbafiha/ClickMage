using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using deVoid.Utils;
using ClickMage.Entities;

public class DragDropController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static DragDropController Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Drag Ghost")]
    [SerializeField] private RectTransform _dragGhost;
    [SerializeField] private Image _ghostIcon;
    [SerializeField] private TextMeshProUGUI _ghostAmountText;

    [Header("Canvas Reference")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Camera _uiCamera;

    [Header("World Drop Settings")]
    [SerializeField] private float _dropRadius = 0.8f;
    [SerializeField] private float _spawnHeightOffset = 1.5f;   // ← units above ground
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private Camera _mainCamera;

    [Header("Entity Drop")]
    [SerializeField] private LayerMask _entityDropLayer = ~0;
    [SerializeField] private float _entityRayDistance = 200f;

    // ── Cached ────────────────────────────────────────────────────────────────
    private RectTransform _canvasRect;
    private Camera _canvasCamera;

    // ── State ─────────────────────────────────────────────────────────────────
    private SlotView _originSlot;
    private ItemStack _carried;
    private bool _isDragging;
    private bool _dropHandledBySlot;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

        _canvasRect = _canvas.GetComponent<RectTransform>();
        _canvasCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (_uiCamera != null ? _uiCamera : _canvas.worldCamera);

        HideGhost();
    }

    private void Update()
    {
        if (!_isDragging) return;
        MoveGhostToMouse();
        if (Input.GetMouseButtonUp(0)) HandleRelease();
    }

    // ── Ghost Movement ─────────────────────────────────────────────────────────

    private void MoveGhostToMouse()
    {
        if (_dragGhost == null || _canvasRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            Input.mousePosition,
            _canvasCamera,
            out Vector2 localPoint
        );
        _dragGhost.localPosition = localPoint;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Normal drag — lifts exactly 1 item (no shift modifier needed anymore,
    /// shift is now reserved for the selection system).
    /// </summary>
    public void BeginSlotDrag(SlotView view)
    {
        if (_isDragging) return;
        if (view.Slot == null || view.Slot.IsEmpty) return;

        _originSlot = view;
        _dropHandledBySlot = false;
        _carried = view.Slot.Remove(1);

        StartGhost();
    }

    /// <summary>
    /// Called when the player drags a slot that already has a shift+click selection.
    /// Lifts exactly the chosen amount.
    /// </summary>
    public void BeginSlotDragWithAmount(SlotView view, int amount)
    {
        if (_isDragging) return;
        if (view.Slot == null || view.Slot.IsEmpty) return;

        amount = Mathf.Clamp(amount, 1, view.Slot.Amount);

        _originSlot = view;
        _dropHandledBySlot = false;
        _carried = view.Slot.Remove(amount);

        StartGhost();
    }

    /// <summary>Called by SlotView.OnDrop.</summary>
    public void DropOnSlot(SlotView target)
    {
        if (!_isDragging || target == null) return;

        ItemStack leftover = target.Owner.AddToSlot(target.Slot, _carried);
        _carried = leftover;

        if (_carried.IsEmpty)
        {
            _dropHandledBySlot = true;
            EndDrag(success: true);
        }
        else
        {
            UpdateGhostAmount();
        }
    }

    // ── Ghost Visuals ─────────────────────────────────────────────────────────

    private void StartGhost()
    {
        _isDragging = true;
        if (_dragGhost == null || _carried.Data == null) return;

        _ghostIcon.sprite = _carried.Data.Icon;
        _ghostIcon.enabled = true;

        UpdateGhostAmount();
        MoveGhostToMouse();
        _dragGhost.gameObject.SetActive(true);
    }

    private void UpdateGhostAmount()
    {
        if (_ghostAmountText == null) return;

        // Only show number when carrying more than 1
        bool show = _carried.Amount > 1;
        _ghostAmountText.text = show ? _carried.Amount.ToString() : string.Empty;
        _ghostAmountText.enabled = show;
    }

    private void HideGhost()
    {
        if (_dragGhost != null)
            _dragGhost.gameObject.SetActive(false);
    }

    // ── Release Handling ──────────────────────────────────────────────────────

    private void HandleRelease()
    {
        if (!_isDragging) return;

        // Slot OnDrop already handled it
        if (_dropHandledBySlot) return;

        if (TryDropOnEntity()) return;

        // Released over a slot but OnDrop didn't fire — safety return
        if (IsPointerDirectlyOverSlot())
        {
            ReturnToOrigin();
            return;
        }

        // Try ground raycast
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, _groundLayer))
        {
            // Offset upward so rigidbody items fall onto the ground
            Vector3 spawnPos = hit.point + Vector3.up * _spawnHeightOffset;
            DispatchDropToWorld(spawnPos);
            EndDrag(success: true);
            return;
        }

        ReturnToOrigin();
    }

    private void DropOnEntity(SelectableComponent entity)
    {
        var baseEntity = entity.GetComponent<BaseEntity>(); // adjust namespace as needed
        if (baseEntity == null)
        {
            Debug.LogWarning("[DragDrop] Entity has no BaseEntity.");
            return;
        }

        int slotIndex = baseEntity.FindEmptySlot();
        if (slotIndex < 0)
        {
            Debug.Log("[DragDrop] No empty slot.");
            return;
        }

        ItemStack leftover = baseEntity.EquipItem(_carried.Data, slotIndex, _carried.Amount);
        _carried = leftover.Amount > 0 ? leftover : ItemStack.Empty;
    }

    private bool TryDropOnEntity()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, _entityRayDistance, _entityDropLayer))
        {
            EndDrag(success: true);
            return false;
        }
            

        // GetComponentInParent so the collider can be on a child mesh object
        var selectable = hit.collider.GetComponentInParent<SelectableComponent>();

        if (selectable == null) return false;
        if (!selectable.HasEmptySlot())
        {
            Debug.Log($"[DragDrop] {selectable.DisplayName} inventory is full.");
            EndDrag(success: false);
            return false;   // fall through to world-drop / return
        }

        DropOnEntity(selectable);
        EndDrag(success: true);
        return true;
    }

    private bool IsPointerDirectlyOverSlot()
    {
        if (EventSystem.current == null) return false;

        var results = new List<RaycastResult>();
        var pointerData = new PointerEventData(EventSystem.current)
        { position = Input.mousePosition };

        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
            if (r.gameObject.GetComponent<SlotView>() != null ||
                r.gameObject.GetComponentInParent<SlotView>() != null)
                return true;

        return false;
    }

    // ── World Drop ────────────────────────────────────────────────────────────

    private void DispatchDropToWorld(Vector3 worldPosition)
    {
        if (_carried.IsEmpty || _carried.Data == null) return;

        var data = new ItemDroppedToWorldData
        {
            Stack = _carried,
            WorldPosition = worldPosition,      // already height-offset
            SourceInventory = _originSlot?.Owner,
            SlotIndex = _originSlot?.SlotIndex ?? -1,
            DropRadius = _dropRadius
        };

        Signals.Get<ItemDroppedToWorldSignal>().Dispatch(data);
        _carried = ItemStack.Empty;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    private void ReturnToOrigin()
    {
        if (!_carried.IsEmpty && _originSlot != null)
        {
            var leftover = _originSlot.Owner.AddToSlot(_originSlot.Slot, _carried);

            // Extremely unlikely (it's the same slot it came from), but if for some
            // reason it doesn't all fit back, don't just drop it — try the rest of
            // that inventory instead of silently destroying items.
            if (!leftover.IsEmpty)
                leftover = _originSlot.Owner.AddItem(leftover);

            // If it *still* doesn't fit (inventory full/changed while dragging),
            // at least don't lose it silently — decide what you want here,
            // e.g. drop to world at player's feet, or log a warning.
            if (!leftover.IsEmpty)
                Debug.LogWarning($"[DragDrop] Could not return {leftover.Amount}x {leftover.Data?.DisplayName} to origin — inventory full/changed.");
        }

        _carried = ItemStack.Empty;
        EndDrag(success: false);
    }

    private void EndDrag(bool success)
    {
        _isDragging = false;
        _dropHandledBySlot = false;
        _originSlot = null;
        _carried = ItemStack.Empty;
        HideGhost();
    }
}
