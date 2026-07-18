// AbyssalHorror.cs
using ClickMage.Stats;
using deVoid.Utils;
using System.Collections.Generic;
using UnityEngine;

public class AbyssalHorror : EnemyCharacter
{
    [SerializeField] private EnemyData _data;
    [SerializeField] private GameObject _gravePrefab;

    [Header("Summon")]
    [SerializeField] private List<EnemyCharacter> _summonPrefabs = new();
    [SerializeField] private int _maxSummons = 3;
    [SerializeField] private float _summonRadius = 2f;

    private readonly List<EnemyCharacter> _activeSummons = new();

    public List<EnemyCharacter> SummonPrefabs => _summonPrefabs;
    public int MaxSummons => _maxSummons;
    public float SummonRadius => _summonRadius;
    public int ActiveSummonCount => _activeSummons.Count;
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
                new SummonBehaviorNode(),   // highest priority — summon if can
                new AcquireCombatTargetNode(),
                new ScoredAdvanceNode()
            )
        );
    }

    public void Summon()
    {
        if (_summonPrefabs == null || _summonPrefabs.Count == 0) return;
        if (_activeSummons.Count >= _maxSummons) return;

        // Pick a random prefab from the list
        var prefab = _summonPrefabs[Random.Range(0, _summonPrefabs.Count)];
        if (prefab == null) return;



        for (int i = 0; ActiveSummonCount < MaxSummons; i++)
        {
            // Spawn around self
            Vector2 circle = Random.insideUnitCircle * _summonRadius;
            Vector3 spawnPos = transform.position + new Vector3(circle.x, 0, circle.y);

            EnemyCharacter summoned = Instantiate(prefab, spawnPos, Quaternion.identity);
            _activeSummons.Add(summoned);

            // Clean up list when summon dies
            summoned.OnDeath += OnSummonDied;
        }

    }

    private void OnSummonDied(EnemyCharacter summoned)
    {
        summoned.OnDeath -= OnSummonDied;
        _activeSummons.Remove(summoned);
    }

    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        CurrentTarget.TakeDamage(AttackDamage, this);
    }

    public override void Die(DeathCause cause)
    {
        DisengageCurrentTarget();

        // Kill all active summons when horror dies
        var toKill = new List<EnemyCharacter>(_activeSummons);
        _activeSummons.Clear();
        foreach (var s in toKill)
        {
            if (s == null) continue;
            s.OnDeath -= OnSummonDied;
            s.Die(DeathCause.KilledByCommander);
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