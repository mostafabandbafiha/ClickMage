using ClickMage.Entities;
using ClickMage.Stats;
using UnityEngine;

public class EntitySelectionVisualsController : MonoBehaviour
{
    public static EntitySelectionVisualsController Instance { get; private set; }

    [Header("Select Ring - shown immediately on selection")]
    [SerializeField] private RangeIndicator _selectIndicatorPrefab;
    [SerializeField] private float _selectRingRadius = 1.2f; // fixed, doesn't depend on stats

    [Header("Range Ring - shown while Alt is held")]
    [SerializeField] private RangeIndicator _rangeIndicatorPrefab;

    private static readonly string[] RangeStatPriority =
    {
        CommonStats.AttackRange,
        CommonStats.Range,
        CommonStats.AreaRadius
    };

    private RangeIndicator _selectIndicator;
    private RangeIndicator _rangeIndicator;

    private BaseEntity _selectedEntity;
    private bool _entityHasRange;
    private bool _rangeVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (SelectionManager.Instance == null) return;
        SelectionManager.Instance.OnSelected += HandleSelected;
        SelectionManager.Instance.OnDeselected += HandleDeselected;
    }

    private void OnDestroy()
    {
        if (SelectionManager.Instance == null) return;
        SelectionManager.Instance.OnSelected -= HandleSelected;
        SelectionManager.Instance.OnDeselected -= HandleDeselected;
    }

    private void HandleSelected(SelectableComponent selected)
    {
        var entity = selected.GetComponent<BaseEntity>()
                     ?? selected.GetComponentInParent<BaseEntity>();

        _selectedEntity = entity;
        _entityHasRange = entity != null && GetRangeStat(entity) > 0f;

        // Select ring always shows on selection, regardless of range stat
        if (_selectIndicator == null)
            _selectIndicator = Instantiate(_selectIndicatorPrefab);

        Transform ringTarget = entity != null ? entity.transform : selected.transform;
        _selectIndicator.Show(ringTarget, _selectRingRadius);

        // Range ring only appears if Alt is already held at the moment of selection;
        // otherwise Update() will pick it up the next time Alt is pressed
        UpdateRangeVisibility();
    }

    private void HandleDeselected(SelectableComponent deselected)
    {
        _selectedEntity = null;
        _entityHasRange = false;
        _selectIndicator?.Hide();
        _rangeIndicator?.Hide();
        _rangeVisible = false;
    }

    private void Update()
    {
        if (_selectedEntity == null) return;

        UpdateRangeVisibility();

        // keep radius live if the stat is buffed/debuffed while held
        if (_rangeVisible)
            _rangeIndicator.SetRadius(GetRangeStat(_selectedEntity));
    }

    private void UpdateRangeVisibility()
    {
        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shouldShow = altHeld && _entityHasRange && _selectedEntity != null;

        if (shouldShow == _rangeVisible) return;
        _rangeVisible = shouldShow;

        if (_rangeVisible)
        {
            if (_rangeIndicator == null)
                _rangeIndicator = Instantiate(_rangeIndicatorPrefab);

            _rangeIndicator.Show(_selectedEntity.transform, GetRangeStat(_selectedEntity));
        }
        else
        {
            _rangeIndicator?.Hide();
        }
    }

    private float GetRangeStat(BaseEntity entity)
    {
        foreach (var key in RangeStatPriority)
        {
            if (!entity.HasStat(key)) continue;
            float value = entity.GetStatValue(key);
            if (value > 0f) return value;
        }
        return 0f;
    }
}