// VillagerCharacter.cs
// A purely social character. Day loop: find a rest point → sit → chat with neighbours.
// At night the existing HouseStructure/DayNightCycle system calls GoHome() automatically.
//
// Behavior tree priority (day):
//   1. RestBehaviorNode  — claims a rest point and sits until energy is full
//   2. WanderBehaviorNode — fallback so the character never stands idle
//
// No extra infrastructure required: reuses RestBehaviorNode, WanderBehaviorNode,
// CharacterNeedsManager, CharacterRestingState (speech bubbles included), and
// the HouseStructure day/night callbacks already in your project.

using ClickMage.Stats;
using System.Collections.Generic;
using UnityEngine;

public class VillagerCharacter : BaseCharacter
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Villager Stats")]
    [Tooltip("Optional stat assets specific to this villager (e.g. charisma). Leave empty if none.")]
    [SerializeField] private List<BaseStat> _villagerStats = new();

    [Header("Needs")]
    [Tooltip("Assign the CharacterNeedsManager that owns the Energy need. " +
             "Auto-created at runtime if left empty.")]
    [SerializeField] private CharacterNeedsManager _needsManager;

    // ── Public API ────────────────────────────────────────────────────────

    public CharacterNeedsManager NeedsManager => _needsManager;

    // ── Unity lifecycle ───────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();

        // Ensure a NeedsManager exists (mirrors GathererCharacter pattern).
        if (_needsManager == null)
            _needsManager = GetComponent<CharacterNeedsManager>();
        if (_needsManager == null)
            _needsManager = gameObject.AddComponent<CharacterNeedsManager>();
    }

    // ── Behavior tree ─────────────────────────────────────────────────────

    /// <summary>
    /// Day loop:
    ///   1. RestBehaviorNode  — find + claim a rest point, sit, chat, restore energy.
    ///   2. WanderBehaviorNode — wander until a rest point frees up.
    ///
    /// Night is handled externally: HouseStructure calls GoHome() via
    /// DayNightCycleManager.OnTimeOfDayChanged, exactly as it does for gatherers.
    /// </summary>
    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new RestBehaviorNode(),              // 1. sit & socialise if tired / rest point available
                new ReturnToGuardPositionNode(),     // 2. head back to post if drifted
                new WanderBehaviorNode()             // 3. wander near guard position until a spot opens up
            )
        );
    }

    // ── Stat list ─────────────────────────────────────────────────────────

    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        foreach (var stat in _villagerStats)
            if (stat != null) list.Add(stat);
        return list;
    }
}