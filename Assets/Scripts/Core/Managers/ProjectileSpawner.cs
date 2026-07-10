using ClickMage.Entities;
using ClickMage.Stats;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public static ProjectileSpawner Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // registry is passed in by the caller - the spawner doesn't own one globally
    public Projectile Spawn(BaseEntity attacker, ProjectileRegistry registry, Targetable target,
                             float damage, Vector3 position, Quaternion rotation)
    {
        DamageType elements = CommonStats.ResolveElements(attacker);
        GameObject prefab = registry.GetPrefab(elements);
        if (prefab == null) return null;

        GameObject obj = PoolManager.Instance.Get(prefab, position, rotation);
        Projectile projectile = obj.GetComponent<Projectile>();
        projectile.Initialize(target, damage, attacker);
        return projectile;
    }
}