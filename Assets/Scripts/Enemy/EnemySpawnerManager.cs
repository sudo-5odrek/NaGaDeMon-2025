using Enemy;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public Transform nexus;
    public float spawnDistance = 10f; // distance around the nexus
    public float startInterval = 3f;  // initial delay between spawns
    public float minInterval = 0.5f;  // fastest possible spawn rate
    public float acceleration = 0.05f; // how fast interval decreases per spawn
    
    public EnemyAttack.EnemyAttackMode currentAttackMode = EnemyAttack.EnemyAttackMode.Impact;

    float currentInterval;
    float timer;

    void Start()
    {
        currentInterval = startInterval;
        timer = currentInterval;
    }

    void Update()
    {
        if (!enemyPrefab || !nexus) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnEnemy();
            // Accelerate spawn rate
            currentInterval = Mathf.Max(minInterval, currentInterval - acceleration);
            timer = currentInterval;
        }
    }

    void SpawnEnemy()
    {
        // pick a random direction around the nexus
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = nexus.position + (Vector3)dir * spawnDistance;

        // spawn and orient enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        if (enemy.TryGetComponent<EnemyPathMover>(out var mover))
        {
            mover.target = nexus; // assign the nexus as the target
        }
        if (enemy.TryGetComponent(out EnemyAttack attack))
            attack.SetAttackMode(currentAttackMode);

        Debug.DrawLine(nexus.position, spawnPos, Color.red, 2f);
    }
}