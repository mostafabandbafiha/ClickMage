using UnityEngine;
using deVoid.Utils;

[RequireComponent(typeof(InteractableComponent))]
public class WorldItemPickup : MonoBehaviour
{
    [SerializeField] private ItemStack _stack;

    public ItemStack Stack => _stack;
    public bool IsClaimed { get; private set; }

    /// <summary>Forwarded from ItemData so GatherItemsBehavior can filter by tag.</summary>
    public bool HasTag(ItemTag tag) =>
        _stack.Data != null && _stack.Data.HasTag(tag);

    private InteractableComponent _interactable;

    private void Awake()
    {
        _interactable = GetComponent<InteractableComponent>();
        _interactable.RegisterInteraction(OnPlayerInteract);
        RefreshText();
    }

    public void Init(ItemStack stack)
    {
        _stack = stack;
        RefreshText();
    }

    // ── Claim system (for gatherer characters) ────────────────────────────

    /// <summary>Returns false if already claimed by another gatherer.</summary>
    public bool TryClaim()
    {
        if (IsClaimed) return false;
        IsClaimed = true;
        return true;
    }

    public void Unclaim() => IsClaimed = false;

    // ── Player interaction ────────────────────────────────────────────────

    private void OnPlayerInteract()
    {
        if (_stack.IsEmpty) return;
        Signals.Get<ItemCollectedSignal>().Dispatch(_stack);
        Destroy(gameObject);
    }

    private void RefreshText()
    {
        if (_interactable == null) return;
        _interactable.SetInteractionText(
            $"Pick up {(_stack.Data != null ? _stack.Data.DisplayName : "item")}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsClaimed ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}