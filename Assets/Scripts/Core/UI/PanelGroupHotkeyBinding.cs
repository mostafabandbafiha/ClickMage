using UnityEngine;

/// <summary>
/// Registers a hotkey action that treats multiple panels as one group:
/// if ANY are open, closes all; if ALL are closed, opens all.
/// </summary>
public class PanelGroupHotkeyBinding : MonoBehaviour, IHotkeyReceiver
{
    [SerializeField] private string _actionId = "ToggleQuickPanels";
    [SerializeField] private UIPanelAnimator[] _panels;

    private void Start() => HotkeyController.Instance?.Register(_actionId, this);
    private void OnDestroy() => HotkeyController.Instance?.Unregister(_actionId, this);

    public void OnHotkeyTriggered(string actionId)
    {
        bool anyOpen = false;
        foreach (var panel in _panels)
        {
            if (panel != null && panel.IsOpen) { anyOpen = true; break; }
        }

        foreach (var panel in _panels)
        {
            if (panel == null) continue;

            if (anyOpen) panel.Close();
            else panel.Open();
        }
    }
}