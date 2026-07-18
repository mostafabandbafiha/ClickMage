// EnemyCharacter.cs — clean abstract base, no assumptions about behavior

using ClickMage.Interface;
using UnityEngine;

public abstract class EnemyCharacter : CombatCharacter
{
    public enum DeathCause { KilledByCommander, KilledByPlayer, Retreated }

    public event System.Action<EnemyCharacter> OnDeath;

    protected override bool IsAutonomous => true;

    [Header("Goal-Directed AI")]
    [Tooltip("Max distance to a combat target before giving up on it and resuming the march toward the Castle.")]
    [SerializeField] private float aggroRadius = 12f;
    [Tooltip("Whether this enemy will engage a Hero it encounters within aggro radius.")]
    [SerializeField] private bool engageHeroes = true;
    [Tooltip("Whether this enemy will fight back against a Tower currently attacking it.")]
    [SerializeField] private bool engageTowersAttackingMe = true;

    public float AggroRadius => aggroRadius;
    public bool EngagesHeroes => engageHeroes;
    public bool EngagesTowersAttackingMe => engageTowersAttackingMe;

    [Tooltip("Small random heading skew (degrees), rolled once at spawn. Only ever breaks ties between equally-scored empty cells in ScoredAdvanceNode — never strong enough to override an actual target. Keeps a crowd from quietly re-funneling into the same empty cell once nothing's left to smash.")]
    [SerializeField] private float personalHeadingBiasRange = 15f;
    public float PersonalHeadingBiasDegrees { get; private set; }


    [Header("Retreat")]
    public Vector3 RetreatDestination { get; set; }

    protected override void Awake()
    {
        base.Awake();
        PersonalHeadingBiasDegrees = Random.Range(-personalHeadingBiasRange, personalHeadingBiasRange);
    }

    // Subclasses set their own speed, BT, stats — no assumptions here

    // Subclasses override to define what dying means for them
    public virtual void Die(DeathCause cause)
    {
        OnDeath?.Invoke(this);
        DisengageCurrentTarget();
        Destroy(gameObject);
    }

    public virtual ICommand<BaseCharacter> CreateAttackCommand(Targetable target)
    {
        return new AttackCommand(target, this);
    }
}