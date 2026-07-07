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
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private Transform _firePoint;

    [Header("Stats")]
    [SerializeField] private BaseStat attackDamage;
    [SerializeField] private BaseStat attackRange;
    [SerializeField] private BaseStat attackCooldown;

    public Transform FirePoint => _firePoint;

    // Current attack target � set by ArcherAttackCommand so animation event can read it

    protected override void Awake()
    {
        base.Awake();
        if (_data != null) Agent.speed = _data.moveSpeed;
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

    public override ICommand<BaseCharacter> CreateAttackCommand(Targetable target)
    {
        return new RangedAttackCommand(target, this);
    }

    // Called by animation event on the shoot frame
    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        if (_arrowPrefab == null || _firePoint == null) return;

        // Aim firepoint at target before spawning
        Vector3 dir = (CurrentTarget.Position - _firePoint.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            _firePoint.rotation = Quaternion.LookRotation(dir);

        GameObject obj = Instantiate(_arrowPrefab, _firePoint.position, _firePoint.rotation);
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Initialize(CurrentTarget, GetStatValue(CommonStats.Damage), this);
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