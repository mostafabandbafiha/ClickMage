using TMPro;
using UnityEngine;
using ClickMage.Stats;

/// <summary>
/// One row in the stats panel. Prefab needs two TMP_Text children:
///   keyLabel   → e.g. "Attack"
///   valueLabel → e.g. "42"
/// </summary>
public class StatRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private TMP_Text valueLabel;

    private BaseStat _stat;

    public void Bind(BaseStat stat)
    {
        _stat = stat;
        Refresh();
    }

    public void Refresh()
    {
        if (_stat == null) return;
        keyLabel.text = CommonStats.GetDisplayName(_stat.StatKey) ; // or a display-name lookup
        valueLabel.text = _stat.GetValue().ToString("0.##");       // "42", "1.5", etc.
    }
}