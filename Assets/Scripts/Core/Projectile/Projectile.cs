using ClickMage.Entities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public Targetable _target;
    public float _damage;
    private float _spawnTime;
    public BaseEntity _attacker;

    private Vector3 _startPosition;
    private Vector3 _lastKnownTargetPosition;
    private float _journeyLength;
    private float _rawT;  // linear time progress 0→1

    public void Initialize(Targetable target, float damage, BaseEntity attacker)
    {
        _attacker = attacker;
        _target = target;
        _damage = damage;
        _spawnTime = Time.time;
        _startPosition = transform.position;
        _lastKnownTargetPosition = target.Position;
        _journeyLength = Vector3.Distance(_startPosition, _lastKnownTargetPosition);
    }

    private void Update()
    {
        if (Time.time >= _spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (_target != null && _target.IsAlive)
            _lastKnownTargetPosition = _target.Position;

        _journeyLength = Vector3.Distance(_startPosition, _lastKnownTargetPosition);

        // Advance raw linear progress
        _rawT += (speed / _journeyLength) * Time.deltaTime;
        _rawT = Mathf.Clamp01(_rawT);

        // Sample curve to get eased progress
        float easedT = speedCurve.Evaluate(_rawT);

        Vector3 flatPosition = Vector3.Lerp(_startPosition, _lastKnownTargetPosition, easedT);
        float arc = Mathf.Sin(easedT * Mathf.PI) * arcHeight;
        Vector3 newPosition = flatPosition + Vector3.up * arc;

        Vector3 moveDirection = newPosition - transform.position;
        if (moveDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        transform.position = newPosition;

        if (_rawT >= 1f || Vector3.Distance(transform.position, _lastKnownTargetPosition) < 0.5f)
            HitTarget();
    }

    protected virtual void OnHit(Vector3 position, float damage)
    {
        if (_target != null && _target.IsAlive)
            _target.TakeDamage(damage, _attacker);
    }


    protected virtual bool DestroyOnHit => true;

    private void HitTarget()
    {
        OnHit(transform.position, _damage);
        if (DestroyOnHit)
            Destroy(gameObject);
    }
}