using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class FogVolume : MonoBehaviour
{
    [Header("Appearance")]
    [ColorUsage(true,true)] public Color colorBottom = new Color(0.45f, 0.65f, 0.9f);
    [ColorUsage(true,true)] public Color colorTop    = new Color(0.75f, 0.88f, 1f);
    [Range(0.1f,4f)] public float heightGradPow = 1.5f;

    [Header("Noise")]
    public float noiseScale = 0.3f;
    public Vector2 speed1   = new Vector2(0.018f,  0.009f);
    public Vector2 speed2   = new Vector2(-0.011f, 0.016f);
    public float secondaryScale = 1.6f;
    [Range(1f,8f)] public float contrast = 2.5f;
    [Range(0f,1f)] public float opacity  = 0.55f;

    [Header("Edge Fade")]
    [Range(0.01f,0.49f)] public float edgeFade   = 0.18f;
    [Range(0.01f,0.49f)] public float groundFade = 0.22f;

    [Header("Vertex Wave")]
    [Range(0f,0.3f)] public float waveHeight = 0.06f;
    public float waveSpeed = 0.6f;
    public float waveScale = 1.5f;

    [Header("Raymarching")]
    [Range(8,64)]        public int   marchSteps  = 24;
    [Range(0.005f,0.1f)] public float stepSize    = 0.02f;
    [Range(0.1f,4f)]     public float densityMult = 1.2f;
    public float lodDistance = 20f;

    // OPT 5: runtime LOD — reduce steps with distance and on mobile
    [Header("Runtime LOD")]
    public bool  autoLOD        = true;
    public float lodNearDist    = 10f;
    public float lodFarDist     = 40f;
    public int   lodNearSteps   = 32;
    public int   lodFarSteps    = 10;

    [Header("Debug")]
    public bool ignoreDepth;

    static readonly int P_ColorBottom    = Shader.PropertyToID("_ColorBottom");
    static readonly int P_ColorTop       = Shader.PropertyToID("_ColorTop");
    static readonly int P_HeightGradPow  = Shader.PropertyToID("_HeightGradPow");
    static readonly int P_NoiseScale     = Shader.PropertyToID("_NoiseScale");
    static readonly int P_Speed1         = Shader.PropertyToID("_Speed1");
    static readonly int P_Speed2         = Shader.PropertyToID("_Speed2");
    static readonly int P_SecondaryScale = Shader.PropertyToID("_SecondaryScale");
    static readonly int P_Contrast       = Shader.PropertyToID("_Contrast");
    static readonly int P_Opacity        = Shader.PropertyToID("_Opacity");
    static readonly int P_EdgeFade       = Shader.PropertyToID("_EdgeFade");
    static readonly int P_GroundFade     = Shader.PropertyToID("_GroundFade");
    static readonly int P_WaveHeight     = Shader.PropertyToID("_WaveHeight");
    static readonly int P_WaveSpeed      = Shader.PropertyToID("_WaveSpeed");
    static readonly int P_WaveScale      = Shader.PropertyToID("_WaveScale");
    static readonly int P_Steps          = Shader.PropertyToID("_Steps");
    static readonly int P_StepSize       = Shader.PropertyToID("_StepSize");
    static readonly int P_DensityMult    = Shader.PropertyToID("_DensityMult");
    static readonly int P_LODDistance    = Shader.PropertyToID("_LODDistance");
    static readonly int P_IgnoreDepth    = Shader.PropertyToID("_IgnoreDepth");

    MeshRenderer          _renderer;
    MaterialPropertyBlock _mpb;
    Camera                _cam;

    public void SetDayNightBlend(float t) { }

    void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        _mpb      = new MaterialPropertyBlock();
        _cam      = Camera.main;
    }

    void Update()
    {
        // OPT 5: auto LOD — adjust step count by camera distance
        if (autoLOD && _cam != null)
        {
            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            marchSteps = (int)Mathf.Lerp(lodNearSteps, lodFarSteps,
                             Mathf.InverseLerp(lodNearDist, lodFarDist, dist));
        }

        Push();
    }

    void Push()
    {
        if (!_renderer || _mpb == null) return;
        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetColor(P_ColorBottom,    colorBottom);
        _mpb.SetColor(P_ColorTop,       colorTop);
        _mpb.SetFloat(P_HeightGradPow,  heightGradPow);
        _mpb.SetFloat(P_NoiseScale,     noiseScale);
        _mpb.SetVector(P_Speed1,  new Vector4(speed1.x, speed1.y, 0, 0));
        _mpb.SetVector(P_Speed2,  new Vector4(speed2.x, speed2.y, 0, 0));
        _mpb.SetFloat(P_SecondaryScale, secondaryScale);
        _mpb.SetFloat(P_Contrast,       contrast);
        _mpb.SetFloat(P_Opacity,        opacity);
        _mpb.SetFloat(P_EdgeFade,       edgeFade);
        _mpb.SetFloat(P_GroundFade,     groundFade);
        _mpb.SetFloat(P_WaveHeight,     waveHeight);
        _mpb.SetFloat(P_WaveSpeed,      waveSpeed);
        _mpb.SetFloat(P_WaveScale,      waveScale);
        _mpb.SetFloat(P_Steps,          marchSteps);
        _mpb.SetFloat(P_StepSize,       stepSize);
        _mpb.SetFloat(P_DensityMult,    densityMult);
        _mpb.SetFloat(P_LODDistance,    lodDistance);
        _mpb.SetFloat(P_IgnoreDepth,    ignoreDepth ? 1f : 0f);

        _renderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.matrix = transform.localToWorldMatrix;
        Handles.color  = new Color(colorTop.r, colorTop.g, colorTop.b, 0.25f);
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.matrix = Matrix4x4.identity;
    }
#endif
}