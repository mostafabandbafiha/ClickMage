// RestBehaviorNode.cs
using UnityEngine;

public class RestBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private const float InteractionRange = 2f;
    private const float EnergyThreshold = 0.7f;

    public bool Execute(BaseCharacter owner)
    {
        // Check if we should rest
        var needsManager = owner.GetComponent<CharacterNeedsManager>();
        if (needsManager == null) return false;

        var energy = needsManager.GetNeed(NeedType.Energy);
        if (energy == null || energy.NormalizedValue > EnergyThreshold)
            return false;

        // Check if a rest point (WorldPoint, type Rest) is available near the character's
        // guard position (not wherever it currently happens to be, so units don't trek
        // across the map to rest).
        if (WorldPointManager.Instance == null) return false;
        var restPoint = WorldPointManager.Instance.GetNearestAvailable(owner.GuardPosition, owner.ActivityRadius, WorldPointType.Rest);
        if (restPoint == null) return false;

        // Claim the spot
        if (!restPoint.TryClaim(owner))
        {
            //Debug.LogWarning($"[RestBehaviorNode] {owner.name} could not claim rest point.");
            return false;
        }

        // Issue commands
        Vector3 sitPosition = restPoint.GetSitPosition(owner);
        float distance = Vector3.Distance(owner.transform.position, sitPosition);

        if (distance > InteractionRange)
            owner.GiveAutonomousCommand(new MoveCommand(sitPosition, InteractionRange));

        owner.GiveAutonomousCommand(new RestCommand(restPoint));

        //Debug.Log($"[RestBehaviorNode] {owner.name} heading to rest (energy: {energy.NormalizedValue:P0})");
        return true;
    }
}