using UnityEngine;

public class DayNightLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light directionalLight;

    [Header("Per-Phase Light Settings")]
    [SerializeField] private PhaseLightSettings dayLight;
    [SerializeField] private PhaseLightSettings sunsetLight;
    [SerializeField] private PhaseLightSettings nightLight;
    [SerializeField] private PhaseLightSettings rainyLight;

    [Header("Sun / Moon Sprites (optional)")]
    [SerializeField] private GameObject sunDisc;
    [SerializeField] private GameObject moonDisc;

    private void Start()
    {
        var mgr = DayNightCycleManager.Instance;
        if (mgr == null) return;

        mgr.OnTransitionProgress += HandleTransition;
        HandleTransition(mgr.CurrentTimeOfDay, mgr.NextTimeOfDay, 0f);
    }

    private void OnDestroy()
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTransitionProgress -= HandleTransition;
    }

    private void HandleTransition(TimeOfDay from, TimeOfDay to, float t)
    {
        var sFrom = GetSettings(from);
        var sTo = GetSettings(to);

        bool hasMidpoint = sFrom.useMidpoint || sTo.useMidpoint;
        PhaseLightSettings midSource = sFrom.useMidpoint ? sFrom : sTo;

        if (hasMidpoint)
        {
            if (t < 0.5f)
            {
                float t1 = t / 0.5f;
                ApplyLerp(sFrom, midSource.midpoint, t1);
                directionalLight.intensity = Mathf.Lerp(sFrom.intensity, midSource.midpoint.intensity, t1);
            }
            else
            {
                float t2 = (t - 0.5f) / 0.5f;
                ApplyLerp(midSource.midpoint, sTo, t2);
                directionalLight.intensity = Mathf.Lerp(midSource.midpoint.intensity, sTo.intensity, t2);
            }
        }
        else
        {
            // ✅ Now works with the new overload
            ApplyLerp(sFrom, sTo, t);
            directionalLight.intensity = Mathf.Lerp(sFrom.intensity, sTo.intensity, t);
        }

        // Sun/Moon swap
        if (sunDisc != null && moonDisc != null)
        {
            if (from == TimeOfDay.Sunset && to == TimeOfDay.Night)
            {
                sunDisc.SetActive(t < 0.5f);
                moonDisc.SetActive(t >= 0.5f);
            }
            else
            {
                bool showMoon = (from == TimeOfDay.Night || to == TimeOfDay.Night);
                sunDisc.SetActive(!showMoon);
                moonDisc.SetActive(showMoon);
            }
        }
    }


    // ── Shared lerp logic ─────────────────────────────────────────
    private void ApplyLerp(PhaseLightSettings a, PhaseLightSettings b, float t)
    {
        directionalLight.transform.rotation = Quaternion.Slerp(
            Quaternion.Euler(a.rotation),
            Quaternion.Euler(b.rotation),
            t
        );

        directionalLight.color = Color.Lerp(a.lightColor, b.lightColor, t);
        RenderSettings.ambientLight = Color.Lerp(a.ambientColor, b.ambientColor, t);
    }

    private void ApplyLerp(PhaseLightSettings a, PhasePoint b, float t)
    {
        directionalLight.transform.rotation = Quaternion.Slerp(
            Quaternion.Euler(a.rotation),
            Quaternion.Euler(b.rotation),
            t
        );

        directionalLight.color = Color.Lerp(a.lightColor, b.lightColor, t);
        RenderSettings.ambientLight = Color.Lerp(a.ambientColor, b.ambientColor, t);
    }

    private void ApplyLerp(PhasePoint a, PhaseLightSettings b, float t)
    {
        directionalLight.transform.rotation = Quaternion.Slerp(
            Quaternion.Euler(a.rotation),
            Quaternion.Euler(b.rotation),
            t
        );

        directionalLight.color = Color.Lerp(a.lightColor, b.lightColor, t);
        RenderSettings.ambientLight = Color.Lerp(a.ambientColor, b.ambientColor, t);
    }

    private PhaseLightSettings GetSettings(TimeOfDay t) => t switch
    {
        TimeOfDay.Day => dayLight,
        TimeOfDay.Sunset => sunsetLight,
        TimeOfDay.Night => nightLight,
        TimeOfDay.Rainy => rainyLight,
        _ => dayLight
    };
}

// ── Shared point data (used for midpoint) ─────────────────────────
[System.Serializable]
public class PhasePoint
{
    public Color lightColor = Color.white;
    public Color ambientColor = new Color(0.2f, 0.2f, 0.2f);

    [Range(0f, 2f)]
    public float intensity = 0f;

    [Tooltip("X = altitude  Y = direction  Z = tilt")]
    public Vector3 rotation = new Vector3(270, -30, 0);
}

// ── Per-phase settings ────────────────────────────────────────────
[System.Serializable]
public class PhaseLightSettings
{
    [Header("Light Properties")]
    public Color lightColor = Color.white;
    public Color ambientColor = new Color(0.2f, 0.2f, 0.2f);

    [Range(0f, 2f)]
    public float intensity = 1f;

    [Tooltip("X = altitude  Y = direction  Z = tilt")]
    public Vector3 rotation = new Vector3(50, -30, 0);

    [Header("Midpoint (optional)")]
    [Tooltip("Enable to add a midpoint during transition (e.g. sun sets, moon rises)")]
    public bool useMidpoint = false;
    public PhasePoint midpoint;
}
