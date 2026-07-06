// -------------------------------------------------------
// Waiting for input resources
// -------------------------------------------------------

using ClickMage.Factories;
using ClickMage.StateMachine;
using UnityEngine;

public sealed class FactoryWaitingForInputState : IState<Factory>
{
    public void Enter(Factory f) =>
        Debug.Log($"[{f.name}] Waiting for input resources.");

    public void Tick(Factory f, float dt)
    {
        if (f.HasRequiredInputs())
            f.ChangeState(new FactoryIdleState());
    }

    public void Exit(Factory f) { }
}