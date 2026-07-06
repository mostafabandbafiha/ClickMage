// FloatingDamageText.cs
using UnityEngine;
using RTLTMPro;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private RTLTextMeshPro _text;
    [SerializeField] private float _floatSpeed = 1.5f;
    [SerializeField] private float _lifetime = 0.8f;
    [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Pop Scale")]
    [SerializeField]
    private AnimationCurve _scaleCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 1.25f),
        new Keyframe(0.3f, 1f),
        new Keyframe(1f, 1f)
    );

    [Header("Random Spawn Spread")]
    [SerializeField] private float _randomXRange = 0.4f;
    [SerializeField] private float _randomYRange = 0.2f;
    [SerializeField] private float _randomFloatSpeedJitter = 0.3f; // +/- variance so they don't all move identically

    private float _t;
    private Camera _cam;
    private Color _baseColor;
    private Vector3 _baseScale;
    private float _floatSpeedActual;

    public void Init(float amount, Color color, Camera cam)
    {
        _text.text = Mathf.RoundToInt(amount).ToString();
        _baseColor = color;
        _text.color = color;
        _cam = cam;
        _t = 0f;
        _baseScale = transform.localScale;

        // Random spawn offset so simultaneous hits don't fully overlap
        Vector3 randomOffset = new Vector3(
            Random.Range(-_randomXRange, _randomXRange),
            Random.Range(0f, _randomYRange),
            0f);
        transform.position += randomOffset;

        _floatSpeedActual = _floatSpeed + Random.Range(-_randomFloatSpeedJitter, _randomFloatSpeedJitter);

        if (_cam != null)
            transform.rotation = _cam.transform.rotation;
    }

    private void Update()
    {
        _t += Time.deltaTime / _lifetime;
        transform.position += Vector3.up * _floatSpeedActual * Time.deltaTime;

        var c = _baseColor;
        c.a = _alphaCurve.Evaluate(_t);
        _text.color = c;

        transform.localScale = _baseScale * _scaleCurve.Evaluate(_t);

        if (_t >= 1f)
            Destroy(gameObject);
    }
}