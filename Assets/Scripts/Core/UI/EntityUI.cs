using ClickMage.Entities;
using ClickMage.Stats;
using RTLTMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EntityUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private GameObject _healthBarRoot;
    [SerializeField] private RTLTextMeshPro _heathText;

    [Header("Settings")]
    [SerializeField] private float _hideWhenFullDelay = 3f;
    [SerializeField] private float _uiUpdateInterval = 0.2f; // passive refresh rate (regen etc.)

    [Header("Floating Damage")]
    [SerializeField] private FloatingDamageText _floatingTextPrefab;
    [SerializeField] private Transform _floatingTextSpawnPoint;

    [Header("Faction Colors")]
    [SerializeField] private Color _playerColor = new Color(0.2f, 0.85f, 0.3f);
    [SerializeField] private Color _enemyColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color _neutralColor = new Color(0.9f, 0.8f, 0.2f);

    [Header("Hit Feedback")]
    [SerializeField] private float _bounceScale = 1.15f;
    [SerializeField] private float _bounceDuration = 0.15f;
    [SerializeField] private AnimationCurve _bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _flashDuration = 0.15f;




    private Targetable _targetable;
    private SelectableComponent _selectable;
    private float _showDistance = 50f;
    protected BaseEntity _entity;
    private Camera _cam;
    private float _hideTimer;
    private bool _isFull = true;
    private bool _isSelected;

    private Color _baseFillColor;
    private Vector3 _healthBarBaseScale;
    private Coroutine _hitFeedbackRoutine;

    private float _uiUpdateTimer;
    private int _lastDisplayedHealth = int.MinValue; // avoids re-writing the string every tick if value hasn't changed


    protected virtual void Awake()
    {
        _cam = Camera.main;
    }

    protected virtual void Start()
    {
        _entity = GetComponentInParent<BaseEntity>();
        _targetable = GetComponentInParent<Targetable>();
        _selectable = GetComponentInParent<SelectableComponent>();

        if (_targetable != null)
            _targetable.OnDamageTaken += HandleDamageTaken;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelected += HandleSelected;
            SelectionManager.Instance.OnDeselected += HandleDeselected;
            _isSelected = _selectable != null && SelectionManager.Instance.CurrentSelected == _selectable;
        }

        _hideTimer = _hideWhenFullDelay;
        _healthBarBaseScale = _healthBarRoot.transform.localScale;

        ApplyFactionColor();
        _heathText.text = _entity.GetStatValue(CommonStats.Health).ToString();
    }

    private void OnDestroy()
    {
        if (_targetable != null)
            _targetable.OnDamageTaken -= HandleDamageTaken;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.OnSelected -= HandleSelected;
            SelectionManager.Instance.OnDeselected -= HandleDeselected;
        }
    }

    // ── Selection ────────────────────────────────────────────────────────
    private void HandleSelected(SelectableComponent selected)
    {
        if (selected == _selectable)
            _isSelected = true;
    }

    private void HandleDeselected(SelectableComponent deselected)
    {
        if (deselected == _selectable)
            _isSelected = false;
    }

    // ── Faction color ────────────────────────────────────────────────────
    private void ApplyFactionColor()
    {
        if (_targetable == null) return;

        _baseFillColor = _targetable.Faction switch
        {
            Faction.Player => _playerColor,
            Faction.Enemy => _enemyColor,
            _ => _neutralColor
        };

        _healthFill.color = _baseFillColor;
    }

    // ── Damage feedback ──────────────────────────────────────────────────
    private void HandleDamageTaken(float amount, BaseEntity attacker, DamageType type)
    {
        PlayHitFeedback();
        UpdateHealthBar(); // force an immediate refresh so the number/fill isn't stale until next poll

        if (_floatingTextPrefab == null) return;
        Color color = CommonStats.GetStatColor(type);
        Vector3 spawnPos = (_floatingTextSpawnPoint != null ? _floatingTextSpawnPoint : transform).position;
        var instance = Instantiate(_floatingTextPrefab, spawnPos, Quaternion.identity, _floatingTextSpawnPoint);
        instance.Init(amount, color, _cam);
    }

    private void PlayHitFeedback()
    {
        if (_hitFeedbackRoutine != null)
            StopCoroutine(_hitFeedbackRoutine);

        _hitFeedbackRoutine = StartCoroutine(HitFeedbackRoutine());
    }

    private IEnumerator HitFeedbackRoutine()
    {
        float duration = Mathf.Max(_bounceDuration, _flashDuration);
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            float p = _bounceCurve.Evaluate(t / half);
            _healthBarRoot.transform.localScale = _healthBarBaseScale * Mathf.Lerp(1f, _bounceScale, p);
            _healthFill.color = Color.Lerp(_baseFillColor, _flashColor, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = _bounceCurve.Evaluate(t / half);
            _healthBarRoot.transform.localScale = _healthBarBaseScale * Mathf.Lerp(_bounceScale, 1f, p);
            _healthFill.color = Color.Lerp(_flashColor, _baseFillColor, p);
            yield return null;
        }

        _healthBarRoot.transform.localScale = _healthBarBaseScale;
        _healthFill.color = _baseFillColor;
        _hitFeedbackRoutine = null;
    }

    protected virtual void Update()
    {
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        _uiUpdateTimer -= Time.deltaTime;
        if (_uiUpdateTimer <= 0f)
        {
            _uiUpdateTimer = _uiUpdateInterval;
            UpdateHealthBar();
        }

        UpdateVisibility();
    }



    private void UpdateHealthBar()
    {
        if (_entity == null || !_entity.HasStat(CommonStats.Health)) return;
        float max = _entity.GetStatValue(CommonStats.MaxHealth);
        if (max <= 0f) return;

        float current = _entity.GetStatValue(CommonStats.Health);
        float t = current / max;
        float prevFill = _healthFill.fillAmount;
        _healthFill.fillAmount = t;

        int displayHealth = Mathf.CeilToInt(Mathf.Max(0f, current));
        if (displayHealth != _lastDisplayedHealth)
        {
            _heathText.text = displayHealth.ToString();
            _lastDisplayedHealth = displayHealth;
        }

        _isFull = t >= 1f;
        if (!_isFull || Mathf.Abs(prevFill - t) > 0.001f)
            _hideTimer = _hideWhenFullDelay;
    }

    private void UpdateVisibility()
    {
        Transform reference = Player.Point ?? _cam.transform;
        float distSq = (reference.position - transform.position).sqrMagnitude;
        bool inRange = distSq <= _showDistance * _showDistance;

        if (_isFull && _hideTimer > 0f)
            _hideTimer -= Time.deltaTime;

        // Selected always forces the bar visible, regardless of range/full-hide timer.
        bool show = _isSelected || (inRange && (!_isFull || _hideTimer > 0f));
        _healthBarRoot.SetActive(show);
    }
}