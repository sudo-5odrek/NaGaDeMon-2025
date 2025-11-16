using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Data")]
    public LevelWaveData waveData;

    // Stores last spawn position to enforce spacing
    private Dictionary<SpawnGroupData, Vector2> lastSpawnPositions = new Dictionary<SpawnGroupData, Vector2>();

    private float levelStartTime;

    void Start()
    {
        if (waveData == null)
        {
            Debug.LogError("No Wave Data assigned to WaveManager!");
            return;
        }

        levelStartTime = Time.time;
        StartCoroutine(RunWaves());
    }

    // ======================================================================
    //  RUN WAVES SEQUENTIALLY BASED ON START TIMES
    // ======================================================================
    IEnumerator RunWaves()
    {
        foreach (var wave in waveData.waves)
        {
            float waitTime = (levelStartTime + wave.startTime) - Time.time;

            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            Debug.Log($"▶ Starting Wave: {wave.waveName}");

            foreach (var group in wave.groups)
                StartCoroutine(RunSpawnGroup(group));
        }
    }

    // ======================================================================
    //  RUN A SINGLE SPAWN GROUP BASED ON ITS PATTERN
    // ======================================================================
    IEnumerator RunSpawnGroup(SpawnGroupData group)
    {
        yield return new WaitForSeconds(group.startDelay);

        switch (group.pattern)
        {
            case SpawnGroupData.PatternMode.Sequential:
                yield return StartCoroutine(SpawnSequential(group));
                break;

            case SpawnGroupData.PatternMode.Cycle:
                yield return StartCoroutine(SpawnCycled(group));
                break;

            case SpawnGroupData.PatternMode.RandomWeight:
                yield return StartCoroutine(SpawnWeighted(group));
                break;
        }
    }

    // ======================================================================
    //  SPAWN PATTERNS
    // ======================================================================

    // ------------------ Sequential: AAAA BBB CCC ------------------
    IEnumerator SpawnSequential(SpawnGroupData group)
    {
        foreach (var entry in group.enemies)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.prefab, group);
                yield return new WaitForSeconds(group.stagger);
            }
        }
    }

    // ------------------ Cycle: ABC ABC ABC ------------------
    IEnumerator SpawnCycled(SpawnGroupData group)
    {
        var remaining = new List<int>();
        foreach (var e in group.enemies)
            remaining.Add(e.count);

        bool done = false;

        while (!done)
        {
            done = true;

            for (int i = 0; i < group.enemies.Count; i++)
            {
                if (remaining[i] > 0)
                {
                    done = false;

                    SpawnEnemy(group.enemies[i].prefab, group);
                    remaining[i]--;

                    yield return new WaitForSeconds(group.stagger);
                }
            }
        }
    }

    // ------------------ Weighted Random ------------------
    IEnumerator SpawnWeighted(SpawnGroupData group)
    {
        var remaining = new List<int>();
        foreach (var e in group.enemies)
            remaining.Add(e.count);

        int totalToSpawn = 0;
        foreach (var e in group.enemies)
            totalToSpawn += e.count;

        for (int i = 0; i < totalToSpawn; i++)
        {
            int chosen = PickWeightedIndex(group, remaining);

            SpawnEnemy(group.enemies[chosen].prefab, group);
            remaining[chosen]--;

            yield return new WaitForSeconds(group.stagger);
        }
    }

    // Weighted random selection helper
    int PickWeightedIndex(SpawnGroupData group, List<int> remaining)
    {
        float totalWeight = 0f;

        for (int i = 0; i < group.enemies.Count; i++)
        {
            if (remaining[i] > 0)
                totalWeight += group.enemies[i].weight;
        }

        float r = Random.value * totalWeight;

        for (int i = 0; i < group.enemies.Count; i++)
        {
            if (remaining[i] > 0)
            {
                r -= group.enemies[i].weight;
                if (r <= 0)
                    return i;
            }
        }

        return 0; // fallback
    }

    // ======================================================================
    //  ENEMY SPAWNING (INCLUDES RANDOM ZONE PICKING + AUTO TARGET ASSIGN)
    // ======================================================================
    void SpawnEnemy(GameObject prefab, SpawnGroupData group)
    {
        if (prefab == null)
        {
            Debug.LogWarning("SpawnEnemy called with a NULL prefab.");
            return;
        }

        Vector2 spawnPos = PickSpawnPoint(group);

        GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Assign Nexus target
        var mover = enemyObj.GetComponent<Enemy.EnemyPathMover>();
        if (mover != null)
        {
            Transform nexus = FindNexus();
            mover.target = nexus;
            mover.RecalculatePath();
        }
    }

    // ======================================================================
    //  SPAWNPOINT LOGIC WITH ANTI-CLUMPING
    // ======================================================================

    Vector2 PickSpawnPoint(SpawnGroupData group)
    {
        const float minDistance = 0.8f;   // distance new spawn must keep from previous
        const int maxAttempts = 8;

        Vector2 lastPos = Vector2.positiveInfinity;

        if (lastSpawnPositions.ContainsKey(group))
            lastPos = lastSpawnPositions[group];

        Vector2 candidate = Vector2.zero;

        for (int attempts = 0; attempts < maxAttempts; attempts++)
        {
            candidate = group.spawnZone.GetRandomPoint();

            if (lastPos == Vector2.positiveInfinity ||
                Vector2.Distance(candidate, lastPos) > minDistance)
            {
                lastSpawnPositions[group] = candidate;
                return candidate;
            }
        }

        // fallback if unlucky
        lastSpawnPositions[group] = candidate;
        return candidate;
    }

    // ======================================================================
    //  FIND NEXUS
    // ======================================================================
    Transform FindNexus()
    {
        Nexus nexus = FindObjectOfType<Nexus>();
        if (nexus != null)
            return nexus.transform;

        Debug.LogError("No Nexus found in scene!");
        return null;
    }

    // ======================================================================
    //  GIZMOS — DRAW SPAWN ZONES
    // ======================================================================
    void OnDrawGizmos()
    {
        if (waveData == null)
            return;

        Gizmos.color = new Color(0, 1, 0, 0.2f);

        foreach (var wave in waveData.waves)
        {
            foreach (var group in wave.groups)
            {
                if (group.spawnZone == null) continue;

                Vector2 c = group.spawnZone.center;
                Vector2 s = group.spawnZone.size;

                // solid
                Gizmos.color = new Color(0, 1, 0, 0.15f);
                Gizmos.DrawCube(c, s);

                // outline
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(c, s);
            }
        }
    }
}
