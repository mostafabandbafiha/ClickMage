// HeroCharacter.cs
// A player-commandable hero that stays out at night and engages enemies automatically.
//
// Key differences from GathererCharacter / VillagerCharacter:
//   • StaysOutAtNight stat (> 0) suppresses the GoHome() call from HouseStructure.
//   • Has a house but goes home ONLY when the player explicitly commands it.
//   • Behavior tree: auto-attack enemies in aggro range → wander as fallback.
//   • Player right-clicks ground → MoveCommand (existing CameraController path).
//   • Player right-clicks enemy → ContextMenuManager opens menu; menu issues
//     AttackEnemyCommand, which overrides whatever the BT queued.

using ClickMage.Stats;

public class HeroCharacter : CombatCharacter
{
    protected override bool IsAutonomous => false;

    protected override void Awake()
    {
        base.Awake();
        // Every other character sets Agent.speed from its stat/data in Awake — this was
        // missing here, so the Hero's NavMeshAgent used whatever default sat on the
        // Inspector (often 0), causing the Walk animation to play while the character
        // physically stood still.
        Agent.speed = GetStatValue(CommonStats.MoveSpeed);
    }

    protected override BehaviorTree<BaseCharacter> BuildBehaviorTree()
    {
        return new BehaviorTree<BaseCharacter>(
            new SelectorNode<BaseCharacter>(
                new HeroSeekBehaviorNode(),          // 1. attack if enemy in range
                new ReturnToGuardPositionNode(),     // 2. head back to post if drifted (e.g. after a chase)
                new WanderBehaviorNode()             // 3. idle wander near guard position
            )
        );
    }

    public override void OnAttack()
    {
        if (CurrentTarget == null || !CurrentTarget.IsAlive) return;
        CurrentTarget.TakeDamage(GetStatValue(CommonStats.Damage), this);
    }

}