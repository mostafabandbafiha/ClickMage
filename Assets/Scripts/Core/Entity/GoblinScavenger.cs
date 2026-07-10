// GoblinScavenger.cs — owns everything specific to goblins
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class GoblinScavenger : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;

    [Header("Stats")]
    [SerializeField] private BaseStat attackPower;
    [SerializeField] private BaseStat attackRange;


    protected override void Awake()
    {
        base.Awake();
        Agent.speed = GetStatValue(CommonStats.MoveSpeed);
    }

    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        // "Am I already busy?" (design doc's decision step 1) doesn't need its own
        // node — TryTickBehaviorTree() already refuses to tick while a command is
        // in flight (queue non-empty), which is exactly what "busy = do nothing"
        // means. So this selector only ever runs when the goblin is genuinely free
        // to decide: engage something worth fighting, or continue toward the Castle.
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new AcquireCombatTargetNode(), // Hero nearby, or a Tower attacking me
                new ScoredAdvanceNode()        // proactive: score the forward cone, smash or step
            )
        );
    }

    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        CurrentTarget.TakeDamage(GetStatValue(CommonStats.Damage), this);
    }


    protected override List<BaseStat> BuildStatAssetList()
    {
        var list = base.BuildStatAssetList();
        if (attackPower != null) list.Add(attackPower);
        if (attackRange != null) list.Add(attackRange);
        return list;
    }

    public override void Die(DeathCause cause)
    {
        if (cause == DeathCause.KilledByCommander)
        {
            if (_gravePrefab != null)
                Instantiate(_gravePrefab, transform.position, Quaternion.identity);
        }
        else
        {
            foreach (var slot in Inventory.Slots)
            {
                if (slot.IsEmpty) continue;
                Signals.Get<ItemDroppedToWorldSignal>().Dispatch(new ItemDroppedToWorldData
                {
                    Stack = slot.Stack,
                    WorldPosition = transform.position + Vector3.up * 0.5f,
                    DropRadius = 1f
                });
            }
        }

        base.Die(cause); // fires OnDeath + Destroy
    }
}