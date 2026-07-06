// ArcherSeekBehaviorNode.cs
public class ArcherSeekBehaviorNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not EnemyCharacter archer) return false;

        var target = archer.FindNearestTarget();
        if (target == null || !target.IsAlive) return false;

        if (!target.TryEngage(archer.gameObject)) return false;

        owner.GiveAutonomousCommand(new MoveToTargetCommand(target, archer));
        owner.QueueCommand(new RangedAttackCommand(target, archer));
        return true;
    }
}