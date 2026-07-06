using System;
using UnityEngine;

namespace ClickMage.StateMachine
{
    public class StateMachine<TOwner>
    {
        // Store as IState<TOwner> — contravariance lets us assign
        // IState<BaseCharacter> to IState<HarvesterCharacter> cleanly
        public IState<TOwner> CurrentState { get; private set; }

        private readonly TOwner _owner;

        public StateMachine(TOwner owner)
        {
            _owner = owner;
        }

        public void ChangeState(IState<TOwner> newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[StateMachine] Tried to change to null state.");
                return;
            }

            CurrentState?.Exit(_owner);
            CurrentState = newState;
            CurrentState.Enter(_owner);
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Tick(_owner, deltaTime);
        }

        public bool IsInState<TState>() where TState : IState<TOwner>
            => CurrentState is TState;

    }
}
