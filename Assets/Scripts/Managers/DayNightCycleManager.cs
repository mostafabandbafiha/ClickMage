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
    [Tooltip("How long the blend between two phases takes (seconds)")]
    [SerializeField] private float transitionDuration = 15f;

    [Header("Testing")]
    [SerializeField] private bool useManualControl = false;
    [SerializeField, Range(0f, 1f)] private float manualTimeProgress = 0f;



    // ── Public state ──────────────────────────────────────────────
    public TimeOfDay CurrentTimeOfDay { get; private set; }
    public TimeOfDay NextTimeOfDay { get; private set; }
    public float TransitionProgress { get; private set; } // 0-1 during blend
    public bool IsTransitioning { get; private set; }
    public float PhaseProgress { get; private set; } // 0-1 inside current phase
    public float CurrentPhaseTimeRemaining { get; private set; } // NEW: seconds left until next phase fully starts

    // ── Events ────────────────────────────────────────────────────
    /// Fired once when a new phase STARTS (after transition completes)
    public event Action<TimeOfDay> OnTimeOfDayChanged;
    /// Fired every frame: from, to, blendT (0=fully 'from', 1=fully 'to')
    public event Action<TimeOfDay, TimeOfDay, float> OnTransitionProgress;
    /// Fired every frame: 0-1 progress within the stable part of the phase
    public event Action<float> OnPhaseProgress;

    // ── Private ───────────────────────────────────────────────────
    private Coroutine cycleCoroutine;
    private TimeOfDay previousTimeForManual;

    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (useManualControl)
        {
            UpdateManualTime();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        CurrentTimeOfDay = TimeOfDay.Day;
        NextTimeOfDay = TimeOfDay.Sunset;
        previousTimeForManual = TimeOfDay.Day;
        TransitionProgress = 0f;
        IsTransitioning = false;

        if (!useManualControl)
            cycleCoroutine = StartCoroutine(CycleRoutine());

        // fire initial state
        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, 0f);
    }

    // ── Inspector live editing ────────────────────────────────────
    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (useManualControl)
        {
            if (cycleCoroutine != null) { StopCoroutine(cycleCoroutine); cycleCoroutine = null; }
            UpdateManualTime();
        }
        else
        {
            if (cycleCoroutine == null)
                cycleCoroutine = StartCoroutine(CycleRoutine());
        }
    }

    // ── Manual (slider) mode ──────────────────────────────────────
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

        if (phase != previousTimeForManual)
        {
            CurrentTimeOfDay = phase;
            NextTimeOfDay = GetNextTimeOfDay(phase);
            previousTimeForManual = phase;
            OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);
        }

        PhaseProgress = phaseProgress;

        CurrentPhaseTimeRemaining = GetDuration(phase) * (1f - phaseProgress); 
        // In manual mode treat phase progress as transition progress too
        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, Mathf.SmoothStep(0, 1, phaseProgress));
        OnPhaseProgress?.Invoke(phaseProgress);
    }

    // ── Automatic cycle ───────────────────────────────────────────
    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return RunPhase(TimeOfDay.Day, dayDuration);
            yield return RunPhase(TimeOfDay.Sunset, sunsetDuration);
            yield return RunPhase(TimeOfDay.Night, nightDuration);
            yield return RunPhase(TimeOfDay.Rainy, rainyDuration);
        }
    }

    /// Runs one phase: stable hold → transition into next phase
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
            holdTimer += Time.deltaTime;
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
            transTimer += Time.deltaTime;
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

    // ── Helpers ───────────────────────────────────────────────────
    /// <summary>
    /// Smoothly transitions into the next phase by jumping straight to
    /// the transition blend portion, then continues the normal cycle.
    /// </summary>
    public void SkipToNextPhase()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(SkipToTransitionThenCycle());
    }

    private IEnumerator SkipToTransitionThenCycle()
    {
        TimeOfDay next = GetNextTimeOfDay(CurrentTimeOfDay);

        // Run only the transition blend portion (skip the hold)
        IsTransitioning = true;
        TransitionProgress = 0f;
        float transTimer = 0f;

        while (transTimer < transitionDuration)
        {
            transTimer += Time.deltaTime;
            TransitionProgress = Mathf.Clamp01(transTimer / transitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, TransitionProgress);
            OnTransitionProgress?.Invoke(CurrentTimeOfDay, next, smoothT);
            OnPhaseProgress?.Invoke(1f);
            yield return null;
        }

        IsTransitioning = false;
        TransitionProgress = 1f;

        // Now hand off to the normal cycle starting from 'next'
        cycleCoroutine = StartCoroutine(CycleFrom(next));
    }

    private IEnumerator CycleFrom(TimeOfDay startPhase)
    {
        TimeOfDay[] order = { TimeOfDay.Day, TimeOfDay.Sunset, TimeOfDay.Night, TimeOfDay.Rainy };
        int startIndex = System.Array.IndexOf(order, startPhase);

        while (true)
        {
            for (int i = 0; i < order.Length; i++)
            {
                TimeOfDay phase = order[(startIndex + i) % order.Length];
                yield return RunPhase(phase, GetDuration(phase));
            }
            startIndex = 0;
        }
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

    // ── Force methods ─────────────────────────────────────────────
    [ContextMenu("Force Day")] public void ForceDay() => ForceTimeOfDay(TimeOfDay.Day);
    [ContextMenu("Force Sunset")] public void ForceSunset() => ForceTimeOfDay(TimeOfDay.Sunset);
    [ContextMenu("Force Night")] public void ForceNight() => ForceTimeOfDay(TimeOfDay.Night);
    [ContextMenu("Force Rainy")] public void ForceRainy() => ForceTimeOfDay(TimeOfDay.Rainy);

    private void ForceTimeOfDay(TimeOfDay timeOfDay)
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);

        CurrentTimeOfDay = timeOfDay;
        NextTimeOfDay = GetNextTimeOfDay(timeOfDay);
        TransitionProgress = 0f;
        IsTransitioning = false;
        CurrentPhaseTimeRemaining = GetDuration(timeOfDay); // NEW

        OnTimeOfDayChanged?.Invoke(CurrentTimeOfDay);
        OnTransitionProgress?.Invoke(CurrentTimeOfDay, NextTimeOfDay, 0f);

        if (!useManualControl)
            cycleCoroutine = StartCoroutine(CycleRoutine());
    }
}
