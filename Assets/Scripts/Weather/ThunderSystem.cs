using UnityEngine;
using System.Collections;

/// <summary>
/// Handles lightning flashes and delayed thunder audio during the Rainy phase.
///
/// Setup in Inspector:
///   • Assign a DirectionalLight (or SpotLight)   → lightningLight
///   • Assign an AudioSource                       → thunderAudioSource
///   • Assign one or more thunder AudioClips       → thunderClips
///   • Assign a UI Image (full-screen white)       → screenFlashImage  [optional]
///   • Tweak timing ranges below
/// </summary>
public class ThunderSystem : MonoBehaviour
{
    // ── Lightning Light ───────────────────────────────────────────
    [Header("Lightning Light")]
    [Tooltip("A scene light that briefly flares to simulate a lightning bolt. " +
             "A Directional Light works well for global flashes.")]
    [SerializeField] private Light lightningLight;

    [Tooltip("Intensity of the light at the peak of a flash.")]
    [SerializeField] private float lightningPeakIntensity = 6f;

    [Tooltip("How fast the light fades back to zero after the flash peak (units per second).")]
    [SerializeField] private float lightFadeSpeed = 12f;

    // ── Screen Flash ──────────────────────────────────────────────
    [Header("Screen Flash (optional)")]
    [Tooltip("A full-screen UI Image set to white with alpha 0. " +
             "Used for a camera-wide flash effect. Leave empty to skip.")]
    [SerializeField] private UnityEngine.UI.Image screenFlashImage;

    [Tooltip("Alpha at peak of screen flash (0 = off, 1 = fully white).")]
    [SerializeField, Range(0f, 1f)] private float screenFlashMaxAlpha = 0.35f;

    [Tooltip("How fast the screen flash fades out (units per second).")]
    [SerializeField] private float screenFadeSpeed = 4f;

    // ── Audio ─────────────────────────────────────────────────────
    [Header("Thunder Audio")]
    [Tooltip("AudioSource used to play thunder one-shots. Does NOT need a clip pre-assigned.")]
    [SerializeField] private AudioSource thunderAudioSource;

    [Tooltip("Pool of thunder sound clips. A random one is chosen each strike.")]
    [SerializeField] private AudioClip[] thunderClips;

    [Tooltip("Volume range for thunder (adds variety between distant and close strikes).")]
    [SerializeField] private Vector2 thunderVolumeRange = new Vector2(0.5f, 1f);

    [Tooltip("Min seconds between the flash and the thunder sound (simulates distance).")]
    [SerializeField] private float minSoundDelay = 0.5f;

    [Tooltip("Max seconds between the flash and the thunder sound.")]
    [SerializeField] private float maxSoundDelay = 3.0f;

    // ── Strike Timing ─────────────────────────────────────────────
    [Header("Strike Timing")]
    [Tooltip("Minimum seconds between two lightning strikes.")]
    [SerializeField] private float minStrikeInterval = 5f;

    [Tooltip("Maximum seconds between two lightning strikes.")]
    [SerializeField] private float maxStrikeInterval = 20f;

    [Tooltip("Some strikes fire a rapid double-flash. This is the chance (0 = never, 1 = always).")]
    [SerializeField, Range(0f, 1f)] private float doubleFlashChance = 0.4f;

    [Tooltip("Gap between the two flashes in a double-strike (seconds).")]
    [SerializeField] private float doubleFlashGap = 0.08f;

    // ── Private state ─────────────────────────────────────────────
    private bool isActive = false;
    private Coroutine strikeCoroutine;

    private float currentLightIntensity = 0f;
    private float currentScreenAlpha = 0f;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Start with light off
        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
            lightningLight.enabled = false;
        }

        // Start with screen flash invisible
        if (screenFlashImage != null)
        {
            var c = screenFlashImage.color;
            c.a = 0f;
            screenFlashImage.color = c;
        }
    }

    private void Update()
    {
        FadeLightDown();
        FadeScreenFlashDown();
    }

    // ── Public API ────────────────────────────────────────────────

    public void SetActive(bool active)
    {
        isActive = active;

        if (active)
        {
            if (strikeCoroutine != null) StopCoroutine(strikeCoroutine);
            strikeCoroutine = StartCoroutine(StrikeLoop());
        }
        else
        {
            if (strikeCoroutine != null)
            {
                StopCoroutine(strikeCoroutine);
                strikeCoroutine = null;
            }

            // Let the current flash finish fading naturally via Update
        }
    }

    // ── Strike loop ───────────────────────────────────────────────

    /// Waits a random interval then triggers a lightning strike, forever.
    private IEnumerator StrikeLoop()
    {
        // Small initial delay so the first strike isn't immediate
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        while (true)
        {
            yield return DoStrike();

            float wait = Random.Range(minStrikeInterval, maxStrikeInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    /// Executes one full strike: flash(es) + delayed thunder.
    private IEnumerator DoStrike()
    {
        // ── Flash(es) ────────────────────────────────────────────
        TriggerFlash();

        bool isDouble = Random.value < doubleFlashChance;
        if (isDouble)
        {
            yield return new WaitForSeconds(doubleFlashGap);
            TriggerFlash();
        }

        // ── Delayed thunder ──────────────────────────────────────
        float soundDelay = Random.Range(minSoundDelay, maxSoundDelay);
        yield return new WaitForSeconds(soundDelay);

        PlayThunderSound();
    }

    // ── Flash helpers ─────────────────────────────────────────────

    /// Instantly sets the light and screen flash to their peak values.
    /// Update() fades them back down each frame.
    private void TriggerFlash()
    {
        // Light flash
        if (lightningLight != null)
        {
            lightningLight.enabled = true;
            currentLightIntensity = lightningPeakIntensity;
            lightningLight.intensity = currentLightIntensity;
        }

        // Screen flash
        if (screenFlashImage != null)
        {
            currentScreenAlpha = screenFlashMaxAlpha;
            var c = screenFlashImage.color;
            c.a = currentScreenAlpha;
            screenFlashImage.color = c;
        }
    }

    private void FadeLightDown()
    {
        if (lightningLight == null || !lightningLight.enabled) return;

        currentLightIntensity = Mathf.MoveTowards(
            currentLightIntensity, 0f, lightFadeSpeed * Time.deltaTime);

        lightningLight.intensity = currentLightIntensity;

        if (currentLightIntensity <= 0f)
            lightningLight.enabled = false;
    }

    private void FadeScreenFlashDown()
    {
        if (screenFlashImage == null) return;

        currentScreenAlpha = Mathf.MoveTowards(
            currentScreenAlpha, 0f, screenFadeSpeed * Time.deltaTime);

        var c = screenFlashImage.color;
        c.a = currentScreenAlpha;
        screenFlashImage.color = c;
    }

    // ── Audio ─────────────────────────────────────────────────────

    private void PlayThunderSound()
    {
        if (thunderAudioSource == null || thunderClips == null || thunderClips.Length == 0)
            return;

        AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
        float volume = Random.Range(thunderVolumeRange.x, thunderVolumeRange.y);

        thunderAudioSource.PlayOneShot(clip, volume);
    }

    private void OnDisable()
    {
        // Immediately kill the light so it doesn't stay on between scenes
        if (lightningLight != null)
        {
            lightningLight.intensity = 0f;
            lightningLight.enabled = false;
        }
    }
}