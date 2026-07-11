using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeIndicator : MonoBehaviour
{
    [SerializeField] private int _segments = 64;
    [SerializeField] private float _yOffset = 0.05f; // avoid z-fighting with ground

    private LineRenderer _line;
    private Transform _followTarget;
    private float _radius = -1f;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.loop = true;
        _line.useWorldSpace = false;
        _line.positionCount = _segments;
    }

    public void Show(Transform target, float radius)
    {
        _followTarget = target;
        SetRadius(radius);
        gameObject.SetActive(true);
        SnapToTarget();
    }

    public void Hide()
    {
        _followTarget = null;
        gameObject.SetActive(false);
    }

    public void SetRadius(float radius)
    {
        if (Mathf.Approximately(_radius, radius)) return; // skip recompute if unchanged
        _radius = radius;

        for (int i = 0; i < _segments; i++)
        {
            float angle = (i / (float)_segments) * Mathf.PI * 2f;
            _line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void LateUpdate()
    {
        if (_followTarget != null)
            SnapToTarget();
    }

    private void SnapToTarget()
    {
        Vector3 pos = _followTarget.position;
        pos.y += _yOffset;
        transform.position = pos;
    }
}