using System.Collections.Generic;
using UnityEngine;

public class BuildPanel : MonoBehaviour
{
    public static BuildPanel Instance { get; private set; }

    [SerializeField] private Transform _slotContainer;
    [SerializeField] private StructureSlotView _slotPrefab;
    [SerializeField] private List<StructureDefinition> _availableStructures;
    
    private StructureSlotView _selectedSlot;

    private void Awake()
    {
        Instance = this;
        //gameObject.SetActive(false);
    }

    private void Start() => BuildSlots();

    private void BuildSlots()
    {
        foreach (var def in _availableStructures)
        {
            var slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Bind(def);
        }
    }

    // called by your existing open button
    public void Open()
    {
        gameObject.SetActive(true);

    }

    public void OnBuildModeButtonPressed()
    {
        if (BuildModeController.Instance.IsActive)
        {
            BuildModeController.Instance.LeaveBuildMode();
            //_buildModeButtonLabel.text = "Build Mode";
        }
        else
        {
            BuildModeController.Instance.EnterBuildMode();
            //_buildModeButtonLabel.text = "Exit Build";
        }
    }
    public void OnAccept()
    {
        BuildModeController.Instance.ConfirmCurrent();
        ClearSelection();
    }

    public void OnCancel()
    {
        BuildModeController.Instance.CancelCurrent();
        ClearSelection();
    }

    public void Close()
    {
        BuildModeController.Instance.LeaveBuildMode();
        ClearSelection();
        gameObject.SetActive(false);
    }

    public void SelectStructure(StructureSlotView slot, StructureDefinition structure)
    {
        // deselect previous
        _selectedSlot?.SetSelected(false);

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        BuildModeController.Instance.EnterBuildMode(structure);
    }

    private void ClearSelection()
    {
        _selectedSlot?.SetSelected(false);
        _selectedSlot = null;
    }
    // wired to "Build Mode" button OnClick
    public void OnEnterMoveMode()
    {
        ClearSelection();
        BuildModeController.Instance.EnterBuildMode();
    }
}
