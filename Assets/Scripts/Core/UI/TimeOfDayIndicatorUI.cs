using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RTLTMPro;

public class TimeOfDayIndicatorUI : MonoBehaviour
{
    [Header("Radial Countdown")]
    [SerializeField] private Image _radialFillImage; // Filled / Radial 360

    [Header("Icon Sun-Set / Moon-Rise")]
    [SerializeField] private RectTransform _currentIconRect;
    [SerializeField] private Image _currentIconImage;
    [SerializeField] private RectTransform _nextIconRect;
    [SerializeField] private Image _nextIconImage;
    [SerializeField] private float _slideDistance = 60f; // how far icons move up/down, in UI units

    [Header("Phase Sprites")]
    [SerializeField] private Sprite _daySprite;
    [SerializeField] private Sprite _sunsetSprite;
    [SerializeField] private Sprite _nightSprite;
    [SerializeField] private Sprite _rainySprite;

    [Header("Countdown Text")]
    [SerializeField] private RTLTextMeshPro _remainingTimeText;
    [SerializeField] private bool _showMinutesSeconds = true;

    private Dictionary<TimeOfDay, Sprite> _spriteLookup;
    private TimeOfDay _lastAppliedCurrent;
    private bool _initialized;

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

    private void OnDestroy()
    {
        if (DayNightCycleManager.Instance == null) return;
        DayNightCycleManager.Instance.OnTransitionProgress -= HandleTransitionProgress;
    }

    private void HandleTransitionProgress(TimeOfDay from, TimeOfDay to, float blendT)
    {
        if (!_initialized || _lastAppliedCurrent != from)
        {
            if (_spriteLookup.TryGetValue(from, out var fromSprite))
                _currentIconImage.sprite = fromSprite;
            _lastAppliedCurrent = from;
            _initialized = true;
        }

        if (_spriteLookup.TryGetValue(to, out var toSprite))
            _nextIconImage.sprite = toSprite;

        // First half of the transition: current icon exits (slides down, fades out)
        float exitT = Mathf.Clamp01(blendT / 0.5f);
        float currentY = Mathf.Lerp(0f, -_slideDistance, exitT);
        float currentAlpha = 1f - exitT;
        SetIcon(_currentIconRect, _currentIconImage, currentY, 1f, currentAlpha);

        // Second half: next icon pops up (only starts moving once current is gone)
        float enterT = Mathf.Clamp01((blendT - 0.5f) / 0.5f);
        float poppedT = EaseOutBack(enterT);
        float nextY = Mathf.LerpUnclamped(-_slideDistance, 0f, poppedT);
        float nextScale = Mathf.LerpUnclamped(0.6f, 1f, poppedT);
        float nextAlpha = Mathf.Clamp01(enterT * 2f);
        SetIcon(_nextIconRect, _nextIconImage, nextY, nextScale, nextAlpha);
    }

    private void SetIcon(RectTransform rect, Image image, float yPos, float scale, float alpha)
    {
        if (rect != null)
        {
            var pos = rect.anchoredPosition;
            pos.y = yPos;
            rect.anchoredPosition = pos;
            rect.localScale = Vector3.one * scale;
        }

        if (image != null)
        {
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        float t1 = t - 1f;
        return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
    }

    private void SetIconTransform(RectTransform rect, Image image, float yPos, float alpha)
    {
        if (rect != null)
        {
            var pos = rect.anchoredPosition;
            pos.y = yPos;
            rect.anchoredPosition = pos;
        }

        if (image != null)
        {
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }

    private void Update()
    {
        if (DayNightCycleManager.Instance == null) return;

        var mgr = DayNightCycleManager.Instance;
        float duration = mgr.GetDuration(mgr.CurrentTimeOfDay);
        float remaining = mgr.CurrentPhaseTimeRemaining;

        if (_radialFillImage != null && duration > 0f)
            _radialFillImage.fillAmount = Mathf.Clamp01(remaining / duration);

        if (_remainingTimeText != null)
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