// MoveCommand.cs
using System;
using UnityEngine;
using UnityEngine.AI;
public class MoveCommand : ICommand<BaseCharacter>
{
    private const float StuckGiveUpSeconds = 3f;
    private const float StuckProgressThreshold = 0.05f;

    private readonly Vector3 _destination;
    private readonly float _stoppingDistance;
    private float _stuckTimer;
    private float _bestRemainingDistance = float.MaxValue;
    public event EventHandler CanExecuteChanged;
    public bool IsComplete { get; private set; }

    /// <summary>Exposed so callers (e.g. HeroCharacter) can remember where a player sent this unit.</summary>
    public Vector3 Destination => _destination;

    public MoveCommand(Vector3 destination, float stoppingDistance = 0.5f)
    {
        _destination = destination;
        _stoppingDistance = stoppingDistance;
    }
    public void Start(BaseCharacter character)
    {
        character.ClearBlocked();

        var agent = character.Agent;
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning("[MoveCommand] Agent is not on NavMesh.");
            IsComplete = true;
            return;
        }
        float[] sampleRadii = { 1f, 3f, 6f, 10f };
        NavMeshHit hit;
        bool found = false;
        foreach (var radius in sampleRadii)
        {
            if (NavMesh.SamplePosition(_destination, out hit, radius, agent.areaMask))
            {
                agent.stoppingDistance = _stoppingDistance;
                character.SetDestination(hit.position);
                Debug.Log($"[MoveCommand] Sampled at radius {radius}, moving to {hit.position}");
                found = true;
                character.StateMachine.ChangeState(new CharacterMovingState());
                break;
            }
        }
        if (!found)
        {
            Debug.LogWarning($"[MoveCommand] Could not project {_destination} onto NavMesh after all attempts.");
            IsComplete = true;
        }
    }
    public void Tick(BaseCharacter character, float deltaTime)
    {
        var agent = character.Agent;
        if (agent.pathPending) return;

        // pathStatus == PathInvalid is the reliable "no path exists" signal.
        // agent.hasPath can read false for a frame or two after pathPending
        // resolves, while Unity is still finalizing a longer path's corners —
        // treating that as "no path" aborted every long-distance player move
        // before it ever started (short autonomous hops resolve within a
        // single frame, so this race never showed up for those).
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Finish(character, blocked: true);
            return;
        }

        // A partial path means NavMesh could only get us part-way to the real
        // destination (typically an obstacle — e.g. a wall — blocks the rest).
        // remainingDistance in that case is measured against the path's own
        // truncated endpoint, not the real destination, so it can read as
        // "arrived" the instant we reach the obstacle even though we're nowhere
        // near where we were actually asked to go. Once the agent has actually
        // settled on that partial path, treat it as blocked, not arrived.
        if (agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            bool settled = agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude < 0.02f;
            if (settled)
            {
                Finish(character, blocked: true);
                return;
            }
        }
        else
        {
            bool arrivedByDistance = agent.remainingDistance <= _stoppingDistance;
            bool agentStopped = agent.velocity.sqrMagnitude < 0.02f;
            if (arrivedByDistance && agentStopped)
            {
                Finish(character, blocked: false);
                return;
            }
        }

        // Give up if we're not making real progress — e.g. blocked by an
        // obstacle the NavMesh routes toward but can't actually get past.
        if (agent.remainingDistance < _bestRemainingDistance - StuckProgressThreshold)
        {
            _bestRemainingDistance = agent.remainingDistance;
            _stuckTimer = 0f;
        }
        else
        {
            _stuckTimer += deltaTime;
            if (_stuckTimer >= StuckGiveUpSeconds)
            {
                Finish(character, blocked: true);
            }
        }
    }
    public void Cancel()
    {
        IsComplete = true;
    }

    private void Finish(BaseCharacter character, bool blocked)
    {
        IsComplete = true;
        character.StopMoving();
        character.StateMachine.ChangeState(new CharacterIdleState());
        if (blocked) character.MarkBlocked();
    }
}