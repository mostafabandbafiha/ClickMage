using UnityEngine;

public class PickupCommand : ICommand<BaseCharacter>
{
    private readonly WorldItemPickup _pickup;
    public bool IsComplete { get; private set; }

    public PickupCommand(WorldItemPickup pickup)
    {
        _pickup = pickup;
    }

    public void Start(BaseCharacter character)
    {
        // Item may have been destroyed between claim and arrival (player picked it up etc.)
        if (_pickup == null)
        {
            Debug.LogWarning($"[PickupCommand] Target item is gone. Aborting.");
            IsComplete = true;
            return;
        }

        if (character is not GathererCharacter gatherer)
        {
            IsComplete = true;
            return;
        }

        float dist = Vector3.Distance(character.transform.position, _pickup.transform.position);
        if (dist > 3f)
        {
            Debug.LogWarning($"[PickupCommand] {character.name} too far ({dist:F1}u). Aborting.");
            _pickup.Unclaim();
            IsComplete = true;
            return;
        }

        character.ResetPath();
        character.StateMachine.ChangeState(new CharacterPickupState(_pickup));
        IsComplete = true; // state owns the loop from here
    }

    public void Tick(BaseCharacter character, float deltaTime) { }

    public void Cancel()
    {
        _pickup?.Unclaim();
        IsComplete = true;
    }
}