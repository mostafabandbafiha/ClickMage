using UnityEngine;

public class CannonBall : Projectile
{
    [Header("Area Damage")]
    [SerializeField] private float _explosionRadius = 3f;
    [SerializeField] private float _radiusDamageMultiplier = 0.5f;
    [SerializeField] private LayerMask _damageableLayers;
    [SerializeField] private GameObject _explosionVFX;
    [SerializeField] private float _explosionLingerTime = 0.3f; // just long enough for the boom VFX to read

    protected override float StickDuration => _explosionLingerTime;

    protected override void OnHit(Vector3 hitPoint, Quaternion hitRotation, float damage)
    {
        if (_explosionVFX != null)
        {
            GameObject vfx = Instantiate(_explosionVFX, hitPoint, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        var hits = Physics.OverlapSphere(hitPoint, _explosionRadius, _damageableLayers);
        foreach (var hit in hits)
        {
            var targetable = hit.GetComponentInParent<Targetable>();
            if (targetable == null || !targetable.IsAlive) continue;

            float dist = Vector3.Distance(hitPoint, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / _explosionRadius);
            float finalDamage = damage * Mathf.Lerp(_radiusDamageMultiplier, 1f, falloff);

            targetable.TakeDamage(finalDamage, _attacker, Element);
            ApplyElementalStatus(targetable); // status effect applies to everyone in blast, not just the primary target
        }

        // NOTE: intentionally not calling transform.SetParent(...) here -
        // an explosion shouldn't stick to any single enemy, it stays at
        // the impact point in world space until it's released.
    }
}