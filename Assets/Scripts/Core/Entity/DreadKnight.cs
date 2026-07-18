// DreadKnight.cs
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class DreadKnight : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;

    public float AoeRadius => GetStatValue(CommonStats.AreaRadius);


    protected override void Awake()
    {
        base.Awake();
        Agent.speed = GetStatValue(CommonStats.MoveSpeed);
    }

    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new AcquireCombatTargetNode(),
                new ScoredAdvanceNode()
            )
        );
    }

    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        if (TargetRegistry.Instance == null) return;

        // Damage everything in AoeRadius around self � not around target
        Vector3 center = transform.position;
        float radiusSq = AoeRadius * AoeRadius;
        Faction faction = CurrentTarget.Faction;

        var targets = new List<Targetable>(TargetRegistry.Instance.GetTargets(faction));

        foreach (var t in targets)
        {
            if (t == null || !t.IsAlive) continue;

            float distSq = (t.transform.position - center).sqrMagnitude;
            if (distSq > radiusSq) continue;

            t.TakeDamage(GetStatValue(CommonStats.Damage), this);

            /*if (t is IImpactReactive reactive)
                reactive.OnImpact();*/
        }
    }

    public override void Die(DeathCause cause)
    {
        DisengageCurrentTarget();

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

        base.Die(cause);
    }
}