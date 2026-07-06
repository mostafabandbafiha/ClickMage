using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add to any GameObject that can be interacted with.
/// Other components register callbacks via RegisterInteraction().
/// CameraController only needs to find this component — never the specific logic.
/// </summary>
public class InteractableComponent : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private string _interactionText = "Interact";
    [SerializeField] private bool _isInteractable = true;

    // Unity event so you can also wire things up in the inspector if needed
    [SerializeField] private UnityEvent _onInteract;

    // Code-registered callback — WorldItemPickup hooks in here
    private System.Action _interactionCallback;

    public bool IsInteractable => _isInteractable;
    public string InteractionText => _interactionText;

    /// <summary>
    /// Called by sibling components (e.g. WorldItemPickup) during their Awake.
    /// Replaces direct IInteractable implementation.
    /// </summary>
    public void RegisterInteraction(System.Action callback)
    {
        _interactionCallback = callback;
    }

    /// <summary>
    /// Called by CameraController — it never knows what happens next.
    /// </summary>
    public void Interact()
    {
        if (!_isInteractable) return;

        _interactionCallback?.Invoke();
        _onInteract?.Invoke();
    }

    public void SetInteractable(bool value) => _isInteractable = value;
    public void SetInteractionText(string text) => _interactionText = text;

    // Add this method to InteractableComponent.cs
    public void TriggerInteraction()
    {
        _onInteract?.Invoke();
    }

}
