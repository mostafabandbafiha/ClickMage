using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class UIPanelAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openStateName = "Open";
    [SerializeField] private string _closeStateName = "Close";
    [SerializeField] private Button _button;

    [Header("Interaction (optional)")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Start State")]
    [SerializeField] private bool _startOpen = false;

    public bool IsOpen { get; private set; }

    public event Action OnOpened;
    public event Action OnClosed;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        IsOpen = _startOpen;
        SetInteractable(_startOpen);

        _button.onClick.AddListener(Toggle);
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        SetInteractable(true);
        _animator.Play(_openStateName, 0, 0f); // layer 0, start from beginning
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        _animator.Play(_closeStateName, 0, 0f);
        // Interaction turned off in AnimEvent_OnCloseFinished
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    // ── Animation Events — add these on the last frame of each clip ──
    public void AnimEvent_OnOpenFinished() => OnOpened?.Invoke();

    public void AnimEvent_OnCloseFinished()
    {
        SetInteractable(false);
        OnClosed?.Invoke();
    }

    private void SetInteractable(bool value)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }
}