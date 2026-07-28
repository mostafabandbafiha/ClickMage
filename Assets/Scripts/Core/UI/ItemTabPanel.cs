using System.Collections.Generic;
using ClickMage.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTabPanel : MonoBehaviour
{
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private ItemSlotView _slotPrefab;
    [SerializeField] private List<ItemData> _availableItems;

    [Header("Description")]
    [SerializeField] private GameObject _descriptionRoot;
    [SerializeField] private Image _descriptionIcon;
    [SerializeField] private TMP_Text _descriptionName;
    [SerializeField] private TMP_Text _descriptionText;

    private ItemSlotView _selectedSlot;

    private void Start()
    {
        BuildSlots();
        _descriptionRoot.SetActive(false);
    }

    private void BuildSlots()
    {
        foreach (var item in _availableItems)
        {
            var slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Bind(item);
            slot.OnClicked += HandleSlotClicked;
        }
    }

    private void HandleSlotClicked(ItemSlotView slot)
    {
        _selectedSlot?.SetSelected(false);
        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);

        ShowDescription(slot.Item);
        // no build-mode hookup here — wire equip/use later once that system exists
    }

    private void ShowDescription(ItemData item)
    {
        _descriptionRoot.SetActive(true);
        _descriptionIcon.sprite = item.Icon;
        _descriptionName.text = item.DisplayName;
        _descriptionText.text = item.Description;
    }

    public void ClearSelection()
    {
        _selectedSlot?.SetSelected(false);
        _selectedSlot = null;
        _descriptionRoot.SetActive(false);
    }
}