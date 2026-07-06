// DepositBehaviorNode.cs
using UnityEngine;

public class DepositBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private const float InteractionRange = 2f;

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not GathererCharacter gatherer) return false;
        if (!gatherer.IsCarrying) return false;

        if (Warehouse.Instance == null) return false;

        Vector3 depositPos = Warehouse.Instance.DepositPosition;
        float dist = Vector3.Distance(owner.transform.position, depositPos);

        if (dist > InteractionRange)
        {
            owner.GiveAutonomousCommand(new MoveCommand(depositPos, InteractionRange));
            owner.QueueCommand(new DepositCommand());
        }
        else
        {
            owner.GiveAutonomousCommand(new DepositCommand());
        }

        Debug.Log($"[DepositBehaviorNode] {gatherer.name} heading to warehouse.");
        return true;
    }
}