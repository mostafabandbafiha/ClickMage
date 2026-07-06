using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterDepositState : IState<BaseCharacter>
{
    private enum DepositPhase { PlacingDown, StandingUp }
    private DepositPhase _phase;

    public void Enter(BaseCharacter character)
    {
        _phase = DepositPhase.PlacingDown;
        character.ResetPath();

        // Face the warehouse deposit point
       /* if (Warehouse.Instance != null)
        {
            Vector3 dir = Warehouse.Instance.DepositPosition - character.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                character.transform.rotation = Quaternion.LookRotation(dir);
        }*/

        character.Animator?.PlayAnimation(AnimationKeys.Clips.DepositPlace);
        Debug.Log($"[DepositState] {character.name} depositing item.");
    }

    public void Tick(BaseCharacter character, float deltaTime) { }   // animation events drive this

    public void Exit(BaseCharacter character) { }

    // ── Animation Events ──────────────────────────────────────────────────

    /// <summary>
    /// Bind to the RELEASE FRAME of DepositPlace clip — the frame the hand opens.
    /// Item visual disappears and stack goes into warehouse storage.
    /// </summary>
    public void OnReleaseHit(BaseCharacter character)
    {
        if (character is not GathererCharacter gatherer) return;

        var stack = gatherer.CarriedStack;
        gatherer.DetachItem();
        Warehouse.Instance?.Deposit(stack);
    }

    /// <summary>Bind to the last frame of DepositPlace clip.</summary>
    public void OnPlaceComplete(BaseCharacter character)
    {
        _phase = DepositPhase.StandingUp;
        character.Animator?.PlayAnimation(AnimationKeys.Clips.DepositStandUp);
    }

    /// <summary>Bind to the last frame of DepositStandUp clip.</summary>
    public void OnStandUpComplete(BaseCharacter character)
    {
        character.StateMachine.ChangeState(new CharacterIdleState());
        // Idle → OnQueueEmptied → Autonomous → GatherItemsBehavior runs again
    }
}