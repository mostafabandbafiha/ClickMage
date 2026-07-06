using UnityEngine;

/// <summary>
/// Orchestrator: listens to DayNightCycleManager events and
/// activates / deactivates the correct weather effect system.
///
/// Rain is tracked with two independent flags so night rain and the
/// Rainy phase never cancel each other out.
/// Meteors are completely decoupled from rain — both run freely at night.
/// </summary>
public class WeatherEffectController : MonoBehaviour
{
    [Header("Effect Systems")]
    [SerializeField] private RainSystem rainSystem;
    [SerializeField] private ThunderSystem thunderSystem;
    [SerializeField] private MeteorShowerSystem meteorShowerSystem;

    [Header("Night Rain Settings")]
    [Tooltip("Probability (0-1) that rain falls during a Night phase.")]
    [SerializeField, Range(0f, 1f)] private float nightRainChance = 0.4f;
    [Tooltip("Rain intensity at night (lighter than a full rainy day).")]
    [SerializeField, Range(0f, 1f)] private float nightRainIntensity = 0.4f;

    // ── Two independent rain reasons ──────────────────────────────
    // Rain stays ON as long as either flag is true.
    // This means meteors spawning at night NEVER touches these flags
    // and therefore never accidentally kills the rain.
    private bool rainyPhaseRain = false;   // true during the Rainy phase
    private bool nightRain = false;   // true when the random night roll passes

    // ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (DayNightCycleManager.Instance == null)
        {
            Debug.LogWarning("[WeatherEffectController] DayNightCycleManager not found.");
            return;
        }

        DayNightCycleManager.Instance.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        DayNightCycleManager.Instance.OnTransitionProgress += HandleTransitionProgress;

        // Sync to whatever phase is already running on startup
        HandleTimeOfDayChanged(DayNightCycleManager.Instance.CurrentTimeOfDay);
    }

    private void OnDestroy()
    {
        if (DayNightCycleManager.Instance == null) return;
        DayNightCycleManager.Instance.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        DayNightCycleManager.Instance.OnTransitionProgress -= HandleTransitionProgress;
    }

    // ── Phase change ──────────────────────────────────────────────

    private void HandleTimeOfDayChanged(TimeOfDay newTime)
    {
        switch (newTime)
        {
            case TimeOfDay.Day:
                rainyPhaseRain = false;
                nightRain = false;
                SetThunder(false);
                SetMeteors(false);
                break;

            case TimeOfDay.Sunset:
                rainyPhaseRain = false;
                nightRain = false;
                SetThunder(false);
                SetMeteors(false);
                break;

            case TimeOfDay.Night:
                rainyPhaseRain = false;
                nightRain = Random.value < nightRainChance;  // roll once per night
                SetThunder(false);
                SetMeteors(true);   // meteors always run — rain flags handle rain separately
                break;

            case TimeOfDay.Rainy:
                rainyPhaseRain = true;
                nightRain = false;
                SetThunder(true);
                SetMeteors(false);
                break;
        }

        // Let the unified rain method decide what to do based on both flags
        ApplyRainState();

        Debug.Log($"[WeatherEffectController] Phase → {newTime} | " +
                  $"rainyPhaseRain={rainyPhaseRain} nightRain={nightRain}");
    }

    // ── Transition blending ───────────────────────────────────────

    private void HandleTransitionProgress(TimeOfDay from, TimeOfDay to, float blendT)
    {
        if (rainSystem == null) return;

        // ── INTO Rainy ────────────────────────────────────────────
        if (to == TimeOfDay.Rainy)
        {
            rainSystem.SetActive(true);
            rainSystem.SetIntensity(blendT);
            return;
        }

        // ── OUT of Rainy ──────────────────────────────────────────
        if (from == TimeOfDay.Rainy)
        {
            if (to == TimeOfDay.Night && nightRain)
                // Seamless cross-fade: full rainy → night rain intensity
                rainSystem.SetIntensity(Mathf.Lerp(1f, nightRainIntensity, blendT));
            else
                rainSystem.SetIntensity(1f - blendT);
            return;
        }

        // ── INTO Night (with rain rolled) ─────────────────────────
        if (to == TimeOfDay.Night && nightRain)
        {
            rainSystem.SetActive(true);
            rainSystem.SetIntensity(blendT * nightRainIntensity);
            return;
        }

        // ── OUT of Night ──────────────────────────────────────────
        if (from == TimeOfDay.Night)
        {
            if (nightRain)
            {
                if (to == TimeOfDay.Rainy)
                    // Night rain cross-fades up into full rainy intensity
                    rainSystem.SetIntensity(Mathf.Lerp(nightRainIntensity, 1f, blendT));
                else
                    rainSystem.SetIntensity((1f - blendT) * nightRainIntensity);
            }
            return;
        }
    }

    // ── Unified rain state ────────────────────────────────────────

    /// Reads both flags and sets rain active/intensity accordingly.
    /// Called after any flag changes. This is the ONLY place SetRain is called
    /// from phase-change logic — transitions call SetIntensity directly for smooth blending.
    private void ApplyRainState()
    {
        if (rainyPhaseRain)
        {
            // Full Rainy phase — full intensity
            SetRain(true, 1f);
        }
        else if (nightRain)
        {
            // Night rain — lighter intensity
            SetRain(true, nightRainIntensity);
        }
        else
        {
            // Neither flag active — turn rain off
            SetRain(false, 0f);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetRain(bool active, float intensity)
    {
        if (rainSystem == null) return;
        rainSystem.SetActive(active);
        rainSystem.SetIntensity(intensity);
    }

    private void SetThunder(bool active)
    {
        if (thunderSystem != null) thunderSystem.SetActive(active);
    }

    private void SetMeteors(bool active)
    {
        if (meteorShowerSystem != null) meteorShowerSystem.SetActive(active);
    }
}