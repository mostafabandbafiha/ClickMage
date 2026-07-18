using ClickMage.Interface;
using UnityEngine;

public class RetreatCommand : ICommand<BaseCharacter>
{
    private readonly Vector3 _destination;
    private readonly float _arrivalThreshold;
    public bool IsComplete { get; private set; }

    public RetreatCommand(Vector3 destination, float arrivalThreshold = 1.5f)
    {
        _destination = destination;
        _arrivalThreshold = arrivalThreshold;
    }

    public void Start(BaseCharacter character)
    {
        character.ClearBlocked();
        character.SetDestination(_destination);
        character.StateMachine.ChangeState(new CharacterMovingState());
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        bool arrived = !character.Agent.pathPending &&
                       character.Agent.remainingDistance <= _arrivalThreshold;

        if (arrived)
        {
            IsComplete = true;
            character.StopMoving();
            if (character is EnemyCharacter enemy)
                enemy.Die(EnemyCharacter.DeathCause.Retreated);
        }
    }

    public void Cancel() => IsComplete = true;
}