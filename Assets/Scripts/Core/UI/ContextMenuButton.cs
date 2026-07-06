using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ContextMenuButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    public void Setup(string text, Action onClick)
    {
        label.text = text;
        button.onClick.AddListener(() => onClick());
    }
}
