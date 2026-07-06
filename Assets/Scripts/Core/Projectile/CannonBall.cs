using UnityEngine;

public class CannonBall : Projectile
{
    [Header("Area Damage")]
    [SerializeField] private float _explosionRadius = 3f;
    [SerializeField] private float _radiusDamageMultiplier = 0.5f;
    [SerializeField] private LayerMask _damageableLayers;
    [SerializeField] private GameObject _explosionVFX;

    protected override void OnHit(Vector3 position, float damage)
    {
        // Spawn VFX
        if (_explosionVFX != null)
            Instantiate(_explosionVFX, position, Quaternion.identity);

        // Find everything in radius
        var hits = Physics.OverlapSphere(position, _explosionRadius, _damageableLayers);
        foreach (var hit in hits)
        {
            var targetable = hit.GetComponent<Targetable>();
            if (targetable == null || !targetable.IsAlive) continue;

            // Falloff — full damage at center, reduced at edge
            float dist = Vector3.Distance(position, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / _explosionRadius);
            float finalDamage = damage * Mathf.Lerp(_radiusDamageMultiplier, 1f, falloff);

            targetable.TakeDamage(finalDamage, _attacker);
        }
    }
}