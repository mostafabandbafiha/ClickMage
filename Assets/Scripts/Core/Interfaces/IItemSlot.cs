
    namespace ClickMage.Items
    {



        public interface IItemSlot
        {
            int SlotIndex { get; }
            SlotType SlotType { get; }
            bool IsEmpty { get; }
            bool IsLocked { get; }
            IItem EquippedItem { get; }

            bool CanAcceptItem(IItem item);

            /// <summary>
            /// Equips an item and applies its passive effects to the target
            /// </summary>
            bool TryEquip(IItem item, IEffectTarget target);

            /// <summary>
            /// Unequips the current item and removes its passive effects
            /// </summary>
            bool TryUnequip();

            void Tick(float deltaTime);

            // Active item helpers - slot owns cooldown/charge state
            bool CanUseActive();
            bool TryUseActive();
            float GetCooldownProgress();
            int GetCurrentCharges();
        }
    }

