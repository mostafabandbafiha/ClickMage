// -------------------------------------------------------
// Idle - evaluates what to do next
// -------------------------------------------------------

using ClickMage.Factories;
using ClickMage.StateMachine;

public sealed class FactoryIdleState : IState<Factory>
{
    public void Enter(Factory f) { }

    public void Tick(Factory f, float dt)
    {
        if (f.CurrentRecipe == null)
        {
            f.ChangeState(new FactoryNoRecipeState());
            return;
        }

        if (!f.HasRequiredInputs())
        {
            f.ChangeState(new FactoryWaitingForInputState());
            return;
        }

        if (!f.HasOutputSpace())
        {
            f.ChangeState(new FactoryOutputFullState());
            return;
        }

        // All conditions met - start producing
        f.ChangeState(new FactoryProductionState());
    }

    public void Exit(Factory f) { }
}
