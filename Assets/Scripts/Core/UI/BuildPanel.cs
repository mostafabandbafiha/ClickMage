using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildPanel : MonoBehaviour
{
    public static BuildPanel Instance { get; private set; }

    [System.Serializable]
    private class Tab
    {
        public Button TabButton;
        public GameObject Page; // the whole tab's content root (slots + its own description panel)
    }

    [SerializeField] private List<Tab> _tabs;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (var tab in _tabs)
        {
            var capturedTab = tab; // avoid closure-over-loop-variable bug
            tab.TabButton.onClick.AddListener(() => SwitchTab(capturedTab));
        }

        if (_tabs.Count > 0) SwitchTab(_tabs[0]);
    }

    private void SwitchTab(Tab tab)
    {
        foreach (var t in _tabs)
            t.Page.SetActive(t == tab);
    }

    // ── Existing wired buttons — unchanged behavior ─────────────────────────
    public void Open() => gameObject.SetActive(true);

    public void OnBuildModeButtonPressed()
    {
        if (BuildModeController.Instance.IsActive)
            BuildModeController.Instance.LeaveBuildMode();
        else
            BuildModeController.Instance.EnterBuildMode();
    }

    public void OnAccept() => BuildModeController.Instance.ConfirmCurrent();
    public void OnCancel() => BuildModeController.Instance.CancelCurrent();

    public void Close()
    {
        BuildModeController.Instance.LeaveBuildMode();
        gameObject.SetActive(false);
    }

    public void OnEnterMoveMode() => BuildModeController.Instance.EnterBuildMode();
}