using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unity.VisualScripting;

public class StructureSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _highlight;
    [SerializeField] private TMP_Text _nameText;

    private StructureDefinition _structure;

    public void Bind(StructureDefinition structure)
    {
        _structure = structure;
        _nameText.text = structure.Name;
        if (_iconImage != null) _iconImage.sprite = structure.Icon;
        _highlight.enabled = false;
    }

    public void SetSelected(bool selected) => _highlight.enabled = selected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_structure == null) return;
        StructureTabPanel.Instance.SelectStructure(this, _structure);
    }
}