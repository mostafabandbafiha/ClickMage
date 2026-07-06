using UnityEngine;

public class GrassManager : MonoBehaviour
{
    [System.Serializable]
    public struct GrassPhase
    {
        public Color baseColor;

        public Color tipColor;
        public Color windTint;
    }

    [Header("Transition Curves")]
    public AnimationCurve dayToSunset = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve sunsetToNight = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve nightToDay = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve rainyBlend = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Grass Colors Per Phase")]
    public GrassPhase day;
    public GrassPhase sunset;
    public GrassPhase night;
    public GrassPhase rainy;


    private DayNightCycleManager dayNight;

    void Start()
    {
        dayNight = DayNightCycleManager.Instance;
        dayNight.OnTimeOfDayChanged += OnPhaseChanged;
        dayNight.OnTransitionProgress += OnTransition;
    }

    void OnDestroy()
    {
        if (dayNight == null) return;
        dayNight.OnTimeOfDayChanged -= OnPhaseChanged;
        dayNight.OnTransitionProgress -= OnTransition;
    }

    void OnPhaseChanged(TimeOfDay phase)
    {
        ApplyPhase(GetPhase(phase));
    }

    void OnTransition(TimeOfDay from, TimeOfDay to, float t)
    {
        GrassPhase a = GetPhase(from);
        GrassPhase b = GetPhase(to);

        AnimationCurve curve = GetCurve(from, to);
        float curvedT = curve != null ? curve.Evaluate(t) : t;

        GrassPhase blended = new GrassPhase
        {
            baseColor = Color.Lerp(a.baseColor, b.baseColor, curvedT),
            //midColor = Color.Lerp(a.midColor, b.midColor, curvedT),
            tipColor = Color.Lerp(a.tipColor, b.tipColor, curvedT),
            windTint = Color.Lerp(a.windTint, b.windTint, curvedT)
        };

        ApplyPhase(blended);
    }

    // Blends through a mid color instead of going direct A -> B
    AnimationCurve GetCurve(TimeOfDay from, TimeOfDay to)
    {
        if (from == TimeOfDay.Day && to == TimeOfDay.Sunset)
            return dayToSunset;

        if (from == TimeOfDay.Sunset && to == TimeOfDay.Night)
            return sunsetToNight;

        if (from == TimeOfDay.Night && to == TimeOfDay.Day)
            return nightToDay;

        if (to == TimeOfDay.Rainy)
            return rainyBlend;

        return null;
    }

    void ApplyPhase(GrassPhase p)
    {
        Shader.SetGlobalColor("_BaseColor", p.baseColor);
        Shader.SetGlobalColor("_TipColor", p.tipColor);
        Shader.SetGlobalColor("_WindTint", p.windTint);
        // midColor is only used for blending in C#, not sent to shader
    }

    GrassPhase GetPhase(TimeOfDay phase)
    {
        return phase switch
        {
            TimeOfDay.Day => day,
            TimeOfDay.Sunset => sunset,
            TimeOfDay.Night => night,
            TimeOfDay.Rainy => rainy,
            _ => day
        };
    }
}
