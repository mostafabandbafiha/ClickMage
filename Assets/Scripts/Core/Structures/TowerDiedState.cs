// TowerDiedState.cs — simplified
using ClickMage.StateMachine;

public class TowerDiedState : IState<Tower>
{
    public void Enter(Tower owner)
    {
        owner.ClearTarget();
        // Optionally play death VFX/animation here
        // gameObject stays active so you can show rubble, etc.

        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged += owner.HandleDayNightChanged;
    }

    public void Tick(Tower owner, float deltaTime) { }

    public void Exit(Tower owner)
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged -= owner.HandleDayNightChanged;
    }
}