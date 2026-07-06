using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Blends URP post-process profiles including LUT using the custom LUTBlenderComponent
public class DayNightPostProcessingURP : MonoBehaviour
{
    [Header("Base Volume (leave your scene volume here)")]
    [SerializeField] private Volume blendVolume;

    [Header("Per-Phase Settings")]
    [SerializeField] private PhasePostSettings daySettings;
    [SerializeField] private PhasePostSettings sunsetSettings;
    [SerializeField] private PhasePostSettings nightSettings;
    [SerializeField] private PhasePostSettings rainySettings;
    public UniversalRendererData rendererData; // ✨ ADD THIS - Assign your URP Renderer Asset
    //private ColorLookup colorLookup; // ✨ ADD THIS - Unity's built-in Color Lookup

    // Runtime volume for blending effects
    //private Volume blendVolume;

    // LUT Blender (custom component)
    private LUTBlenderComponent lutBlender;

    // Other effects
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    // Track current transition state
    private PhasePostSettings currentFrom;
    private PhasePostSettings currentTo;

    private void Awake()
    {
        //blendVolume = CreateBlendVolume("PPBlend", sceneVolume.priority + 1);

        GetOrAddOverride(blendVolume, out lutBlender);
        GetOrAddOverride(blendVolume, out bloom);
        GetOrAddOverride(blendVolume, out colorAdjustments);
        GetOrAddOverride(blendVolume, out vignette);
        //GetOrAddOverride(blendVolume, out colorLookup); // ✨ ADD THIS

        Debug.Log($"LUTBlender created: {lutBlender != null}");
        Debug.Log($"Blend volume profile: {blendVolume.profile.name}");

    }

    private void Start()
    {
        var mgr = DayNightCycleManager.Instance;
        if (mgr == null) return;

        mgr.OnTransitionProgress += HandleTransition;

        // Initialize to current phase
        currentFrom = GetSettings(mgr.CurrentTimeOfDay);
        currentTo = GetSettings(mgr.NextTimeOfDay);

        ApplyBlend(currentFrom, currentTo, 0f);
    }

    private void OnDestroy()
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTransitionProgress -= HandleTransition;
    }

    /// Called every frame by DayNightCycleManager
    private void HandleTransition(TimeOfDay from, TimeOfDay to, float t)
    {
        var settingsFrom = GetSettings(from);
        var settingsTo = GetSettings(to);

        // Update cached settings if phase changed
        if (currentFrom != settingsFrom || currentTo != settingsTo)
        {
            currentFrom = settingsFrom;
            currentTo = settingsTo;
        }

        ApplyBlend(settingsFrom, settingsTo, t);
    }

    /// Apply blended settings based on transition progress
    private void ApplyBlend(PhasePostSettings from, PhasePostSettings to, float t)
    {
        blendVolume.weight = 1f;

        // ── LUT Blending ──────────────────────────────────────────
        if (lutBlender != null)
        {
            lutBlender.lut1.Override(from.lut);
            lutBlender.lut2.Override(to.lut);
            lutBlender.blend.Override(t);
            lutBlender.intensity.Override(Mathf.Lerp(from.lutIntensity, to.lutIntensity, t));
        }

        // ── Bloom ─────────────────────────────────────────────────
        if (bloom != null)
        {
            bloom.active = true;
            bloom.intensity.Override(Mathf.Lerp(from.bloomIntensity, to.bloomIntensity, t));
            bloom.threshold.Override(Mathf.Lerp(from.bloomThreshold, to.bloomThreshold, t));
            bloom.tint.Override(Color.Lerp(from.bloomTint, to.bloomTint, t));
        }

        // ── Color Adjustments ─────────────────────────────────────
        if (colorAdjustments != null)
        {
            colorAdjustments.active = true;
            colorAdjustments.postExposure.Override(Mathf.Lerp(from.exposure, to.exposure, t));
            colorAdjustments.contrast.Override(Mathf.Lerp(from.contrast, to.contrast, t));
            colorAdjustments.saturation.Override(Mathf.Lerp(from.saturation, to.saturation, t));
            colorAdjustments.colorFilter.Override(Color.Lerp(from.colorFilter, to.colorFilter, t));
        }

        // ── Vignette ──────────────────────────────────────────────
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.Override(Mathf.Lerp(from.vignetteIntensity, to.vignetteIntensity, t));
            vignette.color.Override(Color.Lerp(from.vignetteColor, to.vignetteColor, t));
        }
    }
   

    private Volume CreateBlendVolume(string name, float priority)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = priority;
        vol.weight = 1f;
        vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        return vol;
    }

    private static void GetOrAddOverride<T>(Volume vol, out T component) where T : VolumeComponent
    {
        if (!vol.profile.TryGet(out component))
            component = vol.profile.Add<T>(true);
    }

    private PhasePostSettings GetSettings(TimeOfDay t) => t switch
    {
        TimeOfDay.Day => daySettings,
        TimeOfDay.Sunset => sunsetSettings,
        TimeOfDay.Night => nightSettings,
        TimeOfDay.Rainy => rainySettings,
        _ => daySettings
    };

    [ContextMenu("Test LUT 1")]
    private void TestLUT1()
    {
        if (lutBlender != null && daySettings.lut != null)
        {
            lutBlender.active = true;
            lutBlender.lut1.Override(daySettings.lut);
            lutBlender.lut2.Override(daySettings.lut);
            lutBlender.blend.Override(0f);
            lutBlender.intensity.Override(1f);
            Debug.Log("Forced LUT1 (Day)");
        }
    }

    [ContextMenu("Test LUT 2")]
    private void TestLUT2()
    {
        if (lutBlender != null && nightSettings.lut != null)
        {
            lutBlender.active = true;
            lutBlender.lut1.Override(nightSettings.lut);
            lutBlender.lut2.Override(nightSettings.lut);
            lutBlender.blend.Override(0f);
            lutBlender.intensity.Override(1f);
            Debug.Log("Forced LUT2 (Night)");
        }
    }

}

// ── Data container ────────────────────────────────────────────────
[System.Serializable]
public class PhasePostSettings
{
    [Header("LUT")]
    public Texture2D lut;
    [Range(0f, 1f)]
    public float lutIntensity = 1f;

    [Header("Bloom")]
    public float bloomIntensity = 0.5f;
    public float bloomThreshold = 1f;
    public Color bloomTint = Color.white;

    [Header("Color Adjustments")]
    public float exposure = 0f;
    public float contrast = 0f;
    [Range(-100f, 100f)]
    public float saturation = 0f;
    public Color colorFilter = Color.white;

    [Header("Vignette")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.2f;
    public Color vignetteColor = Color.black;
}
