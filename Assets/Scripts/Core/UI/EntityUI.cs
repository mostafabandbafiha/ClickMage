using ClickMage.Entities;
using ClickMage.Stats;
using RTLTMPro;
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

    [Header("Floating Damage")]
    [SerializeField] private FloatingDamageText _floatingTextPrefab;
    [SerializeField] private Transform _floatingTextSpawnPoint; // usually same as health bar root, or slightly above

    private Targetable _targetable;

    private float _showDistance = 50f;
    private Transform _player;
    protected BaseEntity _entity;
    private Camera _cam;
    private float _hideTimer;
    private bool _isFull = true;

    protected virtual void Awake()
    {
        // Entity is always the parent
        _cam = Camera.main;
        
    }

    protected virtual void Start()
    {
        _entity = GetComponentInParent<BaseEntity>();
        _targetable = GetComponentInParent<Targetable>();

        if (_targetable != null)
            _targetable.OnDamageTaken += HandleDamageTaken;

        _hideTimer = _hideWhenFullDelay;
        _heathText.text = _entity.GetStatValue(CommonStats.Health).ToString();
    }

    private void OnDestroy()
    {
        if (_targetable != null)
            _targetable.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(float amount, BaseEntity attacker, DamageType type)
    {
        if (_floatingTextPrefab == null) return;

        Color color = CommonStats.GetStatColor(type);
        Vector3 spawnPos = (_floatingTextSpawnPoint != null ? _floatingTextSpawnPoint : transform).position;
        var instance = Instantiate(_floatingTextPrefab, spawnPos, Quaternion.identity, _floatingTextSpawnPoint);
        instance.Init(amount, color, _cam);
    }

    protected virtual void Update()
    {   

        // Always face camera
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        UpdateHealthBar();
        UpdateVisibility();
    }

    private void UpdateHealthBar()
    {
        if (_entity == null || !_entity.HasStat(CommonStats.Health)) return;

        float max = _entity.GetStatValue(CommonStats.MaxHealth);
        if (max <= 0f) return;

        float t = _entity.GetStatValue(CommonStats.Health) / max;
        float prevFill = _healthFill.fillAmount;

        _healthFill.fillAmount = t;
        _heathText.text = _entity.GetStatValue(CommonStats.Health).ToString();

        //_healthFill.color = Color.Lerp(Color.red, Color.green, t);

        _isFull = t >= 1f;

        // Reset timer any time the fill actually changed, or when not full
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

        bool show = inRange && (!_isFull || _hideTimer > 0f);
        _healthBarRoot.SetActive(show);
    }
}