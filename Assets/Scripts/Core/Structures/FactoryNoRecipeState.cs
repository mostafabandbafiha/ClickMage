// -------------------------------------------------------
// No Recipe
// -------------------------------------------------------

using ClickMage.Factories;
using ClickMage.StateMachine;
using UnityEngine;

public sealed class FactoryNoRecipeState : IState<Factory>
{
    public void Enter(Factory f) =>
        Debug.Log($"[{f.name}] Waiting for a recipe.");
    public void Tick(Factory f, float dt) { }
    public void Exit(Factory f) { }
}
