// AttackSeekBehaviorNode.cs — generic "find nearest valid target, engage it" node.
// Was EnemySeekBehaviorNode; works for any CombatCharacter (Goblin, Hero, ...).
public class AttackSeekBehaviorNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not CombatCharacter combatant) return false;
        var target = combatant.FindNearestTarget();
        if (target == null || !target.IsAlive) return false;
        if (!target.TryEngage(combatant.gameObject)) return false;
        owner.GiveAutonomousCommand(new MoveToTargetCommand(target, combatant));
        owner.QueueCommand(new AttackCommand(target, combatant));
        return true;
    }
}