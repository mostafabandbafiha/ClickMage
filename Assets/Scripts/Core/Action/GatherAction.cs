using UnityEngine;
using ClickMage.Interaction;

public class GatherAction : MonoBehaviour, IContextAction
{
    private WorldItemPickup _pickup;

    private void Awake() => _pickup = GetComponent<WorldItemPickup>();

    public string ActionLabel => "Gather";

    public bool IsAvailable(BaseCharacter character)
        => character is GathererCharacter gatherer
        && _pickup != null
        && !_pickup.IsClaimed
        && gatherer.AcceptsItem(_pickup);

    public void Execute(BaseCharacter character)
    {
        if (!_pickup.TryClaim())
        {
            Debug.LogWarning($"[GatherAction] {character.name} lost claim race on {_pickup.name}.");
            return;
        }

        bool isShift = Input.GetKey(KeyCode.LeftShift);

        var moveCmd = new MoveCommand(_pickup.transform.position, stoppingDistance: 1.5f);
        var pickupCmd = new PickupCommand(_pickup);

        if (isShift)
        {
            character.QueueCommand(moveCmd);
            character.QueueCommand(pickupCmd);
        }
        else
        {
            character.GiveCommand(moveCmd);
            character.QueueCommand(pickupCmd);
        }

        Debug.Log($"[GatherAction] {(isShift ? "Queued" : "Issued")} move + pickup → {_pickup.name}");
    }
}