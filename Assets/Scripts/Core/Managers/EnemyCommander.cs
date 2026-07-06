using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a single wave of enemies.
/// Configure in the Inspector or build via code.
/// </summary>
[System.Serializable]
public class WaveData
{
    [Tooltip("Enemy prefab to spawn for this wave")]
    public EnemyCharacter enemyPrefab;

    [Tooltip("How many enemies to spawn")]
    public int count;

    [Tooltip("Seconds between individual spawns")]
    public float timeBetweenSpawns = 0.5f;

    [Tooltip("Extra delay after night begins before this wave starts")]
    public float delayBeforeWave = 3f;
}

/// <summary>
/// Wave lifecycle manager. Responsibilities:
///   - Listen to DayNightCycleManager and start/end waves.
///   - Spawn enemies into SpawnAreas.
///   - Track living enemies so it knows when a wave is cleared.
///   - Kill survivors at dawn.
///
/// It does NOT command enemies after spawn. Each enemy's own behavior
/// tree handles seek/attack autonomously via EnemySeekState.
/// </summary>

public class EnemyCommander : MonoBehaviour
{
    [Header("Loot")]
    [SerializeField] private List<ItemData> possibleLoot = new();
    [SerializeField][Range(0f, 1f)] private float lootChance = 0.3f;

    [Header("Spawn Areas")]
    [SerializeField] private SpawnArea[] spawnAreas;

    [Header("Waves")]
    [SerializeField] private List<WaveData> waves;
    [SerializeField] private bool loopWaves = false;

    private readonly List<EnemyCharacter> _activeEnemies = new();
    private int _currentWaveIndex = 0;
    private bool _waveActive = false;
    private bool _isEndingWave = false; // guard against re-entry

    private void Start()
    {
        SubscribeToDayNightCycle();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDayNightCycle();
    }

    // ── Day/Night ─────────────────────────────────────────────────────────

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

    private void OnTimeOfDayChanged(TimeOfDay newPhase)
    {
        if (newPhase == TimeOfDay.Night)
            TryStartNextWave();
        else
            EndCurrentWave();
    }

    // ── Wave control ──────────────────────────────────────────────────────

    private void TryStartNextWave()
    {
        if (_waveActive) return;
        if (waves == null || waves.Count == 0) return;

        if (_currentWaveIndex >= waves.Count)
        {
            if (loopWaves) _currentWaveIndex = 0;
            else return;
        }

        StartCoroutine(RunWave(waves[_currentWaveIndex]));
    }

    private IEnumerator RunWave(WaveData wave)
    {
        _waveActive = true;
        _isEndingWave = false;

        if (wave.delayBeforeWave > 0f)
            yield return new WaitForSeconds(wave.delayBeforeWave);

        for (int i = 0; i < wave.count; i++)
        {
            SpawnEnemy(wave.enemyPrefab);
            if (wave.timeBetweenSpawns > 0f)
                yield return new WaitForSeconds(wave.timeBetweenSpawns);
        }

        _currentWaveIndex++;
        Debug.Log($"[EnemyCommander] Wave {_currentWaveIndex} spawned ({wave.count} enemies).");
    }

    private void EndCurrentWave()
    {
        if (_isEndingWave) return;
        _isEndingWave = true;
        _waveActive = false;

        // Copy list — Die() triggers OnEnemyDied which modifies _activeEnemies
        var toKill = new List<EnemyCharacter>(_activeEnemies);
        _activeEnemies.Clear();

        foreach (var enemy in toKill)
        {
            if (enemy == null) continue;
            enemy.OnDeath -= OnEnemyDied; // unsubscribe first so OnEnemyDied doesn't fire
            enemy.Die(EnemyCharacter.DeathCause.KilledByCommander);
        }

        _isEndingWave = false;
    }

    // ── Spawning ──────────────────────────────────────────────────────────

    private void SpawnEnemy(EnemyCharacter prefab)
    {
        Vector3 pos = GetRandomSpawnPosition();
        EnemyCharacter enemy = Instantiate(prefab, pos, Quaternion.identity);

        foreach (var item in possibleLoot)
            if (Random.value <= lootChance)
                enemy.Inventory.AddItem(new ItemStack(item, 1));

        _activeEnemies.Add(enemy);
        enemy.OnDeath += OnEnemyDied;
    }

    private void OnEnemyDied(EnemyCharacter enemy)
    {
        enemy.OnDeath -= OnEnemyDied;
        _activeEnemies.Remove(enemy);

        // Only fire wave cleared if wave ended naturally — not from EndCurrentWave
        if (_waveActive && !_isEndingWave && _activeEnemies.Count == 0)
        {
            _waveActive = false;
            Debug.Log("[EnemyCommander] Wave cleared!");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnAreas == null || spawnAreas.Length == 0)
            return transform.position;

        SpawnArea area = spawnAreas[Random.Range(0, spawnAreas.Length)];
        return area != null ? area.GetRandomPoint() : transform.position;
    }
}