using ClickMage.Animation;
using ClickMage.StateMachine;
using UnityEngine;

public class CharacterHarvestingState : IState<BaseCharacter>
{
    private readonly ResourceNode _node;
    private HarvesterCharacter _harvester;
    private bool _isReady; // ← guard flag

    private const float RotationSpeed = 720f;

    public CharacterHarvestingState(ResourceNode node)
    {
        _node = node;
    }

    public void Enter(BaseCharacter character)
    {
        _isReady = false; // ← reset every time we enter
        _harvester = character as HarvesterCharacter;

        if (_harvester == null || !_harvester.CanHarvest)
        {
            //Debug.LogWarning($"[HarvestingState] {character.name} cannot harvest — missing stat.");
            _harvester = null;
            return;
        }

        //character.StopMoving();
        SnapFaceNode(character);

        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);

        _isReady = true; // ← only set true if everything passed
        //Debug.Log($"[HarvestingState] {character.name} begins harvesting {_node.name}");
    }

    public void Tick(BaseCharacter character, float deltaTime)
    {
        // Not ready yet — do nothing this frame
        if (!_isReady) return;

        if (_harvester == null || !_harvester.CanHarvest)
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        if (!_node.CanHarvest())
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        SmoothFaceNode(character, deltaTime);
    }

    public void Exit(BaseCharacter character)
    {
        if (character is HarvesterCharacter harvester)
        {
            // If node is now depleted, blacklist it and return to autonomous
            if (!_node.CanHarvest())
            {
                harvester.BlacklistNode(_node);
            }
        }

        _isReady = false;
        _harvester = null;
        //Debug.Log($"[HarvestingState] {character.name} stopped harvesting.\n{System.Environment.StackTrace}");
    }

    // ── Called by Animation Event (hit frame) ────────────────────────────────

    public void DealHarvestHit(BaseCharacter character)
    {
        if (_harvester == null || !_node.CanHarvest()) return;

        bool harvested = _node.TryHarvest(_harvester);
        //Debug.Log($"[HarvestingState] Hit landed — harvested={harvested}");
    }

    // ── Called by Animation Event (swing end frame) ──────────────────────────

    public void OnSwingComplete(BaseCharacter character)
    {
        if (!_node.CanHarvest())
        {
            character.StateMachine.ChangeState(new CharacterIdleState());
            return;
        }

        character.Animator?.PlayAnimation(AnimationKeys.Clips.Harvest);
        //Debug.Log($"[HarvestingState] Swing complete — starting next swing.");
    }

    // ── Rotation Helpers ──────────────────────────────────────────────────────

    private void SnapFaceNode(BaseCharacter character)
    {
        Vector3 direction = GetDirectionToNode(character);
        if (direction == Vector3.zero) return;
        character.transform.rotation = Quaternion.LookRotation(direction);
    }

    private void SmoothFaceNode(BaseCharacter character, float deltaTime)
    {
        Vector3 direction = GetDirectionToNode(character);
        if (direction == Vector3.zero) return;

        character.transform.rotation = Quaternion.RotateTowards(
            character.transform.rotation,
            Quaternion.LookRotation(direction),
            RotationSpeed * deltaTime
        );
    }

    private Vector3 GetDirectionToNode(BaseCharacter character)
    {
        Vector3 direction = _node.transform.position - character.transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude < 0.001f ? Vector3.zero : direction.normalized;
    }
}
