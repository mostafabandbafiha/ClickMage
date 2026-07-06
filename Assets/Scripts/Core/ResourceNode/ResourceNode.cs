// ResourceNode.cs
using UnityEngine;
using deVoid.Utils;
using ClickMage.Stats;
using ClickMage.StateMachine;
using ClickMage.Entities;
using System.Collections.Generic;

[RequireComponent(typeof(Inventory))]
public class ResourceNode : BaseEntity, IHarvestable
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Resource Node")]
    [SerializeField] private ResourceNodeData _data;
    [SerializeField] private Transform _interactPoint;
    [SerializeField] private Targetable _targetable;

    [Header("Visual Stages")]
    [Tooltip("Assign stage GameObjects here. Must match order of thresholds in SO.")]
    [SerializeField] private GameObject[] _stageObjects;

    [Header("Regen")]
    [Tooltip("Seconds after last hit before regen begins. 0 = regen immediately.")]
    [SerializeField] private float _regenDelay = 3f;


    // ── HP — routed through StatHolder exactly like Tower ─────────────────────
    public float CurrentHP => GetStatValue(CommonStats.Health);
    public float MaxHP => GetStatValue(CommonStats.MaxHealth);
    public float HPPercent => MaxHP > 0f ? CurrentHP / MaxHP : 0f;

    // ── State Machine ─────────────────────────────────────────────────────────
    public StateMachine<ResourceNode> StateMachine { get; private set; }
    public ResourceNodeActiveState ActiveState { get; private set; }
    public ResourceNodeDepletedState DepletedState { get; private set; }

    // ── Internals ─────────────────────────────────────────────────────────────
    private int _currentStageIndex = -1;
    private float _regenDelayTimer = 0f;


    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        // BuildStatAssetList is called by base.Awake() via InitializeStats()
        // so stats are ready before Start() runs — same order as Tower.
        ActiveState = new ResourceNodeActiveState();
        DepletedState = new ResourceNodeDepletedState();
        StateMachine = new StateMachine<ResourceNode>(this);

        base.Awake();
    }

    private void Start()
    {
        Debug.Log($"[{name}] MaxHP stat value: {MaxHP}");

        // Seed health to max on spawn — same as Tower does via stat assets
        SetStatBaseValue(CommonStats.Health, MaxHP);

        if (CurrentHP <= 0f)
            Debug.LogWarning($"[{name}] CurrentHP is 0! Check Max Health stat in inspector.");

        UpdateVisualStage();
        StateMachine.ChangeState(ActiveState);
    }

    private void Update()
    {
        StateMachine.Tick(Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IHarvestable
    // ─────────────────────────────────────────────────────────────────────────

    public bool CanHarvest() => CurrentHP > 0f;

    public bool TryHarvest(IHarvester harvester)
    {
        if (!CanHarvest()) return false;
        ApplyDamage(harvester.HarvestPower);
        return true;
    }

    public bool TryHarvest()
    {
        if (!CanHarvest()) return false;
        ApplyDamage(25f);
        return true;
    }

    public Vector3 GetInteractPosition()
        => _interactPoint != null ? _interactPoint.position : transform.position;

    // ─────────────────────────────────────────────────────────────────────────
    // Damage & Regen
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyDamage(float amount)
    {
        _regenDelayTimer = _regenDelay;

        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.one * 1.15f, 0.08f)
                 .setEase(LeanTweenType.easeOutQuad)
                 .setOnComplete(() =>
                     LeanTween.scale(gameObject, Vector3.one, 0.12f)
                              .setEase(LeanTweenType.easeInQuad));

        int oldStage = _currentStageIndex;

        // Write HP back through StatHolder — same pattern as EntityTargetable.TakeDamage()
        //float newHP = Mathf.Max(0f, CurrentHP - amount);
        //SetStatBaseValue(CommonStats.Health, newHP);
        _targetable?.TakeDamage(amount);

        UpdateVisualStage();

        if (_currentStageIndex != oldStage)
            SpawnOutputs();

        // Always spawn on depletion regardless of stage change
        if (CurrentHP <= 0f)
        {
            SpawnOutputs();
            HandleDepletion();
        }
    }

    private void HandleDepletion()
    {
        float regen = GetStatValue(CommonStats.RegenRate);

        if (regen <= 0f)
        {
            SpawnInventoryContents();
            Destroy(gameObject);
            return;
        }

        if (StateMachine.CurrentState != DepletedState)
            StateMachine.ChangeState(DepletedState);
    }

    private void SpawnInventoryContents()
    {
        if (Inventory == null) return;

        foreach (var slot in Inventory.Slots)
        {
            if (slot.IsEmpty) continue;

            var data = new ItemDroppedToWorldData
            {
                Stack = slot.Stack,
                WorldPosition = transform.position,
                DropRadius = _data.dropRadius,
                SourceInventory = Inventory,
                SlotIndex = slot.SlotIndex
            };

            Signals.Get<ItemDroppedToWorldSignal>().Dispatch(data);
        }
    }

    /// <summary>
    /// Called every frame by ResourceNodeDepletedState and ResourceNodeActiveState.
    /// Respects the regen delay so hits can't be instantly healed.
    /// </summary>
    public void TickRegen(float deltaTime)
    {
        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= deltaTime;
            return;
        }

        if (CurrentHP >= MaxHP) return;

        float rate = GetStatValue(CommonStats.RegenRate);
        if (rate <= 0f) return;

        // Write through StatHolder — consistent with every other HP mutation
        float newHP = Mathf.Min(MaxHP, CurrentHP + rate * deltaTime);
        SetStatBaseValue(CommonStats.Health, newHP);

        UpdateVisualStage();

        if (CurrentHP > 0f && StateMachine.CurrentState == DepletedState)
            StateMachine.ChangeState(ActiveState);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Visual Stages
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateVisualStage()
    {
        if (_stageObjects == null || _stageObjects.Length == 0) return;

        int target = ResolveStageIndex(HPPercent);
        if (target == _currentStageIndex) return;

        for (int i = 0; i < _stageObjects.Length; i++)
        {
            if (_stageObjects[i] != null)
                _stageObjects[i].SetActive(i == target);
        }

        _currentStageIndex = target;
    }

    private int ResolveStageIndex(float hpPercent)
    {
        if (_data.stageThresholds == null || _data.stageThresholds.Length == 0)
            return 0;

        for (int i = 0; i < _data.stageThresholds.Length; i++)
        {
            if (hpPercent >= _data.stageThresholds[i])
                return i;
        }

        return _data.stageThresholds.Length - 1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Item Spawning
    // ─────────────────────────────────────────────────────────────────────────

    private void SpawnOutputs()
    {
        if (_data.outputs == null) return;

        foreach (var output in _data.outputs)
        {
            if (output.item == null) continue;
            if (Random.value > output.dropChance) continue;

            int amount = Random.Range(output.minAmount, output.maxAmount + 1);
            if (amount <= 0) continue;

            var data = new ItemDroppedToWorldData
            {
                Stack = new ItemStack(output.item, amount),
                WorldPosition = transform.position,
                DropRadius = _data.dropRadius,
                SourceInventory = null,
                SlotIndex = -1
            };

            Signals.Get<ItemDroppedToWorldSignal>().Dispatch(data);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State Callback
    // ─────────────────────────────────────────────────────────────────────────

    public void OnDepleted()
    {
        Debug.Log($"[ResourceNode] {name} depleted.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Test/Deal 25 Damage")]
    private void TestDeal25Damage() => TryHarvest(new DebugHarvester(25f));

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetInteractPosition(), 0.3f);

        if (_data != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _data.dropRadius);
        }

        if (Application.isPlaying && StateMachine != null)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"HP: {CurrentHP:F1}/{MaxHP:F0}  " +
                $"Regen: {GetStatValue(CommonStats.RegenRate):F2}/s  " +
                $"Delay: {_regenDelayTimer:F1}s  " +
                $"[{StateMachine.CurrentState?.GetType().Name}]"
            );
        }
    }

    public class DebugHarvester : IHarvester
    {
        public float HarvestPower { get; }
        public DebugHarvester(float power) { HarvestPower = power; }
    }
#endif
}