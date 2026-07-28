using UnityEngine;

/// <summary>
/// Bridges a UIPanelAnimator to the hotkey system without either
/// class knowing about the other directly.
/// </summary>
public class PanelHotkeyBinding : MonoBehaviour, IHotkeyReceiver
{
    [SerializeField] private string _actionId = "ToggleInventory";
    [SerializeField] private UIPanelAnimator _panel;

    private void Start() => HotkeyController.Instance?.Register(_actionId, this);
    private void OnDestroy() => HotkeyController.Instance?.Unregister(_actionId, this);

    public void OnHotkeyTriggered(string actionId) => _panel.Toggle();
}