using UnityEngine;


public class GrassDisplacementPainter : MonoBehaviour
{
    [Header("Painter Settings")]
    public float strength = 1f;

    private GameObject _proxy;
    private Material _painterMaterial;

    void Start()
    {
        int layer = LayerMask.NameToLayer("GrassDisplacement");
        if (layer == -1)
        {
            Debug.LogError("[Painter] 'GrassDisplacement' layer not found. Add it in Project Settings.");
            return;
        }

        var shader = Shader.Find("Custom/DisplacementPainter");
        if (shader == null)
        {
            Debug.LogError("[Painter] Shader 'Custom/DisplacementPainter' not found.");
            return;
        }

        _painterMaterial = new Material(shader);
        _painterMaterial.SetFloat("_Strength", strength);

        BuildProxy(layer);
    }

    void BuildProxy(int layer)
    {
        var col = GetComponentInParent<Collider>();

        if (col is CapsuleCollider cap)
        {
            _proxy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            // Unity capsule primitive is 2 units tall and 1 unit wide by default
            // CapsuleCollider.height includes both hemispheres
            _proxy.transform.localScale = new Vector3(
                cap.radius * 2f,
                cap.height * 0.5f,
                cap.radius * 2f
            );
            _proxy.transform.localPosition = cap.center;
        }
        else if (col is SphereCollider sph)
        {
            _proxy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float d = sph.radius * 2f;
            _proxy.transform.localScale = new Vector3(d, d, d);
            _proxy.transform.localPosition = sph.center;
        }
        else if (col is BoxCollider box)
        {
            _proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _proxy.transform.localScale = box.size;
            _proxy.transform.localPosition = box.center;
        }
        else
        {
            // fallback: small sphere
            _proxy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _proxy.transform.localScale = Vector3.one;
            _proxy.transform.localPosition = Vector3.zero;
            Debug.LogWarning("[Painter] Unsupported collider type, using fallback sphere.");
        }

        // remove the auto-added collider on the primitive
        var c = _proxy.GetComponent<Collider>();
        if (c != null) Destroy(c);

        // parent after setting local scale/position so they apply in local space
        _proxy.transform.SetParent(transform, false);
        _proxy.transform.localRotation = Quaternion.identity;

        _proxy.layer = layer;
        _proxy.name = "DisplacementProxy";

        var mr = _proxy.GetComponent<MeshRenderer>();
        mr.material = _painterMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    // call this at runtime if you change strength dynamically
    public void SetStrength(float value)
    {
        strength = value;
        if (_painterMaterial != null)
            _painterMaterial.SetFloat("_Strength", Mathf.Clamp01(value));
    }

    void OnDestroy()
    {
        if (_proxy != null) Destroy(_proxy);
        if (_painterMaterial != null) Destroy(_painterMaterial);
    }
}
