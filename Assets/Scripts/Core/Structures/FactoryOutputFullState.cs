using ClickMage.Factories;
using ClickMage.StateMachine;
using UnityEngine;

public sealed class FactoryOutputFullState : IState<Factory>
{
    public void Enter(Factory f) =>
        Debug.Log($"[{f.name}] Output storage full.");

    public void Tick(Factory f, float dt)
    {
        if (f.HasOutputSpace())
            f.ChangeState(new FactoryIdleState());
    }

    public void Exit(Factory f) { }
}