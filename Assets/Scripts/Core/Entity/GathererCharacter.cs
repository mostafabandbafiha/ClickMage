// GathererCharacter.cs - UPDATED
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class GathererCharacter : BaseCharacter
{
    [Header("Gatherer Settings")]
    [Tooltip("Only pick up items that have at least one of these tags. Empty = accept all.")]
    [SerializeField] private List<ItemTag> _acceptedTags = new();

    [Header("Stats")]
    [SerializeField] private BaseStat _gatherSpeed;

    [Header("Hand Attachment")]
    [Tooltip("The bone transform where the held item visual is parented.")]
    [SerializeField] private Transform _handBone;

    [Header("Needs")]
    [SerializeField] private CharacterNeedsManager _needsManager;

    private GameObject _heldItemVisual;
    private ItemStack _carriedStack;

    public IReadOnlyList<ItemTag> AcceptedTags => _acceptedTags;
    public bool IsCarrying => !_carriedStack.IsEmpty;
    public ItemStack CarriedStack => _carriedStack;
    public Transform HandBone => _handBone;
    public CharacterNeedsManager NeedsManager => _needsManager;

    public bool AcceptsItem(WorldItemPickup pickup)
    {
        if (_acceptedTags == null || _acceptedTags.Count == 0) return true;

        foreach (var tag in _acceptedTags)
            if (pickup.HasTag(tag)) return true;

        return false;
    }

    public void AttachItem(WorldItemPickup pickup)
    {
        _carriedStack = pickup.Stack;

        if (_handBone != null && pickup.Stack.Data?.WorldVisualPrefab != null)
        {
            _heldItemVisual = Instantiate(
                pickup.Stack.Data.WorldVisualPrefab,
                _handBone.position,
                _handBone.rotation,
                _handBone
            );
            _heldItemVisual.transform.localPosition = Vector3.zero;
            _heldItemVisual.transform.localRotation = Quaternion.identity;
        }

        Object.Destroy(pickup.gameObject);
        Debug.Log($"[GathererCharacter] {name} picked up {_carriedStack.Data?.DisplayName}");
    }

    public void DetachItem()
    {
        if (_heldItemVisual != null)
        {
            Destroy(_heldItemVisual);
            _heldItemVisual = null;
        }

        Debug.Log($"[GathererCharacter] {name} deposited {_carriedStack.Data?.DisplayName}");
        _carriedStack = ItemStack.Empty;
    }

    protected override void Awake()
    {
        base.Awake();

        if (_needsManager == null)
            _needsManager = GetComponent<CharacterNeedsManager>();
        if (_needsManager == null)
            _needsManager = gameObject.AddComponent<CharacterNeedsManager>();
    }

    /// <summary>
    /// Build gatherer-specific behavior tree.
    /// Priority order: Deposit (if carrying) → Rest → Gather → return to post → Wander
    /// </summary>
    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new DepositBehaviorNode(),           // 1. deposit if carrying
                new RestBehaviorNode(),              // 2. rest if tired (BEFORE gather)
                new GatherItemsBehaviorNode(),       // 3. gather only if not tired
                new ReturnToGuardPositionNode(),     // 4. head back to post after the job's done
                new WanderBehaviorNode()             // 5. idle wander near guard position
            )
        );
    }

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        if (_gatherSpeed != null) list.Add(_gatherSpeed);
        return list;
    }
}