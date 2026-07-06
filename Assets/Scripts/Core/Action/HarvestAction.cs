using UnityEngine;
using ClickMage.Interaction;

public class HarvestAction : MonoBehaviour, IContextAction
{
    private ResourceNode _node;

    private void Awake() => _node = GetComponent<ResourceNode>();

    public string ActionLabel => "Harvest";

    public bool IsAvailable(BaseCharacter character)
        => character is HarvesterCharacter
        && _node != null
        && _node.CurrentHP > 0;

    public void Execute(BaseCharacter character)
    {
        bool isShift = Input.GetKey(KeyCode.LeftShift);

        var moveCmd = new MoveCommand(_node.transform.position, stoppingDistance: 1.5f);
        var harvestCmd = new HarvestCommand(_node);

        if (isShift)
        {
            character.QueueCommand(moveCmd);
            character.QueueCommand(harvestCmd);
        }
        else
        {
            character.GiveCommand(moveCmd);
            character.QueueCommand(harvestCmd);
        }

        Debug.Log($"[HarvestAction] {(isShift ? "Queued" : "Issued")} move + harvest → {_node.name}");
    }

}
