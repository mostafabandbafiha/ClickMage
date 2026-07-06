using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop-in replacement / extension of the original EnemyCommander.
///
/// Responsibilities added on top of the original:
///   - Dusk: run ThreatScanner → CounterPickSelector → build EnemyGroup list
///   - Night: spawn groups on staggered timers, track per-enemy survival column
///   - Dawn: feed survival data back to ThreatScanner; update DifficultyBudget
///   - Boss: hardcoded group on night 30, prestige modifier after night 30
///
/// Original EnemyCommander contract preserved:
///   - _activeEnemies list
///   - OnEnemyDied callback
///   - EndCurrentWave kills survivors at dawn
/// </summary>
public class SmartEnemyCommander : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Dependencies")]
    [SerializeField] private GridData _grid;           // assign or inject from BuildModeController
    [SerializeField] private SpawnArea[] _spawnAreas;     // left-side spawn volumes
    [SerializeField] private Transform _heroTransform;  // player hero

    [Header("Roster & Systems")]
    [SerializeField] private EnemyRosterSO _roster;
    [SerializeField] private EnemyCharacter _bossNight30Prefab;   // The Abyssal Horror

    [Header("Loot (same as original EnemyCommander)")]
    [SerializeField] private List<ItemData> _possibleLoot = new();
    [SerializeField][Range(0f, 1f)] private float _lootChance = 0.3f;

    [Header("Prestige (post night 30)")]
    [Tooltip("Extra stat multiplier applied every 10 nights after night 30")]
    [SerializeField] private float _prestigeMultiplierStep = 0.25f;

    // ── Subsystems (created in Awake) ────────────────────────────────────────
    private ThreatScanner _scanner;
    private CounterPickSelector _picker;
    private DifficultyBudget _budget;

    // ── Wave state ───────────────────────────────────────────────────────────
    private readonly List<EnemyCharacter> _activeEnemies = new();
    private readonly Dictionary<EnemyCharacter, (int col, float spawnTime)> _enemyMeta = new();
    // column → list of survival durations (seconds)
    private readonly Dictionary<int, List<float>> _survivalLog = new();

    private List<EnemyGroup> _pendingGroups = new();
    private bool _waveActive = false;
    private bool _isEndingWave = false;
    private int _spawnedCount = 0;
    private int _killedCount = 0;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Build subsystems
        _scanner = gameObject.AddComponent<ThreatScanner>();
        _budget = gameObject.AddComponent<DifficultyBudget>();
        _picker = new CounterPickSelector(_roster);
    }

    private void Start()
    {
        // Inject grid once BuildModeController has initialised it
        // If your BuildModeController exposes a static or singleton, pull from there:
        _grid = BuildModeController.Instance.GetGridData;
        _scanner.Inject(_grid);
        _scanner.InjectHero(_heroTransform);

        SubscribeToDayNightCycle();
    }

    private void OnDestroy() => UnsubscribeFromDayNightCycle();

    // ── Day/Night events ─────────────────────────────────────────────────────

    private void SubscribeToDayNightCycle()
    {
        if (DayNightCycleManager.Instance == null)
        {
            StartCoroutine(RetrySubscription());
            return;
        }
        DayNightCycleManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;
    }

    private IEnumerator RetrySubscription()
    {
        yield return null;
        SubscribeToDayNightCycle();
    }

    private void UnsubscribeFromDayNightCycle()
    {
        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
    }

    private void OnTimeOfDayChanged(TimeOfDay phase)
    {
        if (phase == TimeOfDay.Night)
            PrepareAndStartWave();
        else
            EndCurrentWave();
    }

    // ── Pre-night planning (runs at dusk) ────────────────────────────────────

    private void PrepareAndStartWave()
    {
        if (_waveActive) return;

        // 1. Advance budget (uses last night's kill stats)
        float budget = _budget.AdvanceNight(_spawnedCount, _killedCount);
        int night = _budget.NightNumber;

        // Reset per-night counters
        _spawnedCount = 0;
        _killedCount = 0;
        _survivalLog.Clear();

        // 2. Scan battlefield
        BattlefieldProfile profile = _scanner.Scan(night, budget);

        // 3. Apply prestige modifier after night 30
        if (night > 30)
        {
            int prestigeTier = (night - 30) / 10;
            profile.DifficultyBudget *= 1f + prestigeTier * _prestigeMultiplierStep;
        }

        // 4. Build group plan
        if (night == 30 && _bossNight30Prefab != null)
            _pendingGroups = BuildBossNight(profile);
        else
            _pendingGroups = _picker.BuildNight(profile);

        // 5. Start spawning
        if (_pendingGroups.Count > 0)
            StartCoroutine(RunAllGroups(_pendingGroups));
    }

    // ── Spawning ──────────────────────────────────────────────────────────────

    private IEnumerator RunAllGroups(List<EnemyGroup> groups)
    {
        _waveActive = true;
        _isEndingWave = false;

        // Run groups in parallel (each group fires on its own delay)
        foreach (var group in groups)
            StartCoroutine(SpawnGroup(group));

        yield break;
    }

    private IEnumerator SpawnGroup(EnemyGroup group)
    {
        if (group.Prefab == null)
        {
            Debug.LogWarning($"[SmartEnemyCommander] Group '{group.DebugName}' has null prefab — skipped.");
            yield break;
        }

        if (group.DelayBeforeGroup > 0f)
            yield return new WaitForSeconds(group.DelayBeforeGroup);

        for (int i = 0; i < group.Count; i++)
        {
            if (!_waveActive) yield break;   // dawn came early

            SpawnEnemy(group.Prefab, group.TargetColumn, group.StatMultiplier);

            if (group.TimeBetweenSpawns > 0f)
                yield return new WaitForSeconds(group.TimeBetweenSpawns);
        }
    }

    private void SpawnEnemy(EnemyCharacter prefab, int targetColumn, float statMult)
    {
        Vector3 pos = GetSpawnPosition(targetColumn);
        EnemyCharacter enemy = Instantiate(prefab, pos, Quaternion.identity);

        // Apply stat multiplier so late-game enemies feel meaningfully tougher
        ApplyStatMultiplier(enemy, statMult);

        // Loot
        foreach (var item in _possibleLoot)
            if (Random.value <= _lootChance)
                enemy.Inventory.AddItem(new ItemStack(item, 1));

        // Track
        _activeEnemies.Add(enemy);
        _enemyMeta[enemy] = (targetColumn, Time.time);
        _spawnedCount++;

        enemy.OnDeath += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyCharacter enemy)
    {
        enemy.OnDeath -= OnEnemyDied;
        _activeEnemies.Remove(enemy);

        // Record survival time for zone memory
        if (_enemyMeta.TryGetValue(enemy, out var meta))
        {
            float survived = Time.time - meta.spawnTime;
            if (!_survivalLog.ContainsKey(meta.col))
                _survivalLog[meta.col] = new List<float>();
            _survivalLog[meta.col].Add(survived);
            _enemyMeta.Remove(enemy);
        }

        _killedCount++;

        if (_waveActive && !_isEndingWave && _activeEnemies.Count == 0)
        {
            _waveActive = false;
            FeedbackSurvivalToScanner();
            Debug.Log("[SmartEnemyCommander] Wave cleared early — skipping to day.");

            // Small grace period so the player sees the victory moment
            StartCoroutine(SkipToDayAfterDelay(10f));
        }
    }

    // Add this method:
    private IEnumerator SkipToDayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only skip if we're still in Night (player could have mods that extend night, etc.)
        if (DayNightCycleManager.Instance != null &&
            DayNightCycleManager.Instance.CurrentTimeOfDay == TimeOfDay.Night)
        {
            DayNightCycleManager.Instance.SkipToNextPhase();
        }
    }
    // ── Dawn cleanup ──────────────────────────────────────────────────────────

    private void EndCurrentWave()
    {
        if (_isEndingWave) return;
        _isEndingWave = true;
        _waveActive = false;

        StopAllCoroutines();   // stop any in-progress spawn coroutines

        var toKill = new List<EnemyCharacter>(_activeEnemies);
        _activeEnemies.Clear();
        _enemyMeta.Clear();

        foreach (var enemy in toKill)
        {
            if (enemy == null) continue;
            enemy.OnDeath -= OnEnemyDied;
            enemy.Die(EnemyCharacter.DeathCause.KilledByCommander);
        }

        FeedbackSurvivalToScanner();
        _isEndingWave = false;
    }

    private void FeedbackSurvivalToScanner()
    {
        var avgByCol = new Dictionary<int, float>();
        foreach (var kv in _survivalLog)
        {
            float avg = 0f;
            foreach (float t in kv.Value) avg += t;
            avg /= kv.Value.Count;
            avgByCol[kv.Key] = avg;
        }
        _scanner.RecordNightResults(avgByCol);
    }

    // ── Boss night (Night 30) ─────────────────────────────────────────────────

    /// <summary>
    /// Three-phase attack:
    ///   Phase 1 – probe weakest zone with a swarm
    ///   Phase 2 – sappers target the strongest zone  
    ///   Phase 3 – boss enters from the most dangerous approach
    /// </summary>
    private List<EnemyGroup> BuildBossNight(BattlefieldProfile profile)
    {
        var groups = new List<EnemyGroup>();

        // Phase 1: hit the weakest zone
        if (profile.WeakestZone != null)
        {
            var swarm = _roster.Entries.Find(e => e.BudgetCost < 1.2f && e.UnlockNight <= 30);
            if (swarm != null)
                groups.Add(new EnemyGroup
                {
                    Prefab = swarm.Prefab,
                    DebugName = $"Boss Phase 1 – {swarm.DisplayName} swarm",
                    Count = 20,
                    TargetColumn = profile.WeakestZone.ColumnIndex,
                    DelayBeforeGroup = 0f,
                    TimeBetweenSpawns = 0.3f,
                    StatMultiplier = 2.5f,
                });
        }

        // Phase 2: siege unit at the most defended zone (stress-test it)
        if (profile.StrongestZone != null)
        {
            var siege = _roster.Entries.Find(e => e.IsSeige && e.UnlockNight <= 30);
            if (siege != null)
                groups.Add(new EnemyGroup
                {
                    Prefab = siege.Prefab,
                    DebugName = "Boss Phase 2 – Siege",
                    Count = 3,
                    TargetColumn = profile.StrongestZone.ColumnIndex,
                    DelayBeforeGroup = 30f,
                    TimeBetweenSpawns = 5f,
                    StatMultiplier = 2.8f,
                });
        }

        // Phase 3: the boss itself
        groups.Add(new EnemyGroup
        {
            Prefab = _bossNight30Prefab,
            DebugName = "The Abyssal Horror",
            Count = 1,
            TargetColumn = profile.WeakestZone?.ColumnIndex ?? 0,
            DelayBeforeGroup = 90f,   // gives player time to react to phases 1 & 2
            TimeBetweenSpawns = 0f,
            StatMultiplier = 3f,
        });

        return groups;
    }

    // ── Spawn position helpers ────────────────────────────────────────────────

    /// <summary>
    /// Spawn the enemy at the left edge (col 0) of the grid, in the row that
    /// corresponds to the targetColumn the commander chose.
    /// Enemies walk right — the targetColumn tells them which row to aim for.
    /// (EnemySeekState should read TargetColumn from the enemy's data.)
    /// </summary>
    private Vector3 GetSpawnPosition(int targetColumn)
    {
        if (_spawnAreas != null && _spawnAreas.Length > 0)
        {
            // Pick the spawn area closest to the target row
            SpawnArea area = _spawnAreas[targetColumn % _spawnAreas.Length];
            if (area != null) return area.GetRandomPoint();
        }

        if (_grid != null)
        {
            // Fallback: spawn at left edge, row = targetColumn
            int row = Mathf.Clamp(targetColumn, 0, _grid.Rows - 1);
            return _grid.GridToWorld(0, row);
        }

        return transform.position;
    }

    private static void ApplyStatMultiplier(EnemyCharacter enemy, float mult)
    {
        if (mult <= 1f) return;
        // Assumes EnemyCharacter exposes ScaleStats — adapt to your actual API
        // If you use a stat component, call it here:
        // enemy.GetComponent<CharacterStats>()?.ScaleBaseStats(mult);
        //enemy.ScaleStats(mult);
    }
}