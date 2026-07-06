// HeroDiedState.cs
using ClickMage.Animation;
using ClickMage.StateMachine;

public class HeroDiedState : IState<BaseCharacter>
{
    public void Enter(BaseCharacter owner)
    {
        owner.StopMoving();
        //owner.ClearTarget();
        owner.Animator?.PlayAnimation(AnimationKeys.Clips.SitIdle); // your kneel anim key

        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged += owner.HandleDayNightChanged;
    }

    public void Tick(BaseCharacter owner, float deltaTime) { }

    public void Exit(BaseCharacter owner)
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged -= owner.HandleDayNightChanged;
    }
}
