using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterPickupState : IState<BaseCharacter>
{
    private readonly WorldItemPickup _pickup;

    private enum PickupPhase { BendingDown, Grabbing }
    private PickupPhase _phase;

    public CharacterPickupState(WorldItemPickup pickup)
    {
        _pickup = pickup;
    }

    public void Enter(BaseCharacter character)
    {
        _phase = PickupPhase.BendingDown;
        character.ResetPath();

        // Face the item before bending
        FaceTarget(character, _pickup.transform.position);

        character.Animator?.PlayAnimation(AnimationKeys.Clips.PickupBend);
        Debug.Log($"[PickupState] {character.name} bending to pick up {_pickup?.Stack.Data?.DisplayName}");
    }

    public void Tick(BaseCharacter character, float deltaTime) { }  // animation events drive transitions

    public void Exit(BaseCharacter character) { }

    // ── Animation Events ──────────────────────────────────────────────────

    /// <summary>Bind to the last frame of PickupBend clip.</summary>
    public void OnBendComplete(BaseCharacter character)
    {
        if (_pickup == null)
        {
            // Item vanished — abort
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        _phase = PickupPhase.Grabbing;
        character.Animator?.PlayAnimation(AnimationKeys.Clips.PickupGrab);
    }

    /// <summary>
    /// Bind to the GRAB FRAME of PickupGrab clip — the frame the hand closes.
    /// This is when the item visually transfers to the hand.
    /// </summary>
    public void OnGrabHit(BaseCharacter character)
    {
        if (character is not GathererCharacter gatherer) return;
        if (_pickup == null) return;

        gatherer.AttachItem(_pickup); // spawns visual on hand bone, destroys world pickup
    }

    /// <summary>Bind to the last frame of PickupGrab clip.</summary>
    public void OnGrabComplete(BaseCharacter character)
    {
        // Hand now has the item — go to carry walk
        character.StateMachine.ChangeState(new CharacterCarryState());
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private void FaceTarget(BaseCharacter character, Vector3 target)
    {
        Vector3 dir = target - character.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        character.transform.rotation = Quaternion.LookRotation(dir);
    }
}