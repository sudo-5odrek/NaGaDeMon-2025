using System.Collections.Generic;
using UnityEngine;

namespace Features.Enemies.Scripts
{
    [CreateAssetMenu(fileName = "LevelWaveData", menuName = "Levels/Level Wave Data")]
    public class LevelWaveData : ScriptableObject
    {
        public List<WaveData> waves = new List<WaveData>();
    }

    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Wave";

        public enum WaveMode
        {
            Regular,   // old behavior: spawn once at startTime
            Cycle      // NEW: repeatedly spawn groups during a time window
        }

        [Header("Mode")]
        public WaveMode mode = WaveMode.Regular;

        [Header("Regular Wave Settings")]
        public float startTime; // seconds from level start

        [Header("Cycle Wave Settings")]
        public float cycleStartTime = 0f;
        public float cycleEndTime = 10f;
        public float cycleDelayBetweenGroups = 1.5f;  // How long between group cycles

        [Header("Spawn Groups")]
        public List<SpawnGroupData> groups = new List<SpawnGroupData>();
    }

    // ======================================================================
    //  SPAWN GROUP DATA
    // ======================================================================
    [System.Serializable]
    public class SpawnGroupData
    {
        [Header("Spawn Zone")]
        public SpawnZone spawnZone;

        [Header("Timing")]
        public float stagger = 0.3f;
        public float startDelay = 0f;

        [Header("Cycle Mode")]
        public float cycleGroupDelay = 0.5f; // delay before re-cycling group

        [Header("Enemies")]
        public List<EnemyEntryData> enemies = new List<EnemyEntryData>();

        public enum PatternMode
        {
            Sequential,
            Cycle,
            RandomWeight
        }

        public PatternMode pattern = PatternMode.Sequential;
    }

    // ======================================================================
    //  ENEMY ENTRY
    // ======================================================================
    [System.Serializable]
    public class EnemyEntryData
    {
        public GameObject prefab;
        public int count = 1;
        public float weight = 1;
    }

    // ======================================================================
    //  SPAWN ZONE
    // ======================================================================
    [System.Serializable]
    public class SpawnZone
    {
        public Vector2 center;
        public Vector2 size;

        public Vector2 GetRandomPoint()
        {
            return new Vector2(
                center.x + Random.Range(-size.x * 0.5f, size.x * 0.5f),
                center.y + Random.Range(-size.y * 0.5f, size.y * 0.5f)
            );
        }
    }
}
