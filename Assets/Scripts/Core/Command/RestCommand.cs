using UnityEngine;

public class RestCommand : ICommand<BaseCharacter>
{
    private readonly WorldPoint _restPoint;
    public bool IsComplete { get; private set; }

    public RestCommand(WorldPoint restPoint)
    {
        _restPoint = restPoint;
    }

    public void Start(BaseCharacter character)
    {
        // Verify still in range (move command should have placed us there)
        float dist = Vector3.Distance(character.transform.position, _restPoint.transform.position);
        if (dist > 5f)
        {
            Debug.LogWarning($"[RestCommand] {character.name} too far from rest point ({dist:F1}u). Cancelling.");
            _restPoint.Release(character); // release the spot we claimed
            IsComplete = true;
            return;
        }

        character.ResetPath();
        character.StateMachine.ChangeState(new CharacterRestingState(_restPoint));
        IsComplete = true; // State now owns the loop � command is done
    }

    public void Tick(BaseCharacter character, float deltaTime) { }

    public void Cancel()
    {
        IsComplete = true;
        // Note: Release is handled by CharacterRestingState.Exit()
        // If cancelled before Start(), no spot was claimed yet
    }
}