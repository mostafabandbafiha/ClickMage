// RetreatNode.cs
// Terminal fallback once nothing worth marching toward remains — checked
// AFTER AcquireCombatTargetNode, so an enemy will still fight a hero it
// stumbles into on the way out (heroes are intentionally excluded from the
// "anything left" check itself; enemies only retreat over structures).
public class RetreatNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not EnemyCharacter enemy) return false;
        if (TargetRegistry.Instance == null) return false;
        if (TargetRegistry.Instance.HasLivingStructures(Faction.Player)) return false;

        enemy.GiveAutonomousCommand(new RetreatCommand(enemy.RetreatDestination));
        return true;
    }
}