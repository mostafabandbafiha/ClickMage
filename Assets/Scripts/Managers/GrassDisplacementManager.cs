using UnityEngine;

public class GrassDisplacementManager : MonoBehaviour
{

    [Header("Setup")]
    public Transform playerTransform;
    public RenderTexture displacementRT;
    public Transform areaPlane; // assign your plane here

    [Header("Fade")]
    public float fadeSpeed = 1.5f;

    private Camera _dispCam;
    private RenderTexture _rtA, _rtB;
    private Material _fadeMat;
    private Vector2 _worldOrigin;
    private float _areaSize;


    void Start()
    {
        if (areaPlane == null)
        {
            Debug.LogError("[Manager] Area plane not assigned!");
            return;
        }

        // get world origin from plane position
        _worldOrigin = new Vector2(areaPlane.position.x, areaPlane.position.z);

        // get area size from plane scale (use the larger dimension)
        // default plane is 10x10, so scale.x * 10 gives world size
        _areaSize = Mathf.Max(areaPlane.localScale.x, areaPlane.localScale.z) * 10f;

        var go = new GameObject("DisplacementCamera");
        _dispCam = go.AddComponent<Camera>();
        _dispCam.orthographic = true;
        _dispCam.orthographicSize = _areaSize * 0.5f;
        _dispCam.nearClipPlane = 0.1f;
        _dispCam.farClipPlane = 200f;
        _dispCam.transform.position = new Vector3(_worldOrigin.x, 100f, _worldOrigin.y);
        _dispCam.transform.rotation = Quaternion.Euler(90, 0, 0);
        _dispCam.cullingMask = LayerMask.GetMask("GrassDisplacement");
        _dispCam.backgroundColor = Color.black;
        _dispCam.clearFlags = CameraClearFlags.SolidColor;
        _dispCam.enabled = false;

        int res = displacementRT.width;
        _rtA = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32);
        _rtB = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32);
        _rtA.filterMode = FilterMode.Bilinear;
        _rtB.filterMode = FilterMode.Bilinear;

        _fadeMat = new Material(Shader.Find("Hidden/DisplacementFade"));

        // set globals once
        Shader.SetGlobalVector("_DisplacementBoundsCenter", new Vector4(_worldOrigin.x, 0, _worldOrigin.y, 0));
        Shader.SetGlobalVector("_DisplacementBoundsSize", new Vector4(_areaSize, _areaSize, 0, 0));
        //Shader.SetGlobalFloat("_DisplacementBoundsSize", _areaSize);

        Debug.Log($"[Manager] Area: {_areaSize}x{_areaSize} centered at {_worldOrigin}");
    }

    void LateUpdate()
    {
        if (_dispCam == null) return;

        // render current painter positions into rtB
        _dispCam.targetTexture = _rtB;
        _dispCam.Render();

        // fade accumulated history (rtA) and composite fresh stamps on top
        _fadeMat.SetTexture("_NewFrame", _rtB);
        Graphics.Blit(_rtA, displacementRT, _fadeMat);

        // copy result back to rtA for next frame
        Graphics.Blit(displacementRT, _rtA);

        Shader.SetGlobalTexture("_DisplacementTex", displacementRT);
    }


    void OnDestroy()
    {
        if (_rtA) _rtA.Release();
        if (_rtB) _rtB.Release();
    }
}
