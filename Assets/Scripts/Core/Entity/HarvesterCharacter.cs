// HarvesterCharacter.cs - UPDATED
using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class HarvesterCharacter : BaseCharacter, IHarvester
{
    [SerializeField] private CharacterData _data;

    [Header("Stats")]
    [SerializeField] private BaseStat harvestPower;

    [Header("Needs")]
    [SerializeField] private CharacterNeedsManager _needsManager;

    private Dictionary<ResourceNode, float> _harvestBlacklist = new();
    private const float BLACKLIST_DURATION = 10f;

    public float HarvestPower => GetStatValue(CommonStats.HarvestPower);
    public bool CanHarvest => GetStatValue(CommonStats.HarvestPower) > 0f;
    public string CharacterName => _data.characterName;
    public CharacterNeedsManager NeedsManager => _needsManager;

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        if (harvestPower != null) list.Add(harvestPower);
        return list;
    }

    protected override void Awake()
    {
        base.Awake();
        Agent.speed = _data.moveSpeed;

        if (_needsManager == null)
            _needsManager = GetComponent<CharacterNeedsManager>();
        if (_needsManager == null)
            _needsManager = gameObject.AddComponent<CharacterNeedsManager>();
    }

    protected override void Update()
    {
        base.Update();

        // Clean up blacklist periodically
        if (Time.frameCount % 60 == 0)
        {
            var expired = new List<ResourceNode>();
            foreach (var kvp in _harvestBlacklist)
            {
                if (Time.time >= kvp.Value)
                    expired.Add(kvp.Key);
            }
            foreach (var node in expired)
                _harvestBlacklist.Remove(node);
        }
    }

    /// <summary>
    /// Build harvester-specific behavior tree.
    /// Priority order: Rest → Harvest → return to post → Wander
    /// </summary>
    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new RestBehaviorNode(),              // 1. rest if tired
                new HarvestBehaviorNode(),           // 2. harvest if resources nearby
                new ReturnToGuardPositionNode(),     // 3. head back to post after the job's done
                new WanderBehaviorNode()             // 4. idle wander near guard position
            )
        );
    }

    public void BlacklistNode(ResourceNode node)
    {
        _harvestBlacklist[node] = Time.time + BLACKLIST_DURATION;
    }

    public bool IsNodeBlacklisted(ResourceNode node)
    {
        if (!_harvestBlacklist.TryGetValue(node, out float expireTime))
            return false;

        if (Time.time >= expireTime)
        {
            _harvestBlacklist.Remove(node);
            return false;
        }

        return true;
    }
}