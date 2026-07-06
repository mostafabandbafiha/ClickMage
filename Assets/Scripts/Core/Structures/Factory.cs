using ClickMage.Entities;
using ClickMage.StateMachine;
using ClickMage.Stats;
using UnityEngine;

namespace ClickMage.Factories
{
    public class Factory : BaseEntity
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Recipe")]
        [SerializeField] private FactoryRecipe _currentRecipe;

        [Header("Recipes")]
        [SerializeField] private FactoryRecipe[] availableRecipes;
        [SerializeField][Range(-1, 10)] private int defaultRecipeIndex = 0;


        // -------------------------------------------------------
        // Public surface
        // -------------------------------------------------------

        public FactoryRecipe CurrentRecipe => _currentRecipe;
        public float ProductionTimer { get; set; }

        public event System.Action<FactoryRecipe> OnProductionStarted;
        public event System.Action<FactoryRecipe> OnProductionCompleted;

        // -------------------------------------------------------
        // State machine
        // -------------------------------------------------------

        private StateMachine<Factory> _stateMachine;

        // -------------------------------------------------------
        // Unity lifecycle
        // -------------------------------------------------------

        // No Awake() override here at all - let BaseEntity.Awake() run
        // so InitializeInventory() completes before Start() subscribes


        protected virtual void Start()
        {
            Inventory.OnChanged += OnInventoryChanged;
            _stateMachine = new StateMachine<Factory>(this);
            _stateMachine.ChangeState(new FactoryIdleState());

            if (defaultRecipeIndex >= 0 && availableRecipes?.Length > 0)
                SetRecipe(availableRecipes[defaultRecipeIndex]);
        }



        protected virtual void OnDestroy()
        {
            if (Inventory != null)
                Inventory.OnChanged -= OnInventoryChanged;
        }

        protected virtual void Update() => _stateMachine?.Tick(Time.deltaTime);

        // -------------------------------------------------------
        // State machine API
        // -------------------------------------------------------

        public void ChangeState(IState<Factory> newState) =>
            _stateMachine.ChangeState(newState);

        // -------------------------------------------------------
        // Inventory changed nudge
        // -------------------------------------------------------

        private void OnInventoryChanged()
        {
            if (_stateMachine == null) return;

            if (_stateMachine.CurrentState is FactoryWaitingForInputState
                || _stateMachine.CurrentState is FactoryOutputFullState)
            {
                _stateMachine.Tick(0f);
            }
        }

        // -------------------------------------------------------
        // Recipe
        // -------------------------------------------------------

        public void SetRecipe(FactoryRecipe recipe)
        {
            _currentRecipe = recipe;
            ChangeState(new FactoryIdleState());
        }

        public FactoryRecipe[] GetAvailableRecipes() => availableRecipes;

        public void SelectRecipe(int index)
        {
            if (availableRecipes == null || index < 0 || index >= availableRecipes.Length) return;
            SetRecipe(availableRecipes[index]);
        }

        // -------------------------------------------------------
        // Stat helpers - use inherited StatHolder, not a duplicate field
        // -------------------------------------------------------

        public float GetProductionSpeed() =>
            HasStat("speed") ? GetStatValue("speed") : 1f;

        private float GetEfficiency() =>
            HasStat("efficiency") ? GetStatValue("efficiency") : 1f;

        // -------------------------------------------------------
        // Core production logic
        // -------------------------------------------------------

        public bool HasRequiredInputs()
        {
            if (_currentRecipe == null) return false;

            foreach (var input in _currentRecipe.inputs)
            {
                if (input.resource == null) continue;
                if (Inventory.CountItem(input.resource) < input.amount)
                    return false;
            }

            return true;
        }

        public bool HasOutputSpace()
        {
            if (_currentRecipe == null) return false;

            int simulatedFreeSlots = CountFreeSlots();
            int slotsNeeded = 0;

            foreach (var output in _currentRecipe.outputs)
            {
                if (output.resource == null) continue;

                int amount = GetScaledOutputAmount(output);
                int existingSpace = CountPartialSpace(output.resource, output.resource.MaxStackSize);
                int remaining = amount - existingSpace;

                if (remaining > 0)
                    slotsNeeded += Mathf.CeilToInt((float)remaining / output.resource.MaxStackSize);
            }

            return slotsNeeded <= simulatedFreeSlots;
        }

        public void ConsumeInputs()
        {
            if (_currentRecipe == null) return;

            foreach (var input in _currentRecipe.inputs)
            {
                if (input.resource == null) continue;
                Inventory.RemoveItem(input.resource, input.amount);
            }
        }

        public void ProduceOutputs()
        {
            if (_currentRecipe == null) return;

            float efficiency = GetEfficiency();

            foreach (var output in _currentRecipe.outputs)
            {
                if (output.resource == null) continue;

                int amount = GetScaledOutputAmount(output, efficiency);
                var leftover = Inventory.AddItem(new ItemStack(output.resource, amount));

                if (leftover.Amount > 0)
                    HandleOutputOverflow(leftover);
            }
        }

        // -------------------------------------------------------
        // Notifications
        // -------------------------------------------------------

        public void NotifyProductionStarted() => OnProductionStarted?.Invoke(_currentRecipe);
        public void NotifyProductionCompleted() => OnProductionCompleted?.Invoke(_currentRecipe);

        // -------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------

        private int GetScaledOutputAmount(FactoryRecipe.OutputProduction output,
                                          float efficiency = 1f) =>
            Mathf.Max(1, Mathf.RoundToInt(output.baseAmount * efficiency));

        private int CountFreeSlots()
        {
            int free = 0;
            foreach (var slot in Inventory.Slots)
                if (slot.IsEmpty) free++;
            return free;
        }

        private int CountPartialSpace(ItemData item, int maxStack)
        {
            int space = 0;
            foreach (var slot in Inventory.Slots)
                if (!slot.IsEmpty && slot.Stack.Data == item)
                    space += maxStack - slot.Stack.Amount;
            return space;
        }

        protected virtual void HandleOutputOverflow(ItemStack overflow)
        {
            Debug.LogWarning(
                $"[{name}] Inventory full – lost {overflow.Amount}x {overflow.Data.name}");
        }
    }
}
