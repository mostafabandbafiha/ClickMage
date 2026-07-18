// AcquireCombatTargetNode.cs
// "Is there something worth engaging?" from the enemy AI design doc. Only runs
// when the enemy isn't already busy (BehaviorTree doesn't get ticked while a
// command is in flight — see BaseCharacter.TryTickBehaviorTree). Looks for
// temporary combat targets in priority order and, if one is found, engages it
// via the normal Move+Attack command pair. Combat targets are temporary — once
// the AttackCommand/MoveToTargetCommand pair ends (target dies, flees beyond
// aggro radius, or becomes unreachable), the enemy naturally falls through to
// SeekCastleNode on the next tick. Config (which categories to engage, aggro
// radius) lives on EnemyCharacter so new enemy types can reuse this node just
// by changing their Inspector values, per the design doc's "same tree,
// different priorities" principle.
using UnityEngine;

public class AcquireCombatTargetNode : IBehaviorNode<BaseCharacter>
{
    public bool Execute(BaseCharacter owner)
    {
        if (owner is not EnemyCharacter enemy) return false;

        // Priority: Hero within aggro radius, then a Tower currently attacking me,
        // then whatever's nearest and engageable in range.
        Targetable target = null;

        if (enemy.EngagesHeroes)
            target = FindNearestHero(enemy);

        if (target == null && enemy.EngagesTowersAttackingMe)
            target = FindTowerAttackingMe(enemy);

        if (target == null)
            target = TargetRegistry.Instance.GetNearestEngageableInRange(
                Faction.Player, enemy.transform.position, enemy.AggroRadius);

        if (target == null) return false;

        enemy.SetCombatTarget(target);
        enemy.GiveAutonomousCommand(new MoveToTargetCommand(target, enemy));
        enemy.QueueCommand(enemy.CreateAttackCommand(target));

        return true;
    }


    private static Targetable FindNearestHero(EnemyCharacter enemy)
    {
        // Castle and Towers are also Faction.Player, so we can't just take
        // TargetRegistry's global nearest — filter to Hero-owned Targetables
        // specifically, then bound by aggro radius and engage capacity.
        if (TargetRegistry.Instance == null) return null;
        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        float aggroSq = enemy.AggroRadius * enemy.AggroRadius;
        foreach (var t in TargetRegistry.Instance.GetTargets(Faction.Player))
        {
            if (t == null || !t.IsAlive) continue;
            if (!t.HasCapacity) continue;
            if (t.GetComponent<HeroCharacter>() == null) continue;
            float sq = (t.Position - enemy.transform.position).sqrMagnitude;
            if (sq > aggroSq) continue;
            if (sq < nearestSq) { nearestSq = sq; nearest = t; }
        }
        return nearest;
    }

   private static Targetable FindTowerAttackingMe(EnemyCharacter enemy)
    {
        var myTargetable = enemy.GetComponent<Targetable>();
        if (myTargetable == null) return null;
        var towers = Object.FindObjectsByType<Tower>(FindObjectsSortMode.None);
        foreach (var tower in towers)
        {
            if (tower == null) continue;
            if (tower.CurrentTarget == myTargetable)
            {
                var t = tower.GetComponent<Targetable>();
                if (t != null && t.HasCapacity) return t; // NEW capacity check
            }
        }
        return null;
    }
}