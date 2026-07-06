using UnityEngine;

/// <summary>
/// Tracks the rolling difficulty budget across all 30 nights.
///
/// Budget is a resource the CounterPickSelector spends to field enemies.
/// It ramps with night number AND adjusts based on how well the player did
/// last night — keeping the challenge in the "hard but possible" zone.
///
/// Performance metric: fraction of enemies that reached the player base.
/// 0.0 = all enemies killed (player dominated) → raise budget
/// 1.0 = all enemies leaked (player overwhelmed) → hold or slightly lower
/// </summary>
public class DifficultyBudget : MonoBehaviour
{
    // ── Tuning ──────────────────────────────────────────────────────────────
    [Header("Night Budget Curve")]
    [Tooltip("Budget on night 1")]
    [SerializeField] private float _startBudget = 10f;

    [Tooltip("Budget ceiling at night 30")]
    [SerializeField] private float _maxBudget = 300f;

    [Tooltip("Exponent of the budget ramp curve. 1 = linear, >1 = slow start/steep end")]
    [SerializeField] private float _rampExponent = 1.8f;

    [Header("Adaptive Adjustment")]
    [Tooltip("If player kills >X% of enemies, raise budget by this fraction")]
    [SerializeField] private float _dominanceThreshold = 0.85f;

    [Tooltip("Budget raise when player dominates (multiplier on top of base ramp)")]
    [SerializeField] private float _dominanceBump = 0.12f;

    [Tooltip("If player leaks >X% of enemies, clamp budget growth to 0")]
    [SerializeField] private float _struggleThreshold = 0.45f;

    [Tooltip("Budget reduction when player is overwhelmed (prevents unfair death spirals)")]
    [SerializeField] private float _struggleRelief = 0.08f;

    // ── State ────────────────────────────────────────────────────────────────
    private float _currentBudget;
    private int _currentNight = 0;

    // ── Public API ───────────────────────────────────────────────────────────

    public float Current => _currentBudget;

    private void Awake()
    {
        _currentBudget = _startBudget;
    }

    /// <summary>
    /// Advance to the next night. Call this at dusk BEFORE scanning threats.
    /// enemiesSpawned / enemiesKilled: totals from the previous night.
    /// Pass 0/0 on night 1 (no previous night).
    /// </summary>
    public float AdvanceNight(int enemiesSpawned, int enemiesKilled)
    {
        _currentNight++;

        float baseBudget = ComputeBaseBudget(_currentNight);

        if (enemiesSpawned > 0)
        {
            float killRate = enemiesKilled / (float)enemiesSpawned;
            baseBudget = ApplyPerformanceAdjustment(baseBudget, killRate);
        }

        _currentBudget = baseBudget;

        Debug.Log($"[DifficultyBudget] Night {_currentNight}: budget = {_currentBudget:F1} " +
                  $"(base {ComputeBaseBudget(_currentNight):F1}, " +
                  $"kills {enemiesKilled}/{enemiesSpawned})");

        return _currentBudget;
    }

    /// <returns>Current night number (1-indexed).</returns>
    public int NightNumber => _currentNight;

    // ── Private helpers ──────────────────────────────────────────────────────

    private float ComputeBaseBudget(int night)
    {
        float t = Mathf.Clamp01((night - 1f) / 29f);   // 0 on night 1, 1 on night 30
        return Mathf.Lerp(_startBudget, _maxBudget, Mathf.Pow(t, _rampExponent));
    }

    private float ApplyPerformanceAdjustment(float baseBudget, float killRate)
    {
        if (killRate >= _dominanceThreshold)
        {
            // Player dominated → push harder
            float bump = baseBudget * _dominanceBump;
            Debug.Log($"[DifficultyBudget] Player dominated ({killRate:P0}) → +{bump:F1} budget");
            return baseBudget + bump;
        }

        if (killRate <= _struggleThreshold)
        {
            // Player struggling → give slight relief so it doesn't spiral
            float relief = baseBudget * _struggleRelief;
            Debug.Log($"[DifficultyBudget] Player struggling ({killRate:P0}) → -{relief:F1} budget");
            return baseBudget - relief;
        }

        // Middle range: normal ramp, no adjustment
        return baseBudget;
    }
}