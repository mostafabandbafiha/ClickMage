using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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
        // _iconImage.sprite = structure.Icon; // add Icon field to StructureDefinition when ready
        _highlight.enabled = false;
    }

    public void SetSelected(bool selected) => _highlight.enabled = selected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_structure == null) return;
        BuildPanel.Instance.SelectStructure(this, _structure);
    }
}
