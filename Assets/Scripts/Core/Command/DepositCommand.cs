using UnityEngine;

public class DepositCommand : ICommand<BaseCharacter>
{
    public bool IsComplete { get; private set; }

    public void Start(BaseCharacter character)
    {
        if (character is not GathererCharacter gatherer || !gatherer.IsCarrying)
        {
            IsComplete = true;
            return;
        }

        if (Warehouse.Instance == null)
        {
            Debug.LogWarning("[DepositCommand] No warehouse in scene.");
            IsComplete = true;
            return;
        }

        float dist = Vector3.Distance(character.transform.position,
                                      Warehouse.Instance.DepositPosition);
        if (dist > 3f)
        {
            Debug.LogWarning($"[DepositCommand] {character.name} too far ({dist:F1}u).");
            IsComplete = true;
            return;
        }

        character.ResetPath();
        character.StateMachine.ChangeState(new CharacterDepositState());
        IsComplete = true;
    }

    public void Tick(BaseCharacter character, float deltaTime) { }
    public void Cancel() => IsComplete = true;
}