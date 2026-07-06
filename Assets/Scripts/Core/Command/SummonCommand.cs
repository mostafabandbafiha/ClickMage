// SummonCommand.cs
public class SummonCommand : ICommand<BaseCharacter>
{
    private readonly AbyssalHorror _horror;

    public bool IsComplete { get; private set; }

    public SummonCommand(AbyssalHorror horror)
    {
        _horror = horror;
    }

    public void Start(BaseCharacter character)
    {
        character.StopMoving();
        character.StateMachine.ChangeState(new SummonState());
    }

    public void Tick(BaseCharacter character, float deltaTime) 
    {
        if (_horror.ActiveSummonCount >= _horror.MaxSummons)
        {
            IsComplete = true;
        }
    }



    public void Cancel() => IsComplete = true;
}