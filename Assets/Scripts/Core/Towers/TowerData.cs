using UnityEngine;

[CreateAssetMenu(fileName = "New Tower Data", menuName = "Game/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Tower Properties")]
    public string towerName;
    public float baseRange = 10f;
    public float baseDamage = 10f;
    public float baseFireRate = 1f; // shots per second
    public float rotationSpeed = 5f;

    [Header("Targeting")]
    public int targetCapacity = 20;

    [Header("Visual")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
    public Transform firePoint; // where projectiles spawn

    [Header("Audio")]
    public AudioClip shootSound;
}
