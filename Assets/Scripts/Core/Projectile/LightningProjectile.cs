using ClickMage.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningProjectile : MonoBehaviour
{
    [Header("Lightning Settings")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private int _segmentCount = 12;
    [SerializeField] private float _zigzagStrength = 0.35f;

    [Header("Timing")]
    [SerializeField] private float _flashDuration = 0.05f;
    [SerializeField] private float _gapBetweenJumps = 0.03f;
    [SerializeField] private int _flickerCount = 2;

    private readonly List<Targetable> _chainTargets = new();
    private Vector3 _startPoint;
    private float _damage;
    private float _chainDamageMultiplier;
    private bool _playing;
    public BaseEntity _attacker;

    public void Initialize(
        Vector3 startPoint,
        List<Targetable> chainTargets,
        float damage,
        float chainDamageMultiplier,
        BaseEntity attacker)
    {
        _attacker = attacker;
        _startPoint = startPoint;
        _chainTargets.Clear();

        if (chainTargets != null)
            _chainTargets.AddRange(chainTargets);

        _damage = damage;
        _chainDamageMultiplier = chainDamageMultiplier;

        if (_lineRenderer != null)
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 0;
        }

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (_playing) yield break;
        _playing = true;

        if (_lineRenderer == null || _chainTargets.Count == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        float currentDamage = _damage;

        for (int i = 0; i < _chainTargets.Count; i++)
        {
            Targetable target = _chainTargets[i];
            if (target == null || !target.IsAlive)
                break;

            Vector3 from = (i == 0) ? _startPoint : _chainTargets[i - 1].Position;
            Vector3 to = target.Position;

            // short "BO zombies" style flicker before impact
            for (int flicker = 0; flicker < _flickerCount; flicker++)
            {
                DrawLightning(from, to);
                yield return new WaitForSeconds(_flashDuration);
                _lineRenderer.positionCount = 0;
                yield return new WaitForSeconds(_gapBetweenJumps);
            }

            DrawLightning(from, to);
            yield return new WaitForSeconds(_flashDuration);

            target.TakeDamage(currentDamage, _attacker);
            currentDamage *= _chainDamageMultiplier;

            _lineRenderer.positionCount = 0;
            yield return new WaitForSeconds(_gapBetweenJumps);
        }

        Destroy(gameObject);
    }

    private void DrawLightning(Vector3 from, Vector3 to)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.positionCount = _segmentCount;

        Vector3 direction = (to - from).normalized;

        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
        if (perpendicular.sqrMagnitude < 0.001f)
            perpendicular = Vector3.Cross(direction, Vector3.right);

        perpendicular.Normalize();

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            Vector3 point = Vector3.Lerp(from, to, t);

            if (i > 0 && i < _segmentCount - 1)
            {
                float zigzag = Random.Range(-_zigzagStrength, _zigzagStrength);
                float vertical = Random.Range(-_zigzagStrength * 0.5f, _zigzagStrength * 0.5f);

                point += perpendicular * zigzag;
                point += Vector3.up * vertical;
            }

            _lineRenderer.SetPosition(i, point);
        }
    }
}