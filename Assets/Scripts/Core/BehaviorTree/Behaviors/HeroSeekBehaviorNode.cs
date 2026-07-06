// HeroSeekBehaviorNode.cs — like AttackSeekBehaviorNode, but a hero should only
// engage targets within its DetectionRadius stat (it doesn't go hunting across
// the map the way a roaming enemy might).
using ClickMage.Stats;
using UnityEngine;
public class HeroSeekBehaviorNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not HeroCharacter hero) return false;

        var target = hero.FindNearestTarget();
        if (target == null || !target.IsAlive) return false;

        float detectionRadius = hero.GetStatValueSafe(CommonStats.DetectionRadius);
        float dist = Vector3.Distance(hero.transform.position, target.transform.position);
        if (dist > detectionRadius) return false;

        if (!target.TryEngage(hero.gameObject)) return false;

        owner.GiveAutonomousCommand(new MoveToTargetCommand(target, hero));
        owner.QueueCommand(new AttackCommand(target, hero));
        return true;
    }
}