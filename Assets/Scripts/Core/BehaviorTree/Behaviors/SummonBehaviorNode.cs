// SummonBehaviorNode.cs
using UnityEngine;

public class SummonBehaviorNode : IBehaviorNode<BaseCharacter>
{
    private float _summonCooldown = 8f;
    private float _cooldownTimer = 0f;

    public bool Execute(BaseCharacter owner)
    {
        if (owner is not AbyssalHorror horror) return false;

        // Cooldown not ready
       /* _cooldownTimer += Time.deltaTime;
        if (_cooldownTimer < _summonCooldown) return false;*/

        // Already at max summons
        if (horror.ActiveSummonCount >= horror.MaxSummons) return false;

        // No prefabs assigned
        if (horror.SummonPrefabs == null || horror.SummonPrefabs.Count == 0) return false;

        _cooldownTimer = 0f;
        horror.GiveAutonomousCommand(new SummonCommand(horror));
        return true;
    }
}