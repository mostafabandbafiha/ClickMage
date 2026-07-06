using UnityEngine;
using System;

/// <summary>
/// Self-contained script on the meteor prefab.
/// Moves itself from its spawn point to the target ground position,
/// then fires the impact callback and destroys itself.
///
/// Setup in Inspector (on the meteor prefab):
///   • Assign a trail / particle system             → meteorTrail  [optional]
///   • Tweak speed and rotation spin below
///
/// This script is initialised at runtime by MeteorShowerSystem.
/// You do NOT need to call anything on it manually.
/// </summary>
public class MeteorProjectile : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("World units per second the meteor travels toward its target.")]
    [SerializeField] private float speed = 25f;

    [Tooltip("How close to the target (in units) before impact is triggered.")]
    [SerializeField] private float arrivalThreshold = 0.4f;

    // ── Visual ────────────────────────────────────────────────────
    [Header("Visual")]
    [Tooltip("Optional particle trail on the meteor prefab. Detached on impact so it fades out naturally.")]
    [SerializeField] private ParticleSystem meteorTrail;

    [Tooltip("Spin speed around the meteor's forward axis (degrees per second). 0 = no spin.")]
    [SerializeField] private float rollSpeed = 120f;

    // ── Private state ─────────────────────────────────────────────
    private Vector3 targetPosition;
    private Action<Vector3> onImpactCallback;
    private bool hasLanded = false;

    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by MeteorShowerSystem immediately after instantiation.
    /// </summary>
    /// <param name="target">World-space ground position to fly toward.</param>
    /// <param name="onImpact">Callback fired when the meteor reaches the target.</param>
    public void Initialise(Vector3 target, Action<Vector3> onImpact)
    {
        targetPosition = target;
        onImpactCallback = onImpact;
        hasLanded = false;
    }

    private void Update()
    {
        if (hasLanded) return;

        MoveTowardTarget();
        ApplyRoll();
        CheckArrival();
    }

    // ── Movement ──────────────────────────────────────────────────

    private void MoveTowardTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    /// Spins the meteor around its own forward axis for a tumbling look.
    private void ApplyRoll()
    {
        if (Mathf.Approximately(rollSpeed, 0f)) return;
        transform.Rotate(Vector3.forward, rollSpeed * Time.deltaTime, Space.Self);
    }

    private void CheckArrival()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget <= arrivalThreshold)
        {
            Land();
        }
    }

    // ── Impact ────────────────────────────────────────────────────

    private void Land()
    {
        if (hasLanded) return;  // Guard against double-firing
        hasLanded = true;

        // Detach the trail so it fades out on its own instead of vanishing instantly
        if (meteorTrail != null)
        {
            meteorTrail.transform.SetParent(null);
            meteorTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // Destroy the detached trail after its longest particle lifetime
            float trailLifetime = meteorTrail.main.startLifetime.constantMax;
            Destroy(meteorTrail.gameObject, trailLifetime + 0.5f);
        }

        // Fire the impact callback — MeteorShowerSystem handles crystal + VFX
        onImpactCallback?.Invoke(targetPosition);

        // Destroy the meteor body
        Destroy(gameObject);
    }

    // ── Safety net ────────────────────────────────────────────────
    // If something goes wrong and the meteor never lands (e.g. target below terrain),
    // destroy it after a timeout so we never leak objects.
    private void OnEnable()
    {
        Invoke(nameof(ForceLand), 15f);
    }

    private void ForceLand()
    {
        if (!hasLanded)
        {
            Debug.LogWarning("[MeteorProjectile] Force-landed after timeout (target may be unreachable).");
            Land();
        }
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw a line from the meteor to its target while in flight
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, arrivalThreshold);
    }
#endif
}