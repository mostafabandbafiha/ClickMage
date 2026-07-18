// SkeletonArcher.cs
using ClickMage.Interface;
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonArcher : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private ProjectileRegistry _arrowRegistry; // holds arrow prefabs only

    [Header("Stats")]
    [SerializeField] private BaseStat attackDamage;
    [SerializeField] private BaseStat attackRange;
    [SerializeField] private BaseStat attackCooldown;

    public Transform FirePoint => _firePoint;

    // Current attack target � set by ArcherAttackCommand so animation event can read it

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
                 new RetreatNode(),
                 new ScoredAdvanceNode()
            )
        );
    }

    public override ICommand<BaseCharacter> CreateAttackCommand(Targetable target)
    {
        return new RangedAttackCommand(target, this);
    }

    // Called by animation event on the shoot frame
    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive || _firePoint == null) return;

        Vector3 dir = (CurrentTarget.Position - _firePoint.position);
        dir.y = 0;
        if (dir != Vector3.zero) _firePoint.rotation = Quaternion.LookRotation(dir);

        ProjectileSpawner.Instance.Spawn(
            this, _arrowRegistry, CurrentTarget, GetStatValue(CommonStats.Damage),
            _firePoint.position, _firePoint.rotation);

    }

    public override void Die(DeathCause cause)
    {
        DisengageCurrentTarget();

        if (cause == DeathCause.Retreated)
        {
            // Quiet despawn - no grave, no loot, this enemy wasn't killed
            base.Die(cause);
            return;
        }

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