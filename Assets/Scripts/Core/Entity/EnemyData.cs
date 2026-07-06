using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ClickMage/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public float moveSpeed = 3.5f;
    public float attackRange = 1.5f;     // how close to start attacking
    public float attackDamage = 10f;
    public float attackCooldown = 1f;    // seconds between hits
    public float detectionRadius = 50f;  // how far it scans for structures
}
