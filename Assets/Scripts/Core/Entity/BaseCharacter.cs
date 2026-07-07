using UnityEngine;
using UnityEngine.AI;
using ClickMage.StateMachine;
using ClickMage.Animation;
using ClickMage.Entities;
using ClickMage.Stats;
using System.Collections.Generic;
using ClickMage.Interface;

public abstract class BaseCharacter : BaseEntity, ICommandable<BaseCharacter>
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Autonomous Behavior")]
    [SerializeField] private float playerControlTimeout = 30f;
    [SerializeField] private float behaviorTreeTickInterval = 3f;

    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Guard Position / Routine")]
    [Tooltip("How far the character may drift from its guard position before ReturnToGuardPositionNode sends it back.")]
    [SerializeField] private float guardReturnThreshold = 3f;
    [Tooltip("Max distance from the guard position to search for wander points and rest points.")]
    [SerializeField] private float activityRadius = 15f;
    [Tooltip("Random pause range (seconds) between wander moves.")]
    [SerializeField] private float wanderIdleMin = 2f;
    [SerializeField] private float wanderIdleMax = 5f;

    // ── Runtime ───────────────────────────────────────────────────────────
    private CommandQueue<BaseCharacter> _commandQueue;
    private bool _isPlayerControlled = false;
    private float _playerControlTimer = 0f;
    private float _behaviorTreeTimer = 0f;

    // Set by OnCommandQueueEmptied (fired synchronously from inside
    // _commandQueue.Tick, mid-Update) and consumed at the TOP of the next
    // Update() call. Without this, a finished command could chain straight
    // into the next one within the same Update() — arrive, go Idle, and
    // immediately get yanked into a new move before a single frame ever
    // renders. That's what caused the "instant turn-around" / animation-skip
    // bug: the character never actually got a real Idle frame to react in.
    private bool _pendingBehaviorTreeTick;

    // ── Public API ────────────────────────────────────────────────────────
    public float RotationSpeed => rotationSpeed;
    public bool IsPlayerControlled => _isPlayerControlled;

    /// <summary>
    /// The character's "home" anchor. Defaults to spawn position; updated whenever
    /// the player issues a direct MoveCommand (via GiveCommand). Jobs (combat, harvest,
    /// gather) may pull the character away temporarily; ReturnToGuardPositionNode and
    /// WanderBehaviorNode both use this as their reference point so units settle back
    /// into their post instead of drifting across the map.
    /// </summary>
    public Vector3 GuardPosition { get; private set; }
    public float GuardReturnThreshold => guardReturnThreshold;
    public float ActivityRadius => activityRadius;
    public float WanderIdleMin => wanderIdleMin;
    public float WanderIdleMax => wanderIdleMax;

    /// <summary>Set by MoveCommand/MoveToTargetCommand when they give up because no
    /// real progress was being made (stuck against an obstacle), as opposed to
    /// completing normally or a target fleeing out of range. Behavior tree nodes
    /// can read this to decide "was I just blocked?" without needing to know which
    /// command was running. Cleared automatically when a new move command starts.</summary>
    public bool WasBlocked { get; private set; }
    public void MarkBlocked() => WasBlocked = true;
    public void ClearBlocked() => WasBlocked = false;

    public NavMeshAgent Agent { get; private set; }
    public CommandQueue<BaseCharacter> CommandQueue => _commandQueue;
    public IAnimatable Animator { get; private set; }
    public StateMachine<BaseCharacter> StateMachine { get; private set; }
    public Vector3 TargetPosition { get; set; }
    public Targetable Targetable { get; private set; }

    protected virtual BehaviorTree<BaseCharacter> BuildBehaviorTree() => null;

    /// <summary>
    /// Override to true in fully autonomous characters (enemies).
    /// Bypasses the player-control timer so the behavior tree always runs.
    /// </summary>
    protected virtual bool IsAutonomous => false;

    private BehaviorTree<BaseCharacter> _behaviorTree;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        Agent = GetComponent<NavMeshAgent>();
        Agent.updateRotation = false;

        Animator = GetComponentInChildren<IAnimatable>();
        StateMachine = new StateMachine<BaseCharacter>(this);

        _commandQueue = new CommandQueue<BaseCharacter>();
        _commandQueue.Initialize(this);
        _commandQueue.OnQueueEmptied += OnCommandQueueEmptied;

        StateMachine.ChangeState(new CharacterIdleState());
        Targetable = GetComponent<Targetable>();

        GuardPosition = transform.position;

        _behaviorTree = BuildBehaviorTree();
    }

    // Subclasses can override Start for post-Awake boot logic.
    protected virtual void Start() { }

    protected virtual void Update()
    {
        // Consume any BT re-tick requested by a command that finished LAST frame
        // (see _pendingBehaviorTreeTick). This runs before the queue/state tick
        // below so the character has already rendered at least one real Idle
        // frame before a new command can start.
        if (_pendingBehaviorTreeTick)
        {
            _pendingBehaviorTreeTick = false;
            TryTickBehaviorTree();
        }

        // Player-control timeout (skipped entirely for autonomous characters).
        if (!IsAutonomous && _isPlayerControlled)
        {
            _playerControlTimer -= Time.deltaTime;
            if (_playerControlTimer <= 0f)
            {
                _isPlayerControlled = false;
                TryTickBehaviorTree();
            }
        }

        _commandQueue.Tick(Time.deltaTime);
        StateMachine.Tick(Time.deltaTime);


        // Tick behavior tree periodically.
        // For autonomous characters, always tick regardless of player-control state.
        bool shouldTick = IsAutonomous || !_isPlayerControlled;
        if (shouldTick && _behaviorTree != null)
        {
            _behaviorTreeTimer += Time.deltaTime;
            if (_behaviorTreeTimer >= behaviorTreeTickInterval)
            {
                _behaviorTreeTimer = 0f;
                TryTickBehaviorTree();
            }
        }
    }


    private void OnCommandQueueEmptied()
    {
        if (IsAutonomous || !_isPlayerControlled)
            _pendingBehaviorTreeTick = true;
    }

    private void TryTickBehaviorTree()
    {
        if (_behaviorTree == null) return;
        if (!_commandQueue.IsEmpty()) return;

        // For player-controlled characters, only tick from idle.
        // For autonomous characters (enemies), tick from idle OR seek —
        // so recovery works even if seek falls back without going through idle.
        if (!IsAutonomous && StateMachine.CurrentState is not CharacterIdleState)
            return;

        // Autonomous: only tick if not already in an active enemy state.
        if (IsAutonomous)
        {
            var state = StateMachine.CurrentState;
            if (state is CombatAttackState)
                return; // already doing something — don't interrupt
        }

        _behaviorTree.Tick(this);
    }

    // ── ICommandable ──────────────────────────────────────────────────────

    public virtual void GiveCommand(ICommand<BaseCharacter> command)
    {
        _isPlayerControlled = true;
        _playerControlTimer = playerControlTimeout;

        // A direct player move redefines "home" for this character — jobs and
        // return-to-guard logic will use this new spot from now on.
        if (command is MoveCommand moveCommand)
            GuardPosition = moveCommand.Destination;

        if (StateMachine.CurrentState is CharacterRestingState restingState)
        {
            restingState.ForceExit(this);
            _commandQueue.GiveCommand(command);
            return;
        }

        // Interrupt an in-progress attack (relevant for player-commandable
        // combatants like heroes; never hit for enemies, which don't call this).
        if (StateMachine.CurrentState is CombatAttackState)
        {
            StateMachine.ChangeState(new CharacterIdleState());
            _commandQueue.GiveCommand(command);
            return;
        }

        if (StateMachine.CurrentState is CharacterHarvestingState)
        {
            StateMachine.ChangeState(new CharacterIdleState());
            _commandQueue.GiveCommand(command);
            return;
        }

        _commandQueue.GiveCommand(command);
    }

    public void GiveAutonomousCommand(ICommand<BaseCharacter> command)
        => _commandQueue.QueueCommand(command);

    public void QueueCommand(ICommand<BaseCharacter> command)
        => _commandQueue.QueueCommand(command);

    public void ClearCommands()
        => _commandQueue.Clear();

    // ── Movement ──────────────────────────────────────────────────────────

    public void SetDestination(Vector3 destination)
    {
        if (!Agent.enabled || !Agent.isOnNavMesh) return;
        TargetPosition = destination;
        Agent.isStopped = false;
        Agent.SetDestination(destination);
    }

    public void StopMoving()
    {
        Agent.ResetPath();
    }

    public void ResetPath() => Agent.ResetPath();

    protected override List<BaseStat> BuildStatAssetList() => base.BuildStatAssetList();

    public void GoHome(HouseStructure home)
    {
        if (GetStatValue(CommonStats.StaysOutAtNight) > 0)
        {
            return;
        }
        InterruptCurrentActivity();
        ClearCommands();
        _isPlayerControlled = true;
        _playerControlTimer = float.MaxValue;
        _commandQueue.GiveCommand(new MoveCommand(home.EnterPosition, 1f));
        _commandQueue.QueueCommand(new EnterHouseCommand(home));
    }

    public void LeaveHome(HouseStructure home)
    {
        _isPlayerControlled = false;
        _playerControlTimer = 0f;
        StateMachine.ChangeState(new CharacterIdleState());
        _commandQueue.GiveCommand(new ExitHouseCommand(home));
    }

    private void InterruptCurrentActivity()
    {
        if (StateMachine.CurrentState is CharacterHarvestingState)
        {
            StateMachine.ChangeState(new CharacterIdleState());
            return;
        }
        if (StateMachine.CurrentState is CharacterRestingState restingState)
        {
            restingState.ForceExit(this);
            return;
        }
        if (StateMachine.CurrentState is CharacterCarryState)
        {
            if (this is GathererCharacter gatherer && gatherer.IsCarrying)
                gatherer.DetachItem();
            StateMachine.ChangeState(new CharacterIdleState());
            return;
        }
        if (StateMachine.CurrentState is CharacterPickupState)
        {
            StateMachine.ChangeState(new CharacterIdleState());
            return;
        }
    }


    // Subscribed by HeroDiedState; mirror of Tower.HandleDayNightChanged
    public void HandleDayNightChanged(TimeOfDay timeOfDay)
    {
        if (timeOfDay == TimeOfDay.Day)
        {
            Revive();
        }
    }

    private void Revive()
    {
        SetStatBaseValue(CommonStats.Health, GetStatValueSafe(CommonStats.Health));

        // Reset IsAlive so the registry sees it again
        var targetable = GetComponent<EntityTargetable>();
        if (targetable != null) targetable.ResetAlive();

        StateMachine.ChangeState(new CharacterIdleState());
    }
}