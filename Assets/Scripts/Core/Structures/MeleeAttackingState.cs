using ClickMage.StateMachine;
using UnityEngine;

public class MeleeAttackingState : IState<Tower>
{
    private static readonly int SpinBool = Animator.StringToHash("IsSpinning");
    private MeleeTower _meleeTower;

    public void Enter(Tower owner)
    {
        if (owner is MeleeTower meleeTower)
        {
            _meleeTower = meleeTower;
            meleeTower.SetSpinning(true);
        }

    }

    public void Tick(Tower owner, float deltaTime)
    {

        // Check if anything is still in range — pure distance, no target lock
        if (!_meleeTower.HasEnemiesInRange())
        { 
            _meleeTower.StateMachine.ChangeState(new TowerIdleState());
        }
     

    }

    public void Exit(Tower owner)
    {
        _meleeTower.SetSpinning(false); 
    }
}