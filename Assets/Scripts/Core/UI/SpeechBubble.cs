using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space speech bubble that appears above a character.
/// Attach the prefab as a child of the character, positioned above the head.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SpeechBubble : MonoBehaviour
{
    [Header("Bubble Settings")]
    [SerializeField] private Image _bubbleImage;           // assign bubble sprite
    [SerializeField] private float _fadeSpeed = 3f;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Coroutine _hideCoroutine;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
        //gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Always face the camera
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;
    }

    /// <summary>Show the bubble for a set duration, then fade out.</summary>
    public void Show(float duration = 3f)
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        gameObject.SetActive(true);
        _hideCoroutine = StartCoroutine(ShowRoutine(duration));
    }

    /// <summary>Immediately hide the bubble.</summary>
    public void Hide()
    {
        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        gameObject.SetActive(false);
        _canvasGroup.alpha = 0f;
    }

    private IEnumerator ShowRoutine(float duration)
    {
        // Fade in
        while (_canvasGroup.alpha < 1f)
        {
            _canvasGroup.alpha += Time.deltaTime * _fadeSpeed;
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(duration);

        // Fade out
        while (_canvasGroup.alpha > 0f)
        {
            _canvasGroup.alpha -= Time.deltaTime * _fadeSpeed;
            yield return null;
        }

        //gameObject.SetActive(false);
    }
}