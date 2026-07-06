// -------------------------------------------------------
// Complete - deposits outputs, loops back
// -------------------------------------------------------

using ClickMage.Factories;
using ClickMage.StateMachine;

public sealed class FactoryCompleteState : IState<Factory>
{
    public void Enter(Factory f)
    {
        f.ProduceOutputs();
        f.ChangeState(new FactoryIdleState()); // Immediately re-evaluate
    }

    public void Tick(Factory f, float dt) { }
    public void Exit(Factory f) { }
}
