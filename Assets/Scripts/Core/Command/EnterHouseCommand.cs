using ClickMage.Interface;
using UnityEngine;

public class EnterHouseCommand : ICommand<BaseCharacter>
{
    private readonly HouseStructure _home;
    public bool IsComplete { get; private set; }

    public EnterHouseCommand(HouseStructure home) => _home = home;

    public void Start(BaseCharacter character)
    {
        _home.CharacterEntered(character);

        // Hide the character visually inside the house
        character.gameObject.SetActive(false);

        Debug.Log($"[EnterHomeCommand] {character.name} entered {_home.name}.");
        IsComplete = true;
    }

    public void Tick(BaseCharacter character, float deltaTime) { }
    public void Cancel() => IsComplete = true;
}