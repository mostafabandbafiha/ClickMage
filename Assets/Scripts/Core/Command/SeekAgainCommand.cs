// SeekAgainCommand.cs
using ClickMage.Interface;

public class SeekAgainCommand : ICommand<BaseCharacter>
{
    public bool IsComplete { get; private set; }

    public void Start(BaseCharacter character)
    {
        IsComplete = true; // completes instantly → OnQueueEmptied → BT ticks
    }

    public void Tick(BaseCharacter character, float deltaTime) { }

    public void Cancel() => IsComplete = true;
}