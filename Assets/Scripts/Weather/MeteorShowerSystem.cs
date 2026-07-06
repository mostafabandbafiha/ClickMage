using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

/// <summary>
/// Spawns meteor projectiles at random intervals during the Night phase.
/// Uses raycast + NavMesh sampling + overlap check to find empty, walkable landing spots.
///
/// Setup in Inspector:
///   • Assign the MeteorProjectile prefab          → meteorPrefab
///   • Assign your crystal prefab                  → crystalPrefab
///   • Assign an impact VFX prefab (optional)      → impactVFXPrefab
///   • Set buildingLayerMask to your buildings layer
///   • Set groundLayerMask to your terrain/ground layer
/// </summary>
public class MeteorShowerSystem : MonoBehaviour
{
    // ── Prefabs ───────────────────────────────────────────────────
    [Header("Prefabs")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private GameObject crystalPrefab;
    [SerializeField] private GameObject impactVFXPrefab;
    [SerializeField] private float impactVFXLifetime = 3f;

    // ── Spawn Timing ──────────────────────────────────────────────
    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnInterval = 4f;
    [SerializeField] private float maxSpawnInterval = 12f;
    [SerializeField, Range(0f, 1f)] private float burstChance = 0.3f;
    [SerializeField] private float burstDelay = 0.6f;

    // ── Spawn Area ────────────────────────────────────────────────
    [Header("Spawn Area")]
    [SerializeField] private float spawnHeight = 30f;
    [SerializeField] private Vector3 targetAreaHalfExtents = new Vector3(20f, 0f, 20f);
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float spawnLateralOffset = 15f;

    // ── Landing Validation ────────────────────────────────────────
    [Header("Landing Validation")]
    [Tooltip("Layer(s) that count as ground/terrain. Used for the downward raycast.")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("How far from the candidate point NavMesh sampling searches for a valid walkable spot.")]
    [SerializeField] private float navMeshSampleRadius = 5f;

    [Tooltip("Layer(s) that count as buildings/obstacles. Meteors won't land within clearance radius of these.")]
    [SerializeField] private LayerMask buildingLayerMask;

    [Tooltip("Meteors won't land within this radius of any building.")]
    [SerializeField] private float buildingClearanceRadius = 3f;

    [Tooltip("How many times to retry finding a valid spot before skipping this meteor spawn.")]
    [SerializeField] private int maxLandingAttempts = 10;

    [Tooltip("Raycast starts this far above groundY so it clears terrain hills.")]
    [SerializeField] private float raycastStartHeight = 50f;

    // ── Crystal ───────────────────────────────────────────────────
    [Header("Crystal Spawn")]
    [SerializeField] private float crystalYOffset = 0.1f;
    [SerializeField] private int maxCrystals = 15;

    // ── Camera Shake ──────────────────────────────────────────────
    [Header("Camera Shake (optional)")]
    [SerializeField] private Camera shakeCamera;
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private float shakeDuration = 0.3f;

    // ── Events ────────────────────────────────────────────────────
    /// Fired when a meteor hits the ground. Passes the world-space impact position.
    public event Action<Vector3> OnMeteorImpact;

    // ── Private state ─────────────────────────────────────────────
    private bool isActive = false;
    private Coroutine spawnCoroutine;

    private readonly System.Collections.Generic.Queue<GameObject> spawnedCrystals
        = new System.Collections.Generic.Queue<GameObject>();

    // ─────────────────────────────────────────────────────────────

    public void SetActive(bool active)
    {
        isActive = active;

        if (active)
        {
            if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
            spawnCoroutine = StartCoroutine(SpawnLoop());
        }
        else
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    // ── Spawn loop ────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));

        while (true)
        {
            SpawnMeteor();

            if (UnityEngine.Random.value < burstChance)
            {
                yield return new WaitForSeconds(burstDelay);
                SpawnMeteor();
            }

            yield return new WaitForSeconds(
                UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    // ── Smart spawn ───────────────────────────────────────────────

    private void SpawnMeteor()
    {
        if (meteorPrefab == null)
        {
            Debug.LogWarning("[MeteorShowerSystem] meteorPrefab is not assigned.");
            return;
        }

        if (!TryGetValidLandingPosition(out Vector3 targetPos))
        {
            Debug.Log("[MeteorShowerSystem] Could not find a valid landing spot — skipping meteor.");
            return;
        }

        // Spawn point: above and to one side of the target for a diagonal entry angle
        Vector2 lateralDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 spawnPos = new Vector3(
            targetPos.x + lateralDir.x * spawnLateralOffset,
            targetPos.y + spawnHeight,
            targetPos.z + lateralDir.y * spawnLateralOffset
        );

        GameObject meteorGO = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
        meteorGO.transform.rotation = Quaternion.LookRotation((targetPos - spawnPos).normalized);

        MeteorProjectile projectile = meteorGO.GetComponent<MeteorProjectile>();
        if (projectile != null)
            projectile.Initialise(targetPos, OnMeteorLanded);
        else
            Debug.LogWarning("[MeteorShowerSystem] meteorPrefab is missing a MeteorProjectile component.");
    }

    /// <summary>
    /// Tries up to maxLandingAttempts times to find a landing point that passes
    /// three checks in order:
    ///
    ///   1. RAYCAST  — fires straight down to find the real ground surface Y.
    ///                 Skips candidates where the ray hits nothing (edge of map, void).
    ///
    ///   2. NAVMESH  — snaps the raycast hit to the nearest valid NavMesh position.
    ///                 Ensures the meteor lands on a walkable, open area (not inside
    ///                 a building's nav-obstacle cutout).
    ///
    ///   3. OVERLAP  — checks a sphere at the NavMesh point against buildingLayerMask.
    ///                 Rejects if any building collider is within buildingClearanceRadius.
    /// </summary>
    private bool TryGetValidLandingPosition(out Vector3 result)
    {
        for (int attempt = 0; attempt < maxLandingAttempts; attempt++)
        {
            // ── Step 1: random candidate in the target XZ area ───────
            Vector3 candidate = new Vector3(
                transform.position.x + UnityEngine.Random.Range(-targetAreaHalfExtents.x, targetAreaHalfExtents.x),
                groundY,
                transform.position.z + UnityEngine.Random.Range(-targetAreaHalfExtents.z, targetAreaHalfExtents.z)
            );

            // ── Step 2: raycast down to find actual surface height ────
            Vector3 rayOrigin = candidate + Vector3.up * raycastStartHeight;

            if (groundLayerMask != 0)
            {
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    raycastStartHeight + 10f, groundLayerMask))
                {
                    // Use the exact surface point the ray hit
                    candidate = hit.point;
                }
                else
                {
                    // Ray found nothing (off edge of map / over a void) — skip
                    continue;
                }
            }
            // If groundLayerMask is not set, candidate stays at flat groundY

            // ── Step 3: NavMesh sample — find nearest walkable point ──
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit,
                navMeshSampleRadius, NavMesh.AllAreas))
            {
                // Completely off the nav mesh — skip
                continue;
            }

            Vector3 navPoint = navHit.position;

            // ── Step 4: overlap sphere — reject if too close to buildings ──
            if (buildingLayerMask != 0)
            {
                Collider[] nearby = Physics.OverlapSphere(
                    navPoint, buildingClearanceRadius, buildingLayerMask);

                if (nearby.Length > 0)
                    continue; // Too close to a building — try again
            }

            // All checks passed
            result = navPoint;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // ── Impact callback (called by MeteorProjectile) ──────────────

    private void OnMeteorLanded(Vector3 impactPos)
    {
        SpawnCrystal(impactPos);
        SpawnImpactVFX(impactPos);
        OnMeteorImpact?.Invoke(impactPos);

        if (shakeCamera != null)
            StartCoroutine(ShakeCamera());

        Debug.Log($"[MeteorShowerSystem] Meteor landed at {impactPos}");
    }

    // ── Crystal ───────────────────────────────────────────────────

    private void SpawnCrystal(Vector3 impactPos)
    {
        if (crystalPrefab == null) return;

        Vector3 crystalPos = new Vector3(impactPos.x, impactPos.y + crystalYOffset, impactPos.z);
        Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        spawnedCrystals.Enqueue(Instantiate(crystalPrefab, crystalPos, rot));

        while (spawnedCrystals.Count > maxCrystals)
        {
            GameObject oldest = spawnedCrystals.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
    }

    // ── VFX ───────────────────────────────────────────────────────

    private void SpawnImpactVFX(Vector3 impactPos)
    {
        if (impactVFXPrefab == null) return;
        Destroy(Instantiate(impactVFXPrefab, impactPos, Quaternion.identity), impactVFXLifetime);
    }

    // ── Camera shake ──────────────────────────────────────────────

    private IEnumerator ShakeCamera()
    {
        Vector3 originalPos = shakeCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float diminish = 1f - (elapsed / shakeDuration);
            shakeCamera.transform.localPosition =
                originalPos + UnityEngine.Random.insideUnitSphere * shakeMagnitude * diminish;

            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeCamera.transform.localPosition = originalPos;
    }

    // ── Cleanup ───────────────────────────────────────────────────

    public void ClearAllCrystals()
    {
        while (spawnedCrystals.Count > 0)
        {
            GameObject c = spawnedCrystals.Dequeue();
            if (c != null) Destroy(c);
        }
    }

    private void OnDisable() => ClearAllCrystals();

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
        Vector3 center = new Vector3(transform.position.x, groundY, transform.position.z);
        Gizmos.DrawCube(center, new Vector3(
            targetAreaHalfExtents.x * 2f, 0.05f, targetAreaHalfExtents.z * 2f));

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
        Gizmos.DrawLine(center, center + Vector3.up * spawnHeight);
    }
#endif
}