// HeroAttackBehaviorNode.cs
// Exact clone of HarvestBehaviorNode with enemy target instead of resource node.

using ClickMage.Stats;
using UnityEngine;

public class HeroAttackBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private readonly float _aggroRadius;

    public HeroAttackBehaviorNode(float aggroRadius)
    {
        _aggroRadius = aggroRadius;
    }

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not HeroCharacter hero) return false;

        // Already attacking — don't interrupt
        if (owner.StateMachine.CurrentState is HeroAttackState) return true;

        // Commands already queued — let them run
        if (!owner.CommandQueue.IsEmpty()) return true;

        float attackRange = hero.HasStat(CommonStats.AttackRange)
            ? hero.GetStatValue(CommonStats.AttackRange) : 2f;

        var target = TargetRegistry.Instance?.GetNearestInRange(
            Faction.Enemy, owner.transform.position, _aggroRadius);

        if (target == null) return false;

        float dist = Vector3.Distance(owner.transform.position, target.transform.position);

        if (dist > attackRange)
            owner.GiveAutonomousCommand(new MoveCommand(target.transform.position, attackRange));

        owner.GiveAutonomousCommand(new AttackCommand(target, hero));

        return true;
    }
}