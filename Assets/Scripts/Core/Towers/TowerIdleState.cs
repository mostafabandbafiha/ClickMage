// TowerIdleState.cs — no subscription, just scan and transition
using UnityEngine;
using ClickMage.StateMachine;

public class TowerIdleState : IState<Tower>
{
    private const float ScanInterval = 0.5f;
    private float _scanTimer = 0f;

    public void Enter(Tower owner)
    {
        owner.ClearTarget();
        _scanTimer = 0f;
    }

    public void Tick(Tower owner, float deltaTime)
    {
        _scanTimer += deltaTime;
        if (_scanTimer < ScanInterval) return;
        _scanTimer = 0f;

        Targetable target = owner.FindAndEngageTarget();
        if (target == null) return;

        owner.StateMachine.ChangeState(new TowerAcquiringTargetState());
    }

    public void Exit(Tower owner) { }
}