// -------------------------------------------------------
// Production - ticks timer
// -------------------------------------------------------

using ClickMage.Factories;
using ClickMage.StateMachine;

public sealed class FactoryProductionState : IState<Factory>
{
    public void Enter(Factory f)
    {
        f.ProductionTimer = 0f;
        f.ConsumeInputs();
        f.NotifyProductionStarted();
    }

    public void Tick(Factory f, float dt)
    {
        f.ProductionTimer += dt * f.GetProductionSpeed();

        float craftTime = f.CurrentRecipe.GetCraftTime(); // speed already applied via stat
        if (f.ProductionTimer >= craftTime)
            f.ChangeState(new FactoryCompleteState());
    }

    public void Exit(Factory f) { }
}
