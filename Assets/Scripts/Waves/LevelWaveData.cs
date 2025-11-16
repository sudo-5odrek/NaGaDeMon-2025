using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelWaveData", menuName = "Levels/Level Wave Data")]
public class LevelWaveData : ScriptableObject
{
    public List<WaveData> waves = new List<WaveData>();
}

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave";
    public float startTime; // seconds from start of level

    public List<SpawnGroupData> groups = new List<SpawnGroupData>();
}

// ======================================================================
//  SPAWN GROUP DATA (now uses SpawnZone instead of Transform spawnPoint)
// ======================================================================
[System.Serializable]
public class SpawnGroupData
{
    [Header("Spawn Zone")]
    public SpawnZone spawnZone;  // ← REPLACES spawnPoint

    [Header("Timing")]
    public float stagger = 0.3f;
    public float startDelay = 0f;

    [Header("Enemies")]
    public List<EnemyEntryData> enemies = new List<EnemyEntryData>();

    public enum PatternMode
    {
        Sequential,      // AAAA BBB CCC
        Cycle,           // ABC ABC ABC
        RandomWeight     // weighted random pick
    }

    public PatternMode pattern = PatternMode.Sequential;
}

// ======================================================================
//  MULTI-ENEMY GROUP ENTRY
// ======================================================================
[System.Serializable]
public class EnemyEntryData
{
    public GameObject prefab;
    public int count = 1;      // how many of THIS enemy to spawn
    public float weight = 1;   // only used in RandomWeight mode
}

// ======================================================================
//  SPAWN ZONE (rectangular area for randomized spawns)
// ======================================================================
[System.Serializable]
public class SpawnZone
{
    public Vector2 center;
    public Vector2 size; // width, height

    public Vector2 GetRandomPoint()
    {
        return new Vector2(
            center.x + Random.Range(-size.x * 0.5f, size.x * 0.5f),
            center.y + Random.Range(-size.y * 0.5f, size.y * 0.5f)
        );
    }
}
