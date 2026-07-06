using ClickMage.Entities;
using ClickMage.StateMachine;
using UnityEngine;
using System.Collections.Generic;

public class HouseStructure : BaseEntity
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("House Settings")]
    [SerializeField] private BaseCharacter _residentPrefab;
    [SerializeField] private int _residentCapacity = 2;
    [SerializeField] private Transform _enterPoint;
    [SerializeField] private Transform _exitPoint;

    // ── State machine ─────────────────────────────────────────────────────
    private StateMachine<HouseStructure> _stateMachine;
    public StateMachine<HouseStructure> StateMachine => _stateMachine;

    // ── Residents ─────────────────────────────────────────────────────────
    private List<BaseCharacter> _residents = new();
    private List<BaseCharacter> _insideResidents = new();

    // ── Public API ────────────────────────────────────────────────────────
    public bool HasSpace => _residents.Count < _residentCapacity;
    public bool AllResidentsHome => _insideResidents.Count == _residents.Count
                                    && _residents.Count > 0;
    public bool IsEmpty => _insideResidents.Count == 0;
    public IReadOnlyList<BaseCharacter> Residents => _residents;

    public Vector3 EnterPosition => _enterPoint != null
        ? _enterPoint.position : transform.position;
    public Vector3 ExitPosition => _exitPoint != null
        ? _exitPoint.position : transform.position;

    public void ChangeState(IState<HouseStructure> newState) =>
        _stateMachine.ChangeState(newState);

    // ── Events ────────────────────────────────────────────────────────────
    public event System.Action<HouseStructure> OnAllResidentsHome;
    public event System.Action<HouseStructure> OnAllResidentsLeft;

    // ── Unity lifecycle ───────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();
        _stateMachine = new StateMachine<HouseStructure>(this);
    }

    private void Start()
    {
        _stateMachine.ChangeState(new HouseEmptyState());

        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;
    }

    private void Update() => _stateMachine?.Tick(Time.deltaTime);

    private void OnDestroy()
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;

        // Clean up spawned characters when house is destroyed
        foreach (var resident in _residents)
            if (resident != null) Destroy(resident.gameObject);
    }

    // ── Called by build system when house is placed ───────────────────────
    public void OnPlaced()
    {
        SpawnResidents();
        _stateMachine.ChangeState(new HouseOccupiedDayState());
    }

    // ── Spawning ──────────────────────────────────────────────────────────
    private void SpawnResidents()
    {
        if (_residentPrefab == null)
        {
            Debug.LogWarning($"[HouseStructure] {name} has no resident prefab assigned.");
            return;
        }

        for (int i = 0; i < _residentCapacity; i++)
        {
            var resident = Instantiate(_residentPrefab, ExitPosition, Quaternion.identity);
            resident.name = $"{_residentPrefab.name}_{name}_{i}";
            _residents.Add(resident);
            Debug.Log($"[HouseStructure] Spawned {resident.name}.");
        }
    }

    // ── Day/Night ─────────────────────────────────────────────────────────
    private void OnTimeOfDayChanged(TimeOfDay timeOfDay)
    {
        if (timeOfDay == TimeOfDay.Night)
            _stateMachine.ChangeState(new HouseCallingResidentsState());
        else if (timeOfDay == TimeOfDay.Day)
            _stateMachine.ChangeState(new HouseReleasingResidentsState());
    }

    // ── Resident tracking ─────────────────────────────────────────────────
    public void CharacterEntered(BaseCharacter character)
    {
        if (!_insideResidents.Contains(character))
            _insideResidents.Add(character);

        character.gameObject.SetActive(false);

        if (AllResidentsHome)
            OnAllResidentsHome?.Invoke(this);
    }

    public void CharacterExited(BaseCharacter character)
    {
        _insideResidents.Remove(character);
        character.gameObject.SetActive(true);
        character.transform.position = ExitPosition;

        if (IsEmpty)
            OnAllResidentsLeft?.Invoke(this);
    }

    public bool IsInside(BaseCharacter character) => _insideResidents.Contains(character);

    // ── Commands to residents ─────────────────────────────────────────────
    public void SendResidentsHome()
    {
        foreach (var resident in _residents)
        {
            if (IsInside(resident)) continue;
            resident.GoHome(this);
        }
    }

    public void ReleaseResidents()
    {
        foreach (var resident in _insideResidents.ToArray())
            resident.LeaveHome(this);
    }
}