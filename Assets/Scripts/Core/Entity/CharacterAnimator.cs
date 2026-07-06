using UnityEngine;
using ClickMage.Animation;

public class CharacterAnimator : MonoBehaviour, IAnimatable
{
    [SerializeField] private Animator animator;
    [SerializeField] private float crossFadeDuration = 0.15f;

    // Tracks the last state WE asked for, not what the Animator has actually
    // settled into. GetCurrentAnimatorStateInfo() lags behind CrossFade() calls
    // made earlier in the same script frame (Mecanim only applies transitions
    // during its own update pass), so guarding against it caused back-to-back
    // calls in one frame (e.g. Idle then immediately Walk, when a move finishes
    // and the next move is queued synchronously) to silently drop the second call.
    private string _lastRequestedState;

    public void PlayAnimation(string stateName)
    {
        if (animator == null) return;
        if (_lastRequestedState == stateName) return; // avoid restarting an already-requested clip
        _lastRequestedState = stateName;

        // CrossFade blends between clips instead of hard-cutting to frame 0,
        // which is what caused the walk -> idle -> walk "pop" between moves.
        animator.CrossFade(stateName, crossFadeDuration);
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