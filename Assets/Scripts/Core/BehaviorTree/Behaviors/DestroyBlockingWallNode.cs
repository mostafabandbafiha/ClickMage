// DestroyBlockingWallNode.cs
// Design doc's Wall Behavior: "Move toward Castle -> Path blocked? -> Find
// blocking wall -> Destroy wall -> Resume Castle." Only fires when the previous
// move genuinely stalled (BaseCharacter.WasBlocked), not just because a Block
// happens to be sitting nearby while the enemy walks past normally — otherwise
// every enemy would take detours to punch walls that were never in its way.
//
// Detection approach: nearest Block-type Targetable within BlockSearchRadius of
// the enemy's current (stuck) position. Blocks are already registered in
// TargetRegistry under Faction.Player, same as Castle/Towers, so this reuses
// existing infrastructure rather than raycasting or walking the build grid.
// Heuristic, not exact: in a dense wall cluster this could occasionally engage
// a neighboring wall instead of the precise one blocking the path, but for
// ordinary wall lines the nearest one IS the blocker.
using UnityEngine;

public class DestroyBlockingWallNode : IBehaviorNode<BaseCharacter>
{
    private const float BlockSearchRadius = 5f;

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not EnemyCharacter enemy) return false;
        if (!owner.WasBlocked) return false;

        var block = FindNearestBlock(enemy);
        if (block == null)
        {
            // Nothing found to blame — clear the flag so we don't keep retrying
            // this node forever; SeekCastleNode will just try the route again.
            owner.ClearBlocked();
            return false;
        }

        if (!block.TryEngage(enemy.gameObject)) return false;

        owner.ClearBlocked();
        enemy.CurrentTarget = block;
        owner.GiveAutonomousCommand(new MoveToTargetCommand(block, enemy));
        owner.QueueCommand(new AttackCommand(block, enemy));
        return true;
    }

    private static Targetable FindNearestBlock(EnemyCharacter enemy)
    {
        if (TargetRegistry.Instance == null) return null;

        Targetable nearest = null;
        float nearestSq = float.MaxValue;
        float radiusSq = BlockSearchRadius * BlockSearchRadius;

        foreach (var t in TargetRegistry.Instance.GetTargets(Faction.Player))
        {
            if (t == null || !t.IsAlive) continue;
            if (t.GetComponent<Block>() == null) continue; // only walls/blocks, never Castle/Tower

            float sq = (t.Position - enemy.transform.position).sqrMagnitude;
            if (sq > radiusSq) continue;
            if (sq < nearestSq) { nearestSq = sq; nearest = t; }
        }
        return nearest;
    }
}