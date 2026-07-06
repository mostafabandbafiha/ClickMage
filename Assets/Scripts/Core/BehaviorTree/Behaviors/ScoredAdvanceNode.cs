// ScoredAdvanceNode.cs
// Replaces DestroyBlockingWallNode + SeekCastleNode with one proactive, local
// decision, per the "army of savages, not a single-file funnel" design
// discussion: each enemy scores a small forward cone of grid cells every tick
// (empty=-1, destructible structure=+5, Castle=+10) and either engages the
// best-scoring structure or steps toward the best-scoring empty cell.
//
// Deliberately a local greedy look, NOT a path search: summing scores over a
// full path to the Castle would make every enemy converge on the one
// objectively-best route (the same problem NavMesh's own optimal pathing
// caused). Because each enemy only ever reasons about the handful of cells
// immediately around itself, two enemies in different spots naturally make
// different local decisions — spreading out is a side effect of the approach,
// not something we have to force.
//
// Two refinements on top of the flat category scores:
// - Alignment bonus: a cell dead ahead scores higher than one at the cone's
//   edge, additively (not multiplicatively — multiplying EmptyScore's negative
//   value by an alignment factor would flip which empty cells look best).
//   This is what makes an enemy prefer a wall genuinely in its path over one
//   just incidentally nearby off to the side.
// - Focus-fire bonus: a heavily-damaged structure scores higher than a fresh
//   one, so enemies gang up and finish off an already-cracked wall instead of
//   spreading damage across many — an emergent "walls fall one at a time"
//   cascade with no coordination needed, just everyone reading the same HP stat.
//
// "Commit until destroyed, don't flip-flop" needs no special code: once this
// engages a structure (MoveToTargetCommand + AttackCommand queued), the
// behavior tree structurally can't re-tick until that command pair finishes —
// the same "busy = do nothing" guarantee used everywhere else in the tree.
using ClickMage.Entities;
using ClickMage.Stats;
using UnityEngine;

public class ScoredAdvanceNode : IBehaviorNode<BaseCharacter>
{
    private const int ConeCellRadius = 3;       // ~3-cell-wide forward look, per design
    private const float ConeHalfAngleDegrees = 35f;
    private const float StructureScore = 5f;
    private const float CastleScore = 10f;
    private const float EmptyScore = -1f;
    private const float DistanceTiebreakWeight = 0.01f; // prefer nearer cells among equal scores
    private const float AlignmentBonusWeight = 2f;       // max bonus for a cell dead ahead
    private const float FocusFireBonusWeight = 3f;       // max bonus for a structure at ~0 HP

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not EnemyCharacter enemy) return false;

        var grid = BuildModeController.Instance != null ? BuildModeController.Instance.GetGridData : null;
        if (grid == null) return false;

        var castle = Castle.Instance;
        var castleTargetable = castle != null ? castle.GetComponent<Targetable>() : null;
        if (castleTargetable == null || !castleTargetable.IsAlive) return false;

        Vector3 toCastle = castleTargetable.Position - owner.transform.position;
        toCastle.y = 0f;
        if (toCastle.sqrMagnitude < 0.01f)
            return EngageCastle(enemy, castleTargetable);

        Vector3 forward = Quaternion.Euler(0, enemy.PersonalHeadingBiasDegrees, 0) * toCastle.normalized;

        if (!grid.WorldToGrid(owner.transform.position, out int myCol, out int myRow)) return false;

        float bestScore = float.NegativeInfinity;
        Vector2Int bestCell = default;
        bool bestIsCastle = false;
        GameObject bestStructure = null;
        bool foundAny = false;

        for (int dc = -ConeCellRadius; dc <= ConeCellRadius; dc++)
        {
            for (int dr = -ConeCellRadius; dr <= ConeCellRadius; dr++)
            {
                if (dc == 0 && dr == 0) continue;
                int col = myCol + dc;
                int row = myRow + dr;
                if (!grid.IsInBounds(col, row)) continue;

                Vector3 cellWorld = grid.GridToWorld(col, row);
                Vector3 dir = cellWorld - owner.transform.position;
                dir.y = 0f;
                float dist = dir.magnitude;
                if (dist < 0.01f) continue;

                float angle = Vector3.Angle(forward, dir);
                if (angle > ConeHalfAngleDegrees) continue; // outside forward cone

                var cell = grid.GetCell(col, row);
                bool isCastleCell = cell.Structure == castle.gameObject;
                GameObject structureHere = null;
                float score;

                if (isCastleCell)
                {
                    score = CastleScore;
                }
                else if (cell.IsOccupied && cell.Structure != null)
                {
                    score = StructureScore + GetFocusFireBonus(cell.Structure);
                    structureHere = cell.Structure;
                }
                else
                {
                    score = EmptyScore;
                }

                // Additive, not multiplicative — a cell dead ahead (angle=0) gets the
                // full bonus, one at the cone's edge gets none. Additive keeps the
                // sign of EmptyScore intact; multiplying a negative value by a
                // shrinking alignment factor would backwards-prefer off-angle cells.
                float alignment = 1f - (angle / ConeHalfAngleDegrees);
                score += alignment * AlignmentBonusWeight;

                score -= dist * DistanceTiebreakWeight;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = new Vector2Int(col, row);
                    bestIsCastle = isCastleCell;
                    bestStructure = structureHere;
                    foundAny = true;
                }
            }
        }

        if (!foundAny) return false;

        if (bestIsCastle)
            return EngageCastle(enemy, castleTargetable);

        if (bestStructure != null)
        {
            var targetable = bestStructure.GetComponent<Targetable>();
            if (targetable != null && targetable.IsAlive)
                return EngageStructure(enemy, targetable);
        }

        // Best option in view was an empty cell — take one step toward it.
        Vector3 stepTarget = grid.GridToWorld(bestCell.x, bestCell.y);
        enemy.GiveAutonomousCommand(new MoveCommand(stepTarget, 0.5f));
        return true;
    }

    /// <summary>0 for a full-health structure, up to FocusFireBonusWeight as it
    /// nears 0 HP. Returns 0 if the structure has no Health/MaxHealth stats —
    /// never penalizes, only ever adds an incentive to finish off a cracked wall.</summary>
    private static float GetFocusFireBonus(GameObject structure)
    {
        var entity = structure.GetComponent<BaseEntity>();
        if (entity == null) return 0f;
        if (!entity.HasStat(CommonStats.Health) || !entity.HasStat(CommonStats.MaxHealth)) return 0f;

        float maxHp = entity.GetStatValue(CommonStats.MaxHealth);
        if (maxHp <= 0f) return 0f;

        float healthFraction = Mathf.Clamp01(entity.GetStatValue(CommonStats.Health) / maxHp);
        return (1f - healthFraction) * FocusFireBonusWeight;
    }

    private static bool EngageCastle(EnemyCharacter enemy, Targetable castleTargetable)
    {
        if (!castleTargetable.TryEngage(enemy.gameObject)) return false;
        enemy.CurrentTarget = castleTargetable;
        enemy.GiveAutonomousCommand(new MoveToTargetCommand(castleTargetable, enemy));
        enemy.QueueCommand(new AttackCommand(castleTargetable, enemy));
        return true;
    }

    private static bool EngageStructure(EnemyCharacter enemy, Targetable targetable)
    {
        if (!targetable.TryEngage(enemy.gameObject)) return false;
        enemy.CurrentTarget = targetable;
        enemy.GiveAutonomousCommand(new MoveToTargetCommand(targetable, enemy));
        enemy.QueueCommand(new AttackCommand(targetable, enemy));
        return true;
    }
}