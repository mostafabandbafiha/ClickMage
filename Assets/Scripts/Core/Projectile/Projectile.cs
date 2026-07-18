using ClickMage.Entities;
using UnityEngine;

public class Projectile : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Element")]
    [SerializeField] private DamageType element = DamageType.Normal;
    [SerializeField] private GameObject hitVfxPrefab;

    [Header("Hit Detection")]
    [SerializeField] private float hitThreshold = 0.3f; // how close to the collider surface counts as a hit
    [SerializeField] private Vector2 hitHeightRange = new Vector2(0.3f, 0.85f); // fraction of collider height, avoids floor & head clipping
    [SerializeField] private float hitHorizontalSpread = 0.6f; // fraction of collider width/depth extents

    private Vector3 _localHitOffset; // relative to target's transform, so it follows the enemy

    [Header("On-Hit Behaviour")]
    [SerializeField] private float stickDuration = 0.2f;


    public DamageType Element => element;

    public Targetable _target;
    public float _damage;
    protected float _spawnTime;
    public BaseEntity _attacker;

    private Collider _targetCollider;
    private Vector3 _startPosition;
    private Vector3 _lastKnownTargetPosition;
    private float _journeyLength;
    private float _rawT;
    private bool _hasHit;
    private float _hitTime;

    public void Initialize(Targetable target, float damage, BaseEntity attacker)
    {
        _attacker = attacker;
        _target = target;
        _damage = damage;
        _spawnTime = Time.time;
        _startPosition = transform.position;
        _hasHit = false;
        _rawT = 0f;

        _targetCollider = target.GetComponent<Collider>();
        if (_targetCollider == null)
            _targetCollider = target.GetComponentInChildren<Collider>();

        Vector3 aimPoint = PickHitPoint(target);
        _localHitOffset = target.transform.InverseTransformPoint(aimPoint);

        _lastKnownTargetPosition = aimPoint;
        _journeyLength = Vector3.Distance(_startPosition, _lastKnownTargetPosition);
    }

    private Vector3 PickHitPoint(Targetable target)
    {
        if (_targetCollider == null)
            return target.Position;

        Bounds b = _targetCollider.bounds;

        float rx = Random.Range(-hitHorizontalSpread, hitHorizontalSpread) * b.extents.x;
        float rz = Random.Range(-hitHorizontalSpread, hitHorizontalSpread) * b.extents.z;
        // bounds.center is mid-height; offset upward within [hitHeightRange] of full height,
        // measured from the bottom of the collider, so it never lands on the floor
        float bottomY = b.center.y - b.extents.y;
        float ry = bottomY + b.size.y * Random.Range(hitHeightRange.x, hitHeightRange.y);

        Vector3 randomInterior = new Vector3(b.center.x + rx, ry, b.center.z + rz);

        // Project that random interior point onto the actual collider surface
        return _targetCollider.ClosestPoint(randomInterior);
    }
    public virtual void OnSpawn()
    {
        _hasHit = false;
        _rawT = 0f;
        transform.SetParent(null);
    }

    public virtual void OnDespawn()
    {
        _target = null;
        _attacker = null;
        _targetCollider = null;
    }

    private void Update()
    {
        if (_hasHit)
        {
            if (Time.time >= _hitTime + StickDuration)
                PoolManager.Instance.Release(gameObject);
            return;
        }

        if (Time.time >= _spawnTime + lifetime)
        {
            PoolManager.Instance.Release(gameObject);
            return;
        }

        if (_target != null && _target.IsAlive)
            _lastKnownTargetPosition = _target.transform.TransformPoint(_localHitOffset);

        _journeyLength = Vector3.Distance(_startPosition, _lastKnownTargetPosition);

        _rawT += (speed / _journeyLength) * Time.deltaTime;
        _rawT = Mathf.Clamp01(_rawT);

        float easedT = speedCurve.Evaluate(_rawT);

        Vector3 flatPosition = Vector3.Lerp(_startPosition, _lastKnownTargetPosition, easedT);
        float arc = Mathf.Sin(easedT * Mathf.PI) * arcHeight;
        Vector3 newPosition = flatPosition + Vector3.up * arc;

        Vector3 moveDirection = newPosition - transform.position;
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        transform.position = newPosition;

        // Distance check against the fixed aim point, not a re-derived closest point
        if (Vector3.Distance(newPosition, _lastKnownTargetPosition) <= hitThreshold)
        {
            Quaternion hitRotation = Quaternion.LookRotation((_lastKnownTargetPosition - _startPosition).normalized);
            ResolveHit(_lastKnownTargetPosition, hitRotation);
            return;
        }

        if (_rawT >= 1f)
            ResolveHit(newPosition, transform.rotation);
    }

    private void ResolveHit(Vector3 hitPoint, Quaternion hitRotation)
    {
        if (_hasHit) return;
        _hasHit = true;
        _hitTime = Time.time;

        transform.position = hitPoint;
        transform.rotation = hitRotation;

        OnHit(hitPoint, hitRotation, _damage);
    }

    protected virtual void OnHit(Vector3 hitPoint, Quaternion hitRotation, float damage)
    {
        if (_target == null || !_target.IsAlive) return;

        _target.TakeDamage(damage, _attacker, element);
        SpawnHitVfx(hitPoint, hitRotation);
        ApplyElementalStatus(_target);

        transform.SetParent(_target.transform, worldPositionStays: true);
    }

    protected virtual float StickDuration => stickDuration;

    protected void SpawnHitVfx(Vector3 position, Quaternion rotation)
    {
        if (hitVfxPrefab == null) return;
        GameObject vfx = Instantiate(hitVfxPrefab, position, rotation);
        Destroy(vfx, 2f);
    }

    protected void ApplyElementalStatus(Targetable target)
    {
        if (element == DamageType.Normal || _attacker == null || target == null) return;

        var handler = target.GetComponent<StatusEffectHandler>();
        if (handler == null) return;

        foreach (DamageType flag in System.Enum.GetValues(typeof(DamageType)))
        {
            if (flag == DamageType.Normal) continue;
            if ((element & flag) != flag) continue;

            string prefix = flag.ToString();
            float duration = _attacker.GetStatValueSafe($"{prefix}OnHitDuration");
            if (duration <= 0f) continue;

            float tickInterval = _attacker.GetStatValueSafe($"{prefix}OnHitTick");
            float damagePerTick = _attacker.GetStatValueSafe($"{prefix}OnHitDamage");

            handler.Apply(new StatusEffectInstance
            {
                EffectId = prefix.ToLower(),
                TimeRemaining = duration,
                TickInterval = tickInterval > 0f ? tickInterval : 1f,
                TickTimer = tickInterval > 0f ? tickInterval : 1f,
                DamagePerTick = damagePerTick,
                MaxStacks = 3,
                Stacks = 1
            });
        }
    }
}