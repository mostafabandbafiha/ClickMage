// SeekCastleNode.cs
// The enemy's one permanent objective, per the design doc: destroy the Castle.
// Runs only when AcquireCombatTargetNode found nothing worth engaging. No
// wandering, no randomness — always the Castle, forever, until it's destroyed.
//
// Known gap (intentionally out of scope for this pass): if the path to the
// Castle is blocked by a Wall, MoveCommand will fail/give up rather than
// finding and destroying the blocking wall (that's the design doc's Siege Unit
// specialty). Goblins will currently just idle and retry next tick in that case.
using ClickMage.Stats;
using UnityEngine;

public class SeekCastleNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not CombatCharacter cc) return false;

        var castle = Castle.Instance;
        if (castle == null) return false;
        var castleTargetable = castle.GetComponent<Targetable>();
        if (castleTargetable == null || !castleTargetable.IsAlive) return false;

        float attackRange = cc.GetStatValue(CommonStats.AttackRange);
        float dist = Vector3.Distance(owner.transform.position, castleTargetable.Position);

        if (dist <= attackRange)
        {
            if (!castleTargetable.TryEngage(owner.gameObject)) return false;
            cc.CurrentTarget = castleTargetable;
            owner.GiveAutonomousCommand(new MoveToTargetCommand(castleTargetable, cc));
            owner.QueueCommand(new AttackCommand(castleTargetable, cc));
            return true;
        }

        owner.GiveAutonomousCommand(new MoveCommand(castleTargetable.Position, attackRange * 0.8f));
        return true;
    }
}