// AttackAction.cs
// Add this component to your enemy prefab (alongside EntityTargetable).
// When a HeroCharacter right-clicks the enemy, ContextMenuManager finds this
// via GetComponents<IContextAction>() and shows an "Attack" button.
//
// Mirrors HarvestAction exactly — same pattern, different command.

using UnityEngine;
using ClickMage.Interaction;
using ClickMage.Stats;

public class AttackAction : MonoBehaviour, IContextAction
{
    private Targetable _targetable;

    private void Awake() => _targetable = GetComponent<Targetable>();

    // ── IContextAction ────────────────────────────────────────────────────

    public string ActionLabel => "Attack";

    public bool IsAvailable(BaseCharacter character)
        => character is HeroCharacter
        && _targetable != null
        && _targetable.IsAlive;

    public void Execute(BaseCharacter character)
    {
        

        if (_targetable == null || !_targetable.IsAlive) return;

        float attackRange = character.HasStat(CommonStats.AttackRange)
            ? character.GetStatValue(CommonStats.AttackRange)
            : 2f;

        bool isShift = Input.GetKey(KeyCode.LeftShift);

        var moveCmd = new MoveCommand(_targetable.transform.position, attackRange);
        var attackCmd = new AttackCommand(_targetable, (CombatCharacter) character);

        if (isShift)
        {
            character.QueueCommand(moveCmd);
            character.QueueCommand(attackCmd);
        }
        else
        {
            character.GiveCommand(moveCmd);
            character.QueueCommand(attackCmd);
        }

        Debug.Log($"[AttackAction] {(isShift ? "Queued" : "Issued")} move + attack → {name}");
    }
}