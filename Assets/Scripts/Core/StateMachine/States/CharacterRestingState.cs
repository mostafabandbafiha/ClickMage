using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterRestingState : IState<BaseCharacter>
{
    private readonly WorldPoint _restPoint;

    // ── Phases ────────────────────────────────────────────────────────────
    private enum RestPhase { SittingDown, SitIdle, StandingUp }
    private RestPhase _phase;

    // ── Energy ────────────────────────────────────────────────────────────
    private const float EnergyRestoreRate = 10f;
    private const float FullEnergyThreshold = 0.95f;

    // ── Social ────────────────────────────────────────────────────────────
    private const float SocialRadius = 5f;
    private const float SocialInterval = 8f;
    private float _socialTimer;
    private SpeechBubble _activeBubble;

    // ── Exit control ──────────────────────────────────────────────────────
    private bool _exitPending = false; // true = stand up then leave

    public CharacterRestingState(WorldPoint restPoint)
    {
        _restPoint = restPoint;
    }

    // ── IState ────────────────────────────────────────────────────────────

    public void Enter(BaseCharacter character)
    {
        _phase = RestPhase.SittingDown;
        _exitPending = false;
        _socialTimer = Random.Range(2f, SocialInterval); // stagger first bubble

        character.ResetPath();
        character.Animator?.PlayAnimation(AnimationKeys.Clips.SitDown);

        Debug.Log($"[RestingState] {character.name} sitting down at {_restPoint.name}");
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        switch (_phase)
        {
            case RestPhase.SittingDown:
                // Waiting for OnSitDownComplete animation event — nothing to tick
                break;

            case RestPhase.SitIdle:
                TickSitIdle(character, deltaTime);
                break;

            case RestPhase.StandingUp:
                // Waiting for OnStandUpComplete animation event — nothing to tick
                break;
        }

        // Always gently face the rest point while seated
        if (_phase != RestPhase.StandingUp)
            FaceRestPoint(character);
    }

    public void Exit(BaseCharacter character)
    {
        _restPoint.Release(character);

        if (_activeBubble != null)
        {
            _activeBubble.Hide();
            _activeBubble = null;
        }

        Debug.Log($"[RestingState] {character.name} left rest point.");
    }

    // ── Animation Events (called by your animation event bridge) ──────────

    /// <summary>Bind this to the last frame of the SitDown clip.</summary>
    public void OnSitDownComplete(BaseCharacter character)
    {
        if (_exitPending)
        {
            // Player issued a command while we were sitting down — stand straight back up
            BeginStandUp(character);
            return;
        }

        _phase = RestPhase.SitIdle;
        character.Animator?.PlayAnimation(AnimationKeys.Clips.SitIdle);
        Debug.Log($"[RestingState] {character.name} now in sit idle.");
    }

    /// <summary>Bind this to the last frame of the StandUp clip.</summary>
    public void OnStandUpComplete(BaseCharacter character)
    {
        // Whether we stood up naturally or were forced — same outcome.
        // Idle state hands off to the command queue (player command or autonomous).
        character.StateMachine.ChangeState(new CharacterIdleState());
    }

    // ── ForceExit (called by BaseCharacter when player gives a command) ───

    /// <summary>
    /// Gracefully interrupts resting so the character stands up before
    /// executing the player's command. Never snaps out mid-animation.
    /// </summary>
    public void ForceExit(BaseCharacter character)
    {
        switch (_phase)
        {
            case RestPhase.SittingDown:
                // Mid sit-down — flag it; OnSitDownComplete will trigger StandUp
                _exitPending = true;
                Debug.Log($"[RestingState] {character.name} will stand up after sit-down finishes.");
                break;

            case RestPhase.SitIdle:
                // Sitting idle — begin standing up now
                _exitPending = true;
                BeginStandUp(character);
                break;

            case RestPhase.StandingUp:
                // Already standing — just make sure flag is set, nothing else needed
                _exitPending = true;
                break;
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void TickSitIdle(BaseCharacter character, float deltaTime)
    {
        // Restore energy
        var needsManager = character.GetComponent<CharacterNeedsManager>();
        if (needsManager != null)
        {
            var energy = needsManager.GetNeed(NeedType.Energy);
            if (energy != null)
            {
                energy.Restore(EnergyRestoreRate * deltaTime);

                if (energy.NormalizedValue >= FullEnergyThreshold)
                {
                    // Energy full — stand up naturally
                    BeginStandUp(character);
                    return;
                }
            }
        }

        // Social bubble tick
        _socialTimer -= deltaTime;
        if (_socialTimer <= 0f)
        {
            _socialTimer = SocialInterval;
            TryStartConversation(character);
        }
    }

    private void BeginStandUp(BaseCharacter character)
    {
        _phase = RestPhase.StandingUp;
        character.Animator?.PlayAnimation(AnimationKeys.Clips.StandUp);
        Debug.Log($"[RestingState] {character.name} standing up.");
    }

    private void FaceRestPoint(BaseCharacter character)
    {
        Vector3 dir = _restPoint.transform.position - character.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        character.transform.rotation = Quaternion.Slerp(
            character.transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 5f
        );
    }

    // ── Social ────────────────────────────────────────────────────────────

    private void TryStartConversation(BaseCharacter character)
    {
        var partner = FindNearestRestingPartner(character);
        if (partner == null) return;

        ShowBubble(character);

        // Staggered reply on partner
        var delayer = character.GetComponent<BubbleDelayer>();
        if (delayer == null) delayer = character.gameObject.AddComponent<BubbleDelayer>();
        delayer.ShowDelayed(partner, delay: 1.5f);

        Debug.Log($"[RestingState] {character.name} socialising with {partner.name}");
    }

    private BaseCharacter FindNearestRestingPartner(BaseCharacter self)
    {
        // Prefer someone at the same rest point
        foreach (var occupant in _restPoint.Occupants)
        {
            if (occupant != self && occupant.StateMachine.CurrentState is CharacterRestingState)
                return occupant;
        }

        // Fallback — nearest resting character within radius
        var colliders = Physics.OverlapSphere(self.transform.position, SocialRadius);
        BaseCharacter nearest = null;
        float minDist = float.MaxValue;

        foreach (var col in colliders)
        {
            var other = col.GetComponent<BaseCharacter>();
            if (other == null || other == self) continue;
            if (other.StateMachine.CurrentState is not CharacterRestingState) continue;

            float dist = Vector3.Distance(self.transform.position, other.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = other;
            }
        }

        return nearest;
    }

    private void ShowBubble(BaseCharacter character)
    {
        _activeBubble = character.GetComponentInChildren<SpeechBubble>();
        if (_activeBubble == null)
        {
            var go = new GameObject("SpeechBubble");
            go.transform.SetParent(character.transform);
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            _activeBubble = go.AddComponent<SpeechBubble>();
        }

        _activeBubble.Show(duration: 3f);
    }
}