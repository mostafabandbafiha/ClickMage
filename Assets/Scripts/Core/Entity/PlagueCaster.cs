// PlagueCaster.cs
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class PlagueCaster : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;
    [SerializeField] private GameObject _poisonOrbPrefab; // different projectile
    [SerializeField] private Transform _firePoint;

    [Header("Stats")]
    [SerializeField] private BaseStat attackDamage;
    [SerializeField] private BaseStat attackRange;
    [SerializeField] private BaseStat attackCooldown;

    public float AttackDamage => _data != null ? _data.attackDamage : 0f;
    public float AttackCooldown => _data != null ? _data.attackCooldown : 2.5f;
    public float AttackRange => _data != null ? _data.attackRange : 7f;
    public Transform FirePoint => _firePoint;


    protected override void Awake()
    {
        base.Awake();
        if (_data != null) Agent.speed = _data.moveSpeed;
    }

    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new ArcherSeekBehaviorNode(), // same seek logic works for any ranged enemy
                new WanderBehaviorNode() // safety fallback if no target exists (e.g. no Castle placed)
            )
        );
    }

    // Called by animation event
    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        if (_poisonOrbPrefab == null || _firePoint == null) return;

        Vector3 dir = (CurrentTarget.Position - _firePoint.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            _firePoint.rotation = Quaternion.LookRotation(dir);

        GameObject obj = Instantiate(_poisonOrbPrefab, _firePoint.position, _firePoint.rotation);
        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Initialize(CurrentTarget, AttackDamage, this);
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