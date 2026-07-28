using ClickMage.Items;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedHighlight;
    [SerializeField] private Button button;

    public ItemData Item { get; private set; }
    public event System.Action<ItemSlotView> OnClicked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() => OnClicked?.Invoke(this));
    }

    public void Bind(ItemData item)
    {
        Item = item;
        if (iconImage != null) iconImage.sprite = item.Icon;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }
}