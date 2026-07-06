// WanderBehaviorNode.cs
// Picks a designer-placed WorldPoint (type: Wander) near the character's
// GuardPosition and moves to it, then idles for a random cooldown before
// wandering again. Falls back to a random offset near the guard position if no
// WorldPoints are registered nearby, so the node still works on scenes that
// haven't been dressed with points yet.
using UnityEngine;

public class WanderBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private const float FallbackWanderRadius = 6f;

    // Per-instance (each character owns its own node instance via BuildBehaviorTree),
    // so this cooldown never leaks between characters.
    private float _nextWanderTime;

    public bool Execute(BaseCharacter owner)
    {
        if (Time.time < _nextWanderTime) return false; // still idling — let the tree fall through

        Vector3 destination = PickDestination(owner);

        owner.GiveAutonomousCommand(new MoveCommand(destination, 0.5f));
        _nextWanderTime = Time.time + Random.Range(owner.WanderIdleMin, owner.WanderIdleMax);
        return true;
    }

    private static Vector3 PickDestination(BaseCharacter owner)
    {
        var point = WorldPointManager.Instance != null
            ? WorldPointManager.Instance.GetRandomPoint(owner.GuardPosition, owner.ActivityRadius, WorldPointType.Wander)
            : null;

        if (point != null)
        {
            Vector2 offset = Random.insideUnitCircle * point.Radius;
            return point.Position + new Vector3(offset.x, 0, offset.y);
        }

        // No WorldPoints nearby — wander loosely around the guard position instead.
        Vector2 fallbackOffset = Random.insideUnitCircle * Mathf.Min(owner.ActivityRadius, FallbackWanderRadius);
        return owner.GuardPosition + new Vector3(fallbackOffset.x, 0, fallbackOffset.y);
    }
}