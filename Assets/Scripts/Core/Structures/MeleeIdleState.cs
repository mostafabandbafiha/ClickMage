using ClickMage.StateMachine;

public class MeleeIdleState : IState<Tower>
{
    private const float ScanInterval = 0.5f;
    private float _scanTimer;
    private MeleeTower _meleeTower;


    public void Enter(Tower owner)
    {
        if (owner is MeleeTower meleeTower)
        {
            _meleeTower = meleeTower;
            _meleeTower.ClearTarget();
            _scanTimer = 0f;
        }
    }

    public void Tick(Tower owner, float deltaTime)
    {
        _scanTimer += deltaTime;
        if (_scanTimer < ScanInterval) return;
        _scanTimer = 0f;

        if (_meleeTower.HasEnemiesInRange())
            _meleeTower.StateMachine.ChangeState(new MeleeAttackingState());
    }

    public void Exit(Tower owner)
    {

    }
}