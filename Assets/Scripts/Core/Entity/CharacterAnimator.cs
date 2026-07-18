using UnityEngine;
using ClickMage.Animation;

public class CharacterAnimator : MonoBehaviour, IAnimatable
{
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeDuration = 0.15f;

    private string _lastRequestedState;

    public void PlayAnimation(string stateName)
    {
        PlayAnimation(stateName, forceRestart: false);
    }

    public void PlayAnimation(string stateName, bool forceRestart)
    {
        if (animator == null) return;

        // Guard against redundant re-triggering of a state we're already in
        // (prevents the walk/idle "pop" between queued moves) — but skip the
        // guard when forceRestart is set, e.g. for repeating actions like
        // attacks, where the same clip legitimately needs to replay from the
        // start every cooldown cycle rather than being treated as "already playing."
        if (!forceRestart && _lastRequestedState == stateName) return;

        _lastRequestedState = stateName;

        // CrossFade blends between clips instead of hard-cutting to frame 0,
        // which is what caused the walk -> idle -> walk "pop" between moves.
        // For forceRestart, still use CrossFade (not Play) so the attack replay
        // itself blends smoothly rather than popping — normalizedTime 0f just
        // ensures it restarts from the beginning of the clip rather than
        // continuing wherever the previous cycle left off.
        animator.CrossFade(stateName, crossFadeDuration, 0, 0f);
    }

    public void SetFloat(string param, float value)
    {
        animator.SetFloat(param, value);
    }

    public void SetBool(string param, bool value)
    {
        animator.SetBool(param, value);
    }

    public float GetClipLength(string stateName)
    {
        if (animator == null) return 0.5f;
        if (animator.runtimeAnimatorController is AnimatorOverrideController overrideController)
        {
            var clip = overrideController[stateName];
            if (clip != null) return clip.length;
        }
        return 0.5f;
    }
}