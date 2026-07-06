using System.Collections.Generic;
using ClickMage.Entities;
using ClickMage.Items;
using ClickMage.Stats;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel shown when an entity is selected.
/// Listens to SelectionManager events only — knows nothing about the camera.
///
/// Inspector wiring:
///   panelRoot        → root GameObject of this panel (to show/hide)
///   nameLabel        → TMP_Text for the entity name
///   iconImage        → Image for the entity icon
///   healthBar        → Slider used as the HP bar fill
///   healthLabel      → TMP_Text showing "100/100"  (optional)
///   statsContainer   → vertical LayoutGroup Transform where StatRowViews are spawned
///   statRowPrefab    → prefab with a StatRowView component
///   slotsContainer   → parent Transform where SlotViews are spawned
///   slotViewPrefab   → prefab with a SlotView component
/// </summary>
public class SelectionPanel : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Header")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private Image iconImage;

    [Header("Health")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthLabel;      // optional "100/100" overlay text

    [Header("Stats")]
    [SerializeField] private Transform statsContainer;  // vertical LayoutGroup parent
    [SerializeField] private StatRowView statRowPrefab; // one row per stat

    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private SlotView slotViewPrefab;

    // ── Runtime ────────────────────────────────────────────────────────────

    private readonly List<SlotView> _activeSlotViews = new();
    private readonly List<StatRowView> _activeStatRows = new();

    private SelectableComponent _currentComponent;
    private Inventory _currentInventory;   // tracked to unsubscribe
    private Targetable _currentTargetable;  // tracked to unsubscribe

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Start()
    {
        SelectionManager.Instance.OnSelected += HandleSelected;
        SelectionManager.Instance.OnDeselected += HandleDeselected;
        HidePanel();
    }

    private void OnDestroy()
    {
        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelected -= HandleSelected;
            SelectionManager.Instance.OnDeselected -= HandleDeselected;
        }

        UnsubscribeHealth();
        UnsubscribeInventory();
    }

    // ── SelectionManager handlers ──────────────────────────────────────────

    private void HandleSelected(SelectableComponent component) => ShowPanel(component);
    private void HandleDeselected(SelectableComponent component) => HidePanel();

    // ── Panel logic ────────────────────────────────────────────────────────

    private void ShowPanel(SelectableComponent component)
    {
        _currentComponent = component;
        panelRoot.SetActive(true);

        // ── Header ────────────────────────────────────────────────────────
        nameLabel.text = component.DisplayName;

        if (iconImage != null)
        {
            iconImage.sprite = component.Icon;
            iconImage.enabled = component.Icon != null;
        }

        // ── Health — subscribe to damage events for live updates ──────────
        UnsubscribeHealth();
        _currentTargetable = component.GetComponent<EntityTargetable>();
        if (_currentTargetable != null)
            _currentTargetable.OnDamageTaken += HandleDamageTaken;

        RefreshHealth();

        // ── Stats — full rebuild when entity changes ──────────────────────
        BuildStatRows(component);

        // ── Inventory slots ───────────────────────────────────────────────
        UnsubscribeInventory();
        _currentInventory = component.Inventory;
        if (_currentInventory != null)
            _currentInventory.OnInventoryChanged += RefreshSlotViews;

        BuildSlotViews();
    }

    private void HidePanel()
    {
        _currentComponent = null;

        nameLabel.text = string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        ClearHealth();
        UnsubscribeHealth();

        ClearStatRows();

        UnsubscribeInventory();
        ClearSlotViews();
    }

    // ── Health ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the OnDamageTaken event — refreshes health bar on every hit.
    /// </summary>
    private void HandleDamageTaken(float amount, BaseEntity attacker, DamageType type)
    {
        RefreshHealth();

        // Also refresh stat rows so any stat that changed mid-combat (armour
        // shred, lifesteal, etc.) stays current.
        RefreshStatRows();
    }

    /// <summary>
    /// Reads CommonStats.Health / CommonStats.MaxHealth and updates the bar.
    /// Mirrors EntityUI.UpdateHealthBar() exactly so keys always stay in sync.
    /// </summary>
    private void RefreshHealth()
    {
        if (healthBar == null || _currentComponent == null) return;

        var entity = _currentComponent.GetComponent<BaseEntity>();
        if (entity == null) { ClearHealth(); return; }

        if (!entity.HasStat(CommonStats.Health) || !entity.HasStat(CommonStats.MaxHealth))
        {
            ClearHealth();
            return;
        }

        float current = entity.GetStatValue(CommonStats.Health);
        float max = entity.GetStatValue(CommonStats.MaxHealth);
        if (max <= 0f) max = 1f;

        healthBar.minValue = 0f;
        healthBar.maxValue = max;
        healthBar.value = current;

        if (healthLabel != null)
            healthLabel.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    private void ClearHealth()
    {
        if (healthBar != null) healthBar.value = 0f;
        if (healthLabel != null) healthLabel.text = string.Empty;
    }

    private void UnsubscribeHealth()
    {
        if (_currentTargetable != null)
        {
            _currentTargetable.OnDamageTaken -= HandleDamageTaken;
            _currentTargetable = null;
        }
    }

    // ── Stat rows ─────────────────────────────────────────────────────────

    /// <summary>
    /// Full rebuild — spawns one StatRowView per stat exposed by the entity.
    /// Skips CommonStats.Health since it is already shown by the health bar.
    /// </summary>
    private void BuildStatRows(SelectableComponent component)
    {
        ClearStatRows();
        if (statsContainer == null || statRowPrefab == null) return;

        var entity = component.GetComponent<BaseEntity>();
        if (entity == null) return;

        var statHolder = entity.StatHolder as StatHolder;
        if (statHolder == null) return;

        foreach (BaseStat stat in statHolder.GetAllStats())
        {
            // Health is already shown in the dedicated bar — skip it here.
            if (stat.StatKey == CommonStats.Health) continue;

            StatRowView row = Instantiate(statRowPrefab, statsContainer);
            row.Bind(stat);
            _activeStatRows.Add(row);
        }
    }

    /// <summary>
    /// Lightweight refresh — tells every existing row to redraw its value.
    /// Called after damage is taken so on-hit modifiers (armour shred, etc.)
    /// are immediately reflected without rebuilding the whole list.
    /// </summary>
    private void RefreshStatRows()
    {
        foreach (StatRowView row in _activeStatRows)
            row.Refresh();
    }

    private void ClearStatRows()
    {
        foreach (StatRowView row in _activeStatRows)
            if (row != null) Destroy(row.gameObject);
        _activeStatRows.Clear();
    }

    // ── Inventory subscription ─────────────────────────────────────────────

    private void UnsubscribeInventory()
    {
        if (_currentInventory != null)
        {
            _currentInventory.OnInventoryChanged -= RefreshSlotViews;
            _currentInventory = null;
        }
    }

    // ── Slot view management ───────────────────────────────────────────────

    /// <summary>
    /// Full rebuild — called once when an entity is first selected.
    /// Creates one SlotView per slot in the entity's Inventory.
    /// </summary>
    private void BuildSlotViews()
    {
        ClearSlotViews();

        if (_currentInventory == null)
        {
            Debug.LogWarning("[SelectionPanel] Selected entity has no Inventory component.");
            return;
        }

        foreach (ItemSlot slot in _currentInventory.Slots)
        {
            SlotView view = Instantiate(slotViewPrefab, slotsContainer);
            view.Bind(slot, _currentInventory, slot.SlotIndex);
            _activeSlotViews.Add(view);
        }
    }

    /// <summary>
    /// Lightweight refresh — called whenever the inventory raises OnInventoryChanged.
    /// Each SlotView already holds a reference to its slot so just tell them to redraw.
    /// </summary>
    private void RefreshSlotViews()
    {
        foreach (SlotView view in _activeSlotViews)
            view.Refresh();
    }

    private void ClearSlotViews()
    {
        foreach (SlotView view in _activeSlotViews)
            if (view != null) Destroy(view.gameObject);
        _activeSlotViews.Clear();
    }
}