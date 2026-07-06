using ClickMage.Entities;
using UnityEngine;

public class Warehouse : BaseEntity
{
    public static Warehouse Instance { get; private set; }

    [Header("Warehouse Settings")]
    [SerializeField] private Transform _depositPoint;

    public Vector3 DepositPosition => _depositPoint != null
        ? _depositPoint.position
        : transform.position;

    public event System.Action<ItemStack> OnItemDeposited;

    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Deposit(ItemStack stack)
    {
        if (stack.IsEmpty) return false;
        var leftover = Inventory.AddItem(stack);
        if (leftover.Amount > 0) return false;
        OnItemDeposited?.Invoke(stack);
        return true;
    }

    public int CountItem(ItemData item) => Inventory.CountItem(item);
    public bool HasItem(ItemData item, int amount = 1) => Inventory.HasItem(item, amount);
}