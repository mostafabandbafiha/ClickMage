// ReturnToGuardPositionNode.cs
// Sends the character back to its guard position (spawn point, or the last spot
// the player explicitly moved it to) once it has drifted further than
// GuardReturnThreshold — e.g. after chasing an enemy, finishing a harvest run,
// or wandering off. Place it AFTER job/combat nodes and BEFORE WanderBehaviorNode
// in the selector so units settle back into their post before idling nearby.
using UnityEngine;

public class ReturnToGuardPositionNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        float dist = Vector3.Distance(owner.transform.position, owner.GuardPosition);
        if (dist <= owner.GuardReturnThreshold) return false;

        owner.GiveAutonomousCommand(new MoveCommand(owner.GuardPosition, owner.GuardReturnThreshold * 0.5f));
        return true;
    }
}