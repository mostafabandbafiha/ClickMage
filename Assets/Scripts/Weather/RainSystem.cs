using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all rain visuals and audio for the Rainy phase.
///
/// Setup in Inspector:
///   • Assign a ParticleSystem (rain droplets)     → rainParticleSystem
///   • Assign puddle prefabs array                 → puddlePrefabs
///   • Assign an AudioSource with rain loop clip   → rainAudioSource
///   • Tweak spawn bounds, rates, and audio range below
/// </summary>
public class RainSystem : MonoBehaviour
{
    // ── Rain Particles ────────────────────────────────────────────
    [Header("Rain Particles")]
    [Tooltip("The particle system that emits rain droplets.")]
    [SerializeField] private ParticleSystem rainParticleSystem;

    [Tooltip("Emission rate when rain is at full intensity.")]
    [SerializeField] private float maxEmissionRate = 500f;

    // ── Puddles ───────────────────────────────────────────────────
    [Header("Puddles")]
    [Tooltip("One or more puddle decal prefabs. A random one is picked per spawn.")]
    [SerializeField] private GameObject[] puddlePrefabs;

    [Tooltip("How many seconds between each puddle spawn at full intensity.")]
    [SerializeField] private float puddleSpawnInterval = 4f;

    [Tooltip("Half-extents of the box inside which puddles can appear (XZ plane, world space).")]
    [SerializeField] private Vector3 puddleSpawnHalfExtents = new Vector3(15f, 0f, 15f);

    [Tooltip("Y position for puddle placement (should sit on your ground level).")]
    [SerializeField] private float puddleGroundY = 0f;

    [Tooltip("Max puddles alive at once. Oldest is destroyed when limit is exceeded.")]
    [SerializeField] private int maxPuddles = 20;

    // ── Audio ─────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("AudioSource with a looping rain clip. Set it to Loop = true in the Inspector.")]
    [SerializeField] private AudioSource rainAudioSource;

    [Tooltip("Target volume when rain is at full intensity.")]
    [SerializeField, Range(0f, 1f)] private float maxRainVolume = 0.6f;

    [Tooltip("How fast volume fades in/out (units per second).")]
    [SerializeField] private float audioFadeSpeed = 1.5f;

    // ── Private state ─────────────────────────────────────────────
    private float currentIntensity = 0f;   // 0-1, driven by WeatherEffectController
    private bool isActive = false;

    private Coroutine puddleCoroutine;

    // Queue to track spawned puddles so we can enforce the cap
    private readonly System.Collections.Generic.Queue<GameObject> spawnedPuddles
        = new System.Collections.Generic.Queue<GameObject>();

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Make sure the particle system starts stopped
        if (rainParticleSystem != null)
        {
            var emission = rainParticleSystem.emission;
            emission.rateOverTime = 0f;
            rainParticleSystem.Stop();
        }

        // Audio starts silent
        if (rainAudioSource != null)
        {
            rainAudioSource.volume = 0f;
            rainAudioSource.Stop();
        }
    }

    private void Update()
    {
        if (!isActive) return;

        ApplyParticleIntensity();
        FadeAudio();
    }

    // ── Public API (called by WeatherEffectController) ────────────

    /// <summary>Activate or deactivate the rain system.</summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (active)
        {
            StartRain();
        }
        else
        {
            StopRain();
        }
    }

    /// <summary>
    /// Set rain intensity 0-1. Called every frame by WeatherEffectController
    /// during transitions so rain fades in/out smoothly.
    /// </summary>
    public void SetIntensity(float intensity)
    {
        currentIntensity = Mathf.Clamp01(intensity);
    }

    // ── Internal ──────────────────────────────────────────────────

    private void StartRain()
    {
        // Start particle system
        if (rainParticleSystem != null && !rainParticleSystem.isPlaying)
            rainParticleSystem.Play();

        // Start audio
        if (rainAudioSource != null && !rainAudioSource.isPlaying)
            rainAudioSource.Play();

        // Start puddle spawner
        if (puddlePrefabs != null && puddlePrefabs.Length > 0)
        {
            if (puddleCoroutine != null) StopCoroutine(puddleCoroutine);
            puddleCoroutine = StartCoroutine(PuddleSpawner());
        }
    }

    private void StopRain()
    {
        // Stop puddle spawner
        if (puddleCoroutine != null)
        {
            StopCoroutine(puddleCoroutine);
            puddleCoroutine = null;
        }

        // Let particles finish naturally rather than cutting off abruptly
        if (rainParticleSystem != null && rainParticleSystem.isPlaying)
            rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Audio will fade out in Update via FadeAudio()
    }

    /// Drives the particle emission rate based on currentIntensity.
    private void ApplyParticleIntensity()
    {
        if (rainParticleSystem == null) return;

        var emission = rainParticleSystem.emission;
        emission.rateOverTime = maxEmissionRate * currentIntensity;
    }

    /// Smoothly fades audio volume toward the target based on active state.
    private void FadeAudio()
    {
        if (rainAudioSource == null) return;

        float targetVolume = isActive ? maxRainVolume * currentIntensity : 0f;
        rainAudioSource.volume = Mathf.MoveTowards(
            rainAudioSource.volume,
            targetVolume,
            audioFadeSpeed * Time.deltaTime
        );

        // Stop the AudioSource once fully silent to save resources
        if (!isActive && rainAudioSource.volume <= 0f && rainAudioSource.isPlaying)
            rainAudioSource.Stop();
    }

    /// Coroutine: periodically spawns a random puddle prefab on the ground.
    private IEnumerator PuddleSpawner()
    {
        while (true)
        {
            // Scale wait time inversely with intensity (more rain = faster puddles)
            float intensity = Mathf.Max(0.1f, currentIntensity);
            yield return new WaitForSeconds(puddleSpawnInterval / intensity);

            SpawnPuddle();
        }
    }

    private void SpawnPuddle()
    {
        if (puddlePrefabs == null || puddlePrefabs.Length == 0) return;

        // Random position inside the spawn box on the ground plane
        Vector3 spawnPos = new Vector3(
            transform.position.x + Random.Range(-puddleSpawnHalfExtents.x, puddleSpawnHalfExtents.x),
            puddleGroundY,
            transform.position.z + Random.Range(-puddleSpawnHalfExtents.z, puddleSpawnHalfExtents.z)
        );

        // Pick a random puddle prefab
        GameObject prefab = puddlePrefabs[Random.Range(0, puddlePrefabs.Length)];
        GameObject puddle = Instantiate(prefab, spawnPos, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));

        spawnedPuddles.Enqueue(puddle);

        // Enforce puddle cap — destroy oldest if over limit
        while (spawnedPuddles.Count > maxPuddles)
        {
            GameObject oldest = spawnedPuddles.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
    }

    /// Clean up all spawned puddles (e.g. on scene exit or forced reset).
    public void ClearAllPuddles()
    {
        while (spawnedPuddles.Count > 0)
        {
            GameObject p = spawnedPuddles.Dequeue();
            if (p != null) Destroy(p);
        }
    }

    private void OnDisable()
    {
        ClearAllPuddles();
    }

#if UNITY_EDITOR
    // Visualise the puddle spawn area in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.25f);
        Vector3 center = new Vector3(transform.position.x, puddleGroundY, transform.position.z);
        Gizmos.DrawCube(center, new Vector3(
            puddleSpawnHalfExtents.x * 2f,
            0.05f,
            puddleSpawnHalfExtents.z * 2f
        ));
    }
#endif
}