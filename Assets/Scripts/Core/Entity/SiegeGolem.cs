// SiegeGolem.cs
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class SiegeGolem : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;

    public float AreaRadius => GetStatValue(CommonStats.AreaRadius);
    public float AttackDamage => GetStatValue(CommonStats.Damage);

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

    // Called by animation event on impact frame
    public override void OnAttack()
    {
        Debug.Log($"[SiegeGolem] OnAttack � radius={AreaRadius}, damage={AttackDamage}, target={CurrentTarget}");
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        if (TargetRegistry.Instance == null) return;

        Vector3 center = CurrentTarget.transform.position;
        float radius = AreaRadius;
        float damage = AttackDamage;
        float radiusSq = radius * radius;
        Faction faction = CurrentTarget.Faction;

        var targets = new List<Targetable>(TargetRegistry.Instance.GetTargets(faction));
        Debug.Log($"[SiegeGolem] Checking {targets.Count} targets of faction {faction}");

        foreach (var t in targets)
        {
            if (t == null || !t.IsAlive) continue;
            float distSq = (t.transform.position - center).sqrMagnitude;
            Debug.Log($"[SiegeGolem] {t.name} distSq={distSq} radiusSq={radiusSq}");
            if (distSq > radiusSq) continue;

            t.TakeDamage(damage, this);
            if (t is IImpactReactive reactive)
            {
                Debug.Log($"[SiegeGolem] Triggering OnImpact on {t.name}");
                reactive.OnImpact();
            }
            else
            {
                Debug.Log($"[SiegeGolem] {t.name} is NOT IImpactReactive");
            }
        }
    }
    public override void Die(DeathCause cause)
    {
        CurrentTarget = null;

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