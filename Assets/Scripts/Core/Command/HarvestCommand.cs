using ClickMage.Interface;
using UnityEngine;

public class HarvestCommand : ICommand<BaseCharacter>
{
    private readonly ResourceNode _node;
    private readonly float _interactionRange;

    public bool IsComplete { get; private set; }

    public HarvestCommand(ResourceNode node, float interactionRange = 4.0f)
    {
        _node = node;
        _interactionRange = interactionRange;
    }

    public void Start(BaseCharacter character)
    {
        float dist = Vector3.Distance(character.transform.position, _node.transform.position);
        Debug.Log($"[HarvestCommand] Start — dist: {dist:F2}u | interactionRange: {_interactionRange}u");

        if (dist > _interactionRange)
        {
            Debug.LogWarning("[HarvestCommand] Started out of range — move command should have run first.");
            IsComplete = true;
            return;
        }

        if (character is not HarvesterCharacter harvester)
        {
            Debug.LogWarning("[HarvestCommand] Character is not a HarvesterCharacter.");
            IsComplete = true;
            return;
        }

        harvester.ResetPath();
        harvester.StateMachine.ChangeState(new CharacterHarvestingState(_node)); // ← need to confirm StateMachine is accessible
        IsComplete = true;
    }

    public void Tick(BaseCharacter character, float deltaTime) { }

    public void Cancel()
    {
        IsComplete = true;
    }
}
