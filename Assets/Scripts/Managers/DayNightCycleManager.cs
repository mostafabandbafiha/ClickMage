using UnityEngine;
using System;
using System.Collections;

public enum TimeOfDay { Day, Sunset, Night, Rainy }

public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("Phase Durations")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float sunsetDuration = 60f;
    [SerializeField] private float nightDuration = 120f;
    [SerializeField] private float rainyDuration = 90f;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 15f;

    [Header("Testing")]
    [SerializeField] private bool useManualControl = false;
    [SerializeField, Range(0f, 1f)] private float manualTimeProgress = 0f;

    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public TimeOfDay NextTimeOfDay { get; private set; }
    public float TransitionProgress { get; private set; }
    public bool IsTransitioning { get; private set; }
    public float PhaseProgress { get; private set; }
    public float CurrentPhaseTimeRemaining { get; private set; }

    public event Action<TimeOfDay> OnTimeOfDayChanged;
    public event Action<TimeOfDay, TimeOfDay, float> OnTransitionProgress;
    public event Action<float> OnPhaseProgress;

    private Coroutine _cycleCoroutine;
    private TimeOfDay _previousTimeForManual;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CurrentTimeOfDay = TimeOfDay.Day;
        NextTimeOfDay = TimeOfDay.Sunset;
        _previousTimeForManual = TimeOfDay.Day;
        TransitionProgress = 0f;
        IsTransitioning = false;

        if (!useManualControl)
            RestartCycleFrom(TimeOfDay.Day);

        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, 0f);
    }

    // NEW: resume automatically if this object was ever disabled/re-enabled
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!useManualControl && _cycleCoroutine == null)
            RestartCycleFrom(CurrentTimeOfDay);
    }

    private void OnDisable()
    {
        StopCycleCoroutine();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (useManualControl)
        {
            StopCycleCoroutine();
            UpdateManualTime();
        }
        else if (_cycleCoroutine == null)
        {
            RestartCycleFrom(CurrentTimeOfDay);
        }
    }

    // ── Single choke point for starting/stopping the cycle ─────────────────
    // Every place that needs to (re)start the automatic cycle goes through
    // here, so there is never more than one CycleFrom coroutine alive.
    private void RestartCycleFrom(TimeOfDay phase)
    {
        StopCycleCoroutine();
        _cycleCoroutine = StartCoroutine(CycleFrom(phase));
    }

    private void StopCycleCoroutine()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }
    }

    private void UpdateManualTime()
    {
        float total = dayDuration + sunsetDuration + nightDuration + rainyDuration;
        float abs = manualTimeProgress * total;

        TimeOfDay phase;
        float phaseProgress;

        if (abs < dayDuration)
        {
            phase = TimeOfDay.Day;
            phaseProgress = abs / dayDuration;
        }
        else if (abs < dayDuration + sunsetDuration)
        {
            phase = TimeOfDay.Sunset;
            phaseProgress = (abs - dayDuration) / sunsetDuration;
        }
        else if (abs < dayDuration + sunsetDuration + nightDuration)
        {
            phase = TimeOfDay.Night;
            phaseProgress = (abs - dayDuration - sunsetDuration) / nightDuration;
        }
        else
        {
            phase = TimeOfDay.Rainy;
            phaseProgress = (abs - dayDuration - sunsetDuration - nightDuration) / rainyDuration;
        }

        if (phase != _previousTimeForManual)
        {
            CurrentTimeOfDay = phase;
            NextTimeOfDay = GetNextTimeOfDay(phase);
            _previousTimeForManual = phase;
            OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);
        }

        PhaseProgress = phaseProgress;
        CurrentPhaseTimeRemaining = GetDuration(phase) * (1f - phaseProgress);

        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, Mathf.SmoothStep(0, 1, phaseProgress));
        OnPhaseProgress?.Invoke(phaseProgress);
    }

    private void Update()
    {
        if (useManualControl)
            UpdateManualTime();
    }

    private IEnumerator RunPhase(TimeOfDay phase, float duration)
    {
        CurrentTimeOfDay = phase;
        NextTimeOfDay = GetNextTimeOfDay(phase);
        IsTransitioning = false;
        TransitionProgress = 0f;
        CurrentPhaseTimeRemaining = duration;

        OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);

        float holdTime = Mathf.Max(0f, duration - transitionDuration);
        float holdTimer = 0f;

        while (holdTimer < holdTime)
        {
            // clamp deltaTime so a single-frame hitch (e.g. many enemies dying
            // and destroying at once) can't blow past a big chunk of the timer
            holdTimer += Mathf.Min(Time.deltaTime, 0.25f);
            PhaseProgress = Mathf.Clamp01(holdTimer / holdTime);
            CurrentPhaseTimeRemaining = duration - holdTimer;

            OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, 0f);
            OnPhaseProgress?.Invoke(PhaseProgress);
            yield return null;
        }

        IsTransitioning = true;
        float transTimer = 0f;

        while (transTimer < transitionDuration)
        {
            transTimer += Mathf.Min(Time.deltaTime, 0.25f);
            TransitionProgress = Mathf.Clamp01(transTimer / transitionDuration);
            CurrentPhaseTimeRemaining = Mathf.Max(0f, transitionDuration - transTimer);

            float smoothT = Mathf.SmoothStep(0f, 1f, TransitionProgress);
            OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, smoothT);
            OnPhaseProgress?.Invoke(1f);
            yield return null;
        }

        IsTransitioning = false;
        TransitionProgress = 1f;
        CurrentPhaseTimeRemaining = 0f;
    }

    private IEnumerator CycleFrom(TimeOfDay startPhase)
    {
        TimeOfDay[] order = { TimeOfDay.Day, TimeOfDay.Sunset, TimeOfDay.Night, TimeOfDay.Rainy };
        int startIndex = System.Array.IndexOf(order, startPhase);
        if (startIndex < 0) startIndex = 0;

        bool firstLoop = true;
        while (true)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (firstLoop && i < startIndex) continue; // skip already-passed phases only on the first loop
                TimeOfDay phase = order[i];
                yield return RunPhase(phase, GetDuration(phase));
            }
            firstLoop = false;
        }
    }

    public void SkipToNextPhase()
    {
        StopCycleCoroutine();
        _cycleCoroutine = StartCoroutine(SkipToTransitionThenCycle());
    }

    private IEnumerator SkipToTransitionThenCycle()
    {
        TimeOfDay next = GetNextTimeOfDay(CurrentTimeOfDay);

        IsTransitioning = true;
        TransitionProgress = 0f;
        float transTimer = 0f;

        while (transTimer < transitionDuration)
        {
            transTimer += Mathf.Min(Time.deltaTime, 0.25f);
            TransitionProgress = Mathf.Clamp01(transTimer / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, TransitionProgress);
            OnTransitionProgress?.Invoke(CurrentTimeOfDay, next, smoothT);
            OnPhaseProgress?.Invoke(1f);
            yield return null;
        }

        IsTransitioning = false;
        TransitionProgress = 1f;

        _cycleCoroutine = StartCoroutine(CycleFrom(next));
    }

    public TimeOfDay GetNextTimeOfDay(TimeOfDay current) => current switch
    {
        TimeOfDay.Day => TimeOfDay.Sunset,
        TimeOfDay.Sunset => TimeOfDay.Night,
        TimeOfDay.Night => TimeOfDay.Rainy,
        TimeOfDay.Rainy => TimeOfDay.Day,
        _ => TimeOfDay.Day
    };

    public float GetDuration(TimeOfDay t) => t switch
    {
        TimeOfDay.Day => dayDuration,
        TimeOfDay.Sunset => sunsetDuration,
        TimeOfDay.Night => nightDuration,
        TimeOfDay.Rainy => rainyDuration,
        _ => dayDuration
    };

    public bool IsNightTime() => CurrentTimeOfDay == TimeOfDay.Night;

    [ContextMenu("Force Day")] public void ForceDay() => ForceTimeOfDay(TimeOfDay.Day);
    [ContextMenu("Force Sunset")] public void ForceSunset() => ForceTimeOfDay(TimeOfDay.Sunset);
    [ContextMenu("Force Night")] public void ForceNight() => ForceTimeOfDay(TimeOfDay.Night);
    [ContextMenu("Force Rainy")] public void ForceRainy() => ForceTimeOfDay(TimeOfDay.Rainy);

    private void ForceTimeOfDay(TimeOfDay timeOfDay)
    {
        CurrentTimeOfDay = timeOfDay;
        NextTimeOfDay = GetNextTimeOfDay(timeOfDay);
        TransitionProgress = 0f;
        IsTransitioning = false;
        CurrentPhaseTimeRemaining = GetDuration(timeOfDay);

        OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);
        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, 0f);

        // FIXED: continue the cycle from the forced phase instead of
        // restarting the whole loop back at Day.
        if (!useManualControl)
            RestartCycleFrom(timeOfDay);
    }
}