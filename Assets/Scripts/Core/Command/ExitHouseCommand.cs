using ClickMage.Animation;
using ClickMage.Interface;
using UnityEngine;

public class ExitHouseCommand : ICommand<BaseCharacter>
{
    private readonly HouseStructure _home;
    public bool IsComplete { get; private set; }

    public ExitHouseCommand(HouseStructure home) => _home = home;

    public void Start(BaseCharacter character)
    {
        _home.CharacterExited(character);
        character.gameObject.SetActive(true);
        character.transform.position = _home.ExitPosition;

        // Force idle so no stale animation plays on exit
        character.StateMachine.ChangeState(new CharacterIdleState());
        character.Animator?.PlayAnimation(AnimationKeys.Clips.Idle);

        IsComplete = true;
    }

    public void Tick(BaseCharacter character, float deltaTime) { }
    public void Cancel() => IsComplete = true;
}