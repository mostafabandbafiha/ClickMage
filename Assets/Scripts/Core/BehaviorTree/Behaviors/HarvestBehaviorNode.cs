// HarvestBehaviorNode.cs
using UnityEngine;

public class HarvestBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private const float SearchRadius = 30f;
    private const float InteractionRange = 4f;
    private const float MinEnergyThreshold = 0.2f;
    private const float MinNodeHPPercent = 0.3f;

    public bool Execute(BaseCharacter owner)
    {
        // Must be a harvester
        if (owner is not HarvesterCharacter harvester || !harvester.CanHarvest)
            return false;

        // Check energy
        var needsManager = owner.GetComponent<CharacterNeedsManager>();
        if (needsManager != null)
        {
            var energy = needsManager.GetNeed(NeedType.Energy);
            if (energy != null && energy.NormalizedValue < MinEnergyThreshold)
                return false;
        }

        // Find nearest harvestable resource
        var nodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        ResourceNode closest = null;
        float minDist = float.MaxValue;

        foreach (var node in nodes)
        {
            if (!node.CanHarvest()) continue;
            if (harvester.IsNodeBlacklisted(node)) continue;
            if (node.HPPercent < MinNodeHPPercent) continue;

            float dist = Vector3.Distance(owner.transform.position, node.transform.position);
            if (dist > SearchRadius) continue;

            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }

        if (closest == null) return false;

        // Issue commands
        if (minDist > InteractionRange) { 
            owner.GiveAutonomousCommand(new MoveCommand(closest.transform.position, InteractionRange));
        }

        owner.GiveAutonomousCommand(new HarvestCommand(closest, InteractionRange));

        //Debug.Log($"[HarvestBehaviorNode] {harvester.name} moving to harvest {closest.name}");
        return true;
    }
}
