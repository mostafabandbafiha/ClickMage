// GatherItemsBehaviorNode.cs
using UnityEngine;
using System.Linq;

public class GatherItemsBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private const float InteractionRange = 2f;

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not GathererCharacter gatherer) return false;
        if (gatherer.IsCarrying) return false;

        // Find available items
        var items = Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None)
            .Where(p => !p.IsClaimed && gatherer.AcceptsItem(p))
            .ToArray();

        if (items.Length == 0) return false;

        // Pick closest
        WorldItemPickup closest = null;
        float minDist = float.MaxValue;

        foreach (var item in items)
        {
            float dist = Vector3.Distance(owner.transform.position, item.transform.position);
            if (dist < minDist) { minDist = dist; closest = item; }
        }

        if (closest == null) return false;

        // Claim it
        if (!closest.TryClaim())
        {
            //Debug.LogWarning($"[GatherItemsBehaviorNode] {owner.name} lost claim race.");
            return false;
        }

        // Issue commands
        if (minDist > InteractionRange)
            owner.GiveAutonomousCommand(new MoveCommand(closest.transform.position, InteractionRange));

        owner.GiveAutonomousCommand(new PickupCommand(closest));

        //Debug.Log($"[GatherItemsBehaviorNode] {gatherer.name} going for {closest.Stack.Data?.DisplayName}");
        return true;
    }
}
