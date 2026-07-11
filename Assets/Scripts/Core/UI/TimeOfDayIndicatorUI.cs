using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTLTMPro;

public class TimeOfDayIndicatorUI : MonoBehaviour
{
    [Header("Sprite Crossfade")]
    [SerializeField] private Image _currentPhaseImage; // bottom layer
    [SerializeField] private Image _nextPhaseImage;     // top layer, fades in during transition

    [Header("Phase Sprites")]
    [SerializeField] private Sprite _daySprite;
    [SerializeField] private Sprite _sunsetSprite;
    [SerializeField] private Sprite _nightSprite;
    [SerializeField] private Sprite _rainySprite;

    [Header("Countdown Text")]
    [SerializeField] private RTLTextMeshPro _remainingTimeText;
    [SerializeField] private bool _showMinutesSeconds = true; // mm:ss vs raw seconds

    private Dictionary<TimeOfDay, Sprite> _spriteLookup;

    private void Awake()
    {
        _spriteLookup = new Dictionary<TimeOfDay, Sprite>
        {
            { TimeOfDay.Day, _daySprite },
            { TimeOfDay.Sunset, _sunsetSprite },
            { TimeOfDay.Night, _nightSprite },
            { TimeOfDay.Rainy, _rainySprite },
        };
    }

    private void Start()
    {
        if (DayNightCycleManager.Instance == null) return;
        DayNightCycleManager.Instance.OnTransitionProgress += HandleTransitionProgress;
    }

    private void OnDestory()
    {
        if (DayNightCycleManager.Instance == null) return;
        DayNightCycleManager.Instance.OnTransitionProgress -= HandleTransitionProgress;
    }

    private void HandleTransitionProgress(TimeOfDay from, TimeOfDay to, float blendT)
    {
        if (_currentPhaseImage != null && _spriteLookup.TryGetValue(from, out var fromSprite))
        {
            _currentPhaseImage.sprite = fromSprite;
            var c = _currentPhaseImage.color;
            c.a = 1f; // base layer always fully visible; the "to" layer fades on top of it
            _currentPhaseImage.color = c;
        }

        if (_nextPhaseImage != null && _spriteLookup.TryGetValue(to, out var toSprite))
        {
            _nextPhaseImage.sprite = toSprite;
            var c = _nextPhaseImage.color;
            c.a = blendT; // 0 = invisible (still fully 'from'), 1 = fully replaced 'to'
            _nextPhaseImage.color = c;
        }
    }

    private void Update()
    {
        if (DayNightCycleManager.Instance == null || _remainingTimeText == null) return;

        float remaining = DayNightCycleManager.Instance.CurrentPhaseTimeRemaining;
        _remainingTimeText.text = _showMinutesSeconds ? FormatMinutesSeconds(remaining) : Mathf.CeilToInt(remaining).ToString();
    }

    private static string FormatMinutesSeconds(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}