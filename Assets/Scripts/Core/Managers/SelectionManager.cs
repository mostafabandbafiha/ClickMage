using System;
using UnityEngine;

/// <summary>
/// Singleton. Single source of truth for what is currently selected.
/// CameraController calls Select / Deselect.
/// UI listens to OnSelected / OnDeselected.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────

    public static SelectionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── State ──────────────────────────────────────────────────────────────

    public SelectableComponent CurrentSelected { get; private set; }
    public bool HasSelection => CurrentSelected != null;

    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>Fired after a new object is selected. Passes the new SelectableComponent.</summary>
    public event Action<SelectableComponent> OnSelected;

    /// <summary>Fired after the current object is deselected. Passes what WAS selected.</summary>
    public event Action<SelectableComponent> OnDeselected;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Select a new target.
    /// If the same object is already selected, this is a no-op.
    /// If something else is selected, it is deselected first.
    /// </summary>
    public void Select(SelectableComponent target)
    {
        if (target == null)
        {
            Deselect();
            return;
        }

        // clicking the already-selected object does nothing
        if (CurrentSelected == target) return;

        // deselect previous
        if (CurrentSelected != null)
        {
            SelectableComponent previous = CurrentSelected;
            CurrentSelected = null;
            previous.OnDeselected();
            OnDeselected?.Invoke(previous);
        }

        // select new
        CurrentSelected = target;
        CurrentSelected.OnSelected();
        Debug.Log(CurrentSelected.DisplayName);
        OnSelected?.Invoke(CurrentSelected);
    }

    /// <summary>
    /// Clear the current selection.
    /// Does nothing if nothing is selected.
    /// </summary>
    public void Deselect()
    {
        if (CurrentSelected == null) return;

        SelectableComponent previous = CurrentSelected;
        CurrentSelected = null;
        previous.OnDeselected();
        OnDeselected?.Invoke(previous);
    }
}
