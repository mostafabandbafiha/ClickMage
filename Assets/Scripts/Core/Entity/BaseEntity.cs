using System.Collections.Generic;
using ClickMage.Items;
using ClickMage.Stats;
using UnityEngine;

namespace ClickMage.Entities
{
    public abstract class BaseEntity : MonoBehaviour, IEffectTarget, ISlottable
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField] protected List<BaseStat> statAssets = new List<BaseStat>();

        [Header("Equipment Slots")]
        [SerializeField] private int maxSlots = 4;

        // ── Runtime ───────────────────────────────────────────────────────────
        private StatHolder _statHolder;
        private Inventory _inventory;
        private AuraHandler _auraHandler;

        // ── Public API ────────────────────────────────────────────────────────
        public IStatHolder StatHolder => _statHolder;

        public Inventory Inventory
        {
            get
            {
                if (_inventory == null) InitializeInventory();
                return _inventory;
            }
        }

        // ── ISlottable implementation ─────────────────────────────────────────
        public int MaxSlots => maxSlots;

        public IReadOnlyList<IItemSlot> Slots
        {
            get
            {
                // Cast each ItemSlot to IItemSlot for the interface
                var result = new List<IItemSlot>();
                if (_inventory == null) return result;
                foreach (var slot in _inventory.Slots)
                    result.Add((IItemSlot)slot);
                return result;
            }
        }

        public bool CanEquipItem(IItem item) => HasFreeSlot();

        public bool EquipItem(IItem item, int slotIndex)
        {
            if (item is not ItemData data) return false;
            var leftover = EquipItem(data, slotIndex);
            return leftover.IsEmpty;
        }

        //public void UnequipItem(int slotIndex) => UnequipItem(slotIndex);

        // ── Unity lifecycle ───────────────────────────────────────────────────
        protected virtual void Awake()
        {
            InitializeStats();
            InitializeInventory();
            ApplyAllPassiveEffects();
        }

        // ── Initialisation ────────────────────────────────────────────────────
        private void InitializeStats()
        {
            _statHolder = new StatHolder();

            // Let subclasses contribute their serialized stats
            var allStats = BuildStatAssetList();

            foreach (var asset in allStats)
                if (asset != null)
                    _statHolder.AddStat(asset.Clone());

            OnStatsInitialized();
        }

        private void ApplyAllPassiveEffects()
        {
            foreach (var slot in _inventory.Slots)
            {
                if (slot.IsEmpty) continue;
                foreach (var effect in slot.Item.PassiveEffects)
                    effect?.Apply(_statHolder);
            }
        }
        // ── Override in subclasses to contribute stats ────────────────────────
        protected virtual List<BaseStat> BuildStatAssetList()
        {
            // Base returns the inspector list on BaseEntity itself
            return new List<BaseStat>(statAssets);
        }

        private void InitializeInventory()
        {
            if (_inventory != null) return;

            _inventory = GetComponent<Inventory>();
            if (_inventory == null)
                _inventory = gameObject.AddComponent<Inventory>();

            _inventory.SlotCount = maxSlots;
            _inventory.InitSlots();
            _inventory.OnSlotChanged += HandleSlotChanged;

            _auraHandler = GetComponent<AuraHandler>();
            if (_auraHandler == null)
            {
                _auraHandler = gameObject.AddComponent<AuraHandler>();
                _auraHandler.Initialize(_inventory, $"Aura_{GetInstanceID()}");
            }
            else
            {
                _auraHandler.Initialize(_inventory, $"Aura_{GetInstanceID()}");
            }
        }

        // ── Extension points ──────────────────────────────────────────────────
        protected virtual void OnStatsInitialized() { }
        protected virtual void OnSlotChanged(ItemSlot slot)
        {
            // Lazily add AuraHandler the moment an aura item is equipped
            bool hasAuraItem = false;
            foreach (var s in _inventory.Slots)
            {
                if (s.IsEmpty) continue;
                foreach (var effect in s.Item.PassiveEffects)
                {
                    if (effect is AuraItemEffect)
                    {
                        hasAuraItem = true;
                        break;
                    }
                }
            }

            if (hasAuraItem && GetComponent<AuraHandler>() == null)
            {
                _auraHandler = gameObject.AddComponent<AuraHandler>();
                _auraHandler.Initialize(_inventory, $"Aura_{GetInstanceID()}");
            }
        }
        private void HandleSlotChanged(ItemSlot slot) => OnSlotChanged(slot);

        // ── Stat helpers ──────────────────────────────────────────────────────
        public float GetStatValue(string statKey) => _statHolder.GetStatValue(statKey);
        public void SetStatBaseValue(string statKey, float v) => _statHolder.SetStatBaseValue(statKey, v);
        public bool HasStat(string statKey) => _statHolder.HasStat(statKey);
        public void AddStatModifier(string statKey, StatModifier modifier) =>
            _statHolder.AddModifier(statKey, modifier);
        public void RemoveStatModifiersFromSource(string source) =>
            _statHolder.RemoveModifiersFromSource(source);


        // ── Slot helpers — UPDATED to apply passives ───────────────────────────

        public ItemStack EquipItem(ItemData item, int slotIndex, int amount = 1)
        {
            var slot = _inventory.GetSlot(slotIndex);
            if (slot == null || item == null) return new ItemStack(item, amount);

            var leftover = _inventory.AddToSlot(slot, new ItemStack(item, amount));

            // Apply passive effects if the item was successfully placed
            if (!slot.IsEmpty && slot.Item == item)
                ApplyPassiveEffects(item);

            return leftover;
        }

        public ItemStack UnequipItem(int slotIndex)
        {
            var slot = _inventory.GetSlot(slotIndex);
            if (slot == null) return ItemStack.Empty;

            var oldItem = slot.Item;
            var result = slot.Clear();

            if (oldItem != null)
                RemovePassiveEffects(oldItem);

            return result;
        }

        private void ApplyPassiveEffects(ItemData item)
        {
            foreach (var effect in item.PassiveEffects)
                effect?.Apply(_statHolder);
        }

        private void RemovePassiveEffects(ItemData item)
        {
            foreach (var effect in item.PassiveEffects)
                effect?.Remove(_statHolder);
        }
        public float GetStatValueSafe(string statKey) =>
            HasStat(statKey) ? GetStatValue(statKey) : 0f;

        public ItemData GetItemInSlot(int slotIndex) =>
            _inventory.GetSlot(slotIndex)?.Item;

        public ItemStack GetStackInSlot(int slotIndex) =>
            _inventory.GetSlot(slotIndex)?.Stack ?? ItemStack.Empty;

        public int FindEmptySlot()
        {
            foreach (var slot in _inventory.Slots)
                if (slot.IsEmpty) return slot.SlotIndex;
            return -1;
        }

        public List<ItemData> GetAllEquippedItems()
        {
            var result = new List<ItemData>();
            foreach (var slot in _inventory.Slots)
                if (!slot.IsEmpty) result.Add(slot.Item);
            return result;
        }

        public bool HasFreeSlot() => FindEmptySlot() >= 0;

        public bool HasItem(ItemData item, int amount = 1) =>
            _inventory.HasItem(item, amount);
    }
}
