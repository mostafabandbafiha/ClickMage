using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StructureTabPanel : MonoBehaviour
{
    public static StructureTabPanel Instance { get; private set; }

    [SerializeField] private Transform _slotContainer;
    [SerializeField] private StructureSlotView _slotPrefab;
    [SerializeField] private List<StructureDefinition> _availableStructures;

    [Header("Description")]
    [SerializeField] private GameObject _descriptionRoot;
    [SerializeField] private Image _descriptionIcon;
    [SerializeField] private TMP_Text _descriptionName;
    [SerializeField] private TMP_Text _descriptionText;

    private StructureSlotView _selectedSlot;

    private void Awake() => Instance = this;

    private void Start()
    {
        BuildSlots();
        _descriptionRoot.SetActive(false);
    }

    private void BuildSlots()
    {
        foreach (var def in _availableStructures)
        {
            var slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Bind(def);
        }
    }

    // called directly by StructureSlotView.OnPointerClick
    public void SelectStructure(StructureSlotView slot, StructureDefinition structure)
    {
        _selectedSlot?.SetSelected(false);
        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        ShowDescription(structure);
        BuildModeController.Instance.EnterBuildMode(structure);
    }

    private void ShowDescription(StructureDefinition def)
    {
        _descriptionRoot.SetActive(true);
        _descriptionIcon.sprite = def.Icon;
        _descriptionName.text = def.Name;
        _descriptionText.text = def.Description;
    }

    public void ClearSelection()
    {
        _selectedSlot?.SetSelected(false);
        _selectedSlot = null;
        _descriptionRoot.SetActive(false);
    }
}