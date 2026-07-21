using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public List<EnemyGroup> enemyGroups;
        public int waveQuota;
        public float spawnInterval;
        public int spawnCount;
    }

    [System.Serializable]
    public class EnemyGroup
    {
        public string enemyName;
        public int enemyCount;
        public int spawnCount;
        public GameObject enemyPrefab;
        public bool isBoss;
    }

    [Header("Pooling")]
    public bool useObjectPooling = true;

    public static readonly List<EnemyStats> activeEnemies = new List<EnemyStats>();

    [Header("Fallback Waves")]
    public List<Wave> waves;
    public int currentWaveCount;
    public GameObject reinforcedEnemyPrefab;

    [Header("Spawner Attributes")]
    public float waveInterval;
    public int enemiesAlive;
    public int maxEnemiesAllowed;
    public bool maxEnemiesReached;

    [Header("Spawn Position")]
    public List<Transform> relativeSpawnPoints;

    [Header("Spawn Randomness")]
    public float spawnRadius = 4f;
    public float enemySpacing = 0.5f;
    public float minDistanceFromPlayer = 5f;

    private float spawnTimer;
    private bool isWaitingForNextWave;
    private bool moduleStageManaged;
    private bool moduleStageComplete;
    private Transform player;
    private GameObject normalEnemyPrefab;

    private void Start()
    {
        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("EnemySpawner: PlayerMovement not found.");
            enabled = false;
            return;
        }

        if (waves == null || waves.Count == 0)
        {
            Debug.LogError("EnemySpawner: No waves assigned.");
            enabled = false;
            return;
        }

        if (relativeSpawnPoints == null || relativeSpawnPoints.Count == 0)
        {
            Debug.LogError("EnemySpawner: No spawn points assigned.");
            enabled = false;
            return;
        }

        player = playerMovement.transform;
        normalEnemyPrefab ??= GetFallbackEnemyPrefab();

        if (!moduleStageManaged)
        {
            CalculateWaveQuota();
        }
    }

    private void Update()
    {
        if (moduleStageManaged)
        {
            UpdateModuleStage();
            return;
        }

        UpdateFallbackWaves();
    }

    /// <summary>
    /// Called by StageManager. It reuses the teammate's spawn, pooling, and
    /// death-counting mechanics while changing only the wave data for a node.
    /// </summary>
    public void ConfigureStage(StageType stageType, int stageIndex)
    {
        normalEnemyPrefab ??= GetFallbackEnemyPrefab();
        reinforcedEnemyPrefab ??= normalEnemyPrefab;

        if (normalEnemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: No base enemy prefab is available for the procedural stage.");
            return;
        }

        moduleStageManaged = true;
        moduleStageComplete = false;
        currentWaveCount = 0;
        spawnTimer = 0f;
        enemiesAlive = 0;
        maxEnemiesReached = false;

        int normalCount = 6 + Mathf.Max(0, stageIndex - 1) * 2;
        int reinforcedCount = Mathf.Max(1, stageIndex / 2);
        List<EnemyGroup> groups = new List<EnemyGroup>();

        if (stageType == StageType.Elite)
        {
            groups.Add(CreateGroup("Elite Boss", 1, reinforcedEnemyPrefab, true));
            groups.Add(CreateGroup("Reinforced Bat", reinforcedCount + 1, reinforcedEnemyPrefab));
            groups.Add(CreateGroup("Bat", normalCount, normalEnemyPrefab));
        }
        else
        {
            groups.Add(CreateGroup("Bat", normalCount, normalEnemyPrefab));
            groups.Add(CreateGroup("Reinforced Bat", reinforcedCount, reinforcedEnemyPrefab));
        }

        Wave proceduralWave = new Wave
        {
            waveName = $"{stageType} Stage {stageIndex}",
            enemyGroups = groups,
            spawnInterval = Mathf.Max(0.35f, 0.85f - stageIndex * 0.03f),
            spawnCount = 0
        };

        waves = new List<Wave> { proceduralWave };
        CalculateWaveQuota();
    }

    private EnemyGroup CreateGroup(string name, int count, GameObject prefab, bool isBoss = false)
    {
        return new EnemyGroup
        {
            enemyName = name,
            enemyCount = count,
            spawnCount = 0,
            enemyPrefab = prefab,
            isBoss = isBoss
        };
    }

    private void UpdateModuleStage()
    {
        if (waves == null || waves.Count == 0 || currentWaveCount >= waves.Count || moduleStageComplete)
        {
            return;
        }

        Wave currentWave = waves[currentWaveCount];
        if (currentWave.spawnCount >= currentWave.waveQuota && enemiesAlive <= 0)
        {
            moduleStageComplete = true;
            GameEvents.RaiseWaveCleared();
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentWave.spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemies();
        }
    }

    private void UpdateFallbackWaves()
    {
        if (waves == null || currentWaveCount >= waves.Count)
        {
            return;
        }

        if (waves[currentWaveCount].spawnCount >= waves[currentWaveCount].waveQuota
            && enemiesAlive <= 0
            && !isWaitingForNextWave)
        {
            StartCoroutine(BeginNextWave());
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= waves[currentWaveCount].spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemies();
        }
    }

    private System.Collections.IEnumerator BeginNextWave()
    {
        isWaitingForNextWave = true;
        yield return new WaitForSeconds(waveInterval);

        if (currentWaveCount < waves.Count - 1)
        {
            currentWaveCount++;
            CalculateWaveQuota();
            spawnTimer = 0f;
        }

        isWaitingForNextWave = false;
    }

    private void CalculateWaveQuota()
    {
        if (waves == null || currentWaveCount >= waves.Count)
        {
            return;
        }

        int currentWaveQuota = 0;
        foreach (EnemyGroup enemyGroup in waves[currentWaveCount].enemyGroups)
        {
            currentWaveQuota += enemyGroup.enemyCount;
        }

        waves[currentWaveCount].waveQuota = currentWaveQuota;
    }

    private Vector2 GetSpawnPosition()
    {
        Vector2 fallbackPosition = player != null ? player.position : transform.position;

        for (int i = 0; i < 20; i++)
        {
            Transform spawnPoint = relativeSpawnPoints[Random.Range(0, relativeSpawnPoints.Count)];
            Vector2 spawnPosition = (Vector2)spawnPoint.position + Random.insideUnitCircle * spawnRadius;
            fallbackPosition = spawnPosition;

            if (player != null && Vector2.Distance(player.position, spawnPosition) < minDistanceFromPlayer)
            {
                continue;
            }

            if (MapBoundary.Instance == null || MapBoundary.Instance.IsInside(spawnPosition, enemySpacing))
            {
                return spawnPosition;
            }
        }

        return MapBoundary.Instance != null
            ? MapBoundary.Instance.ClampPosition(fallbackPosition, enemySpacing)
            : fallbackPosition;
    }

    private void SpawnEnemies()
    {
        if (currentWaveCount >= waves.Count || maxEnemiesReached)
        {
            return;
        }

        Wave currentWave = waves[currentWaveCount];
        if (currentWave.spawnCount >= currentWave.waveQuota)
        {
            return;
        }

        foreach (EnemyGroup enemyGroup in currentWave.enemyGroups)
        {
            if (enemyGroup.spawnCount >= enemyGroup.enemyCount)
            {
                continue;
            }

            if (enemiesAlive >= maxEnemiesAllowed)
            {
                maxEnemiesReached = true;
                return;
            }

            if (enemyGroup.enemyPrefab == null)
            {
                Debug.LogWarning($"EnemySpawner: Enemy prefab missing in group {enemyGroup.enemyName}.");
                continue;
            }

            Vector2 spawnPosition = GetSpawnPosition();
            GameObject spawnedEnemy = useObjectPooling && ObjectPoolManager.Instance != null
                ? ObjectPoolManager.Instance.GetObject(enemyGroup.enemyPrefab, spawnPosition, Quaternion.identity)
                : Instantiate(enemyGroup.enemyPrefab, spawnPosition, Quaternion.identity);

            if (enemyGroup.isBoss)
            {
                StageEnemyModifier modifier = spawnedEnemy.GetComponent<StageEnemyModifier>();
                if (modifier == null)
                {
                    modifier = spawnedEnemy.AddComponent<StageEnemyModifier>();
                }

                modifier.ConfigureAsBoss();
            }

            enemyGroup.spawnCount++;
            currentWave.spawnCount++;
            enemiesAlive++;
        }

        if (enemiesAlive < maxEnemiesAllowed)
        {
            maxEnemiesReached = false;
        }
    }

    private GameObject GetFallbackEnemyPrefab()
    {
        if (waves == null)
        {
            return null;
        }

        foreach (Wave wave in waves)
        {
            if (wave.enemyGroups == null)
            {
                continue;
            }

            foreach (EnemyGroup group in wave.enemyGroups)
            {
                if (group.enemyPrefab != null)
                {
                    return group.enemyPrefab;
                }
            }
        }

        return null;
    }

    public void OnEnemyKilled()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
    }

    /// <summary>
    /// Stops the currently generated stage without raising WaveCleared. This is
    /// used when the player dies so a lingering enemy cannot complete a wave
    /// behind the death menu.
    /// </summary>
    public void StopCurrentStage()
    {
        enabled = false;
        StopAllCoroutines();
        moduleStageComplete = true;
        isWaitingForNextWave = false;
        spawnTimer = 0f;
        enemiesAlive = 0;
        maxEnemiesReached = false;

        List<EnemyStats> enemiesToRelease = new List<EnemyStats>(activeEnemies);
        foreach (EnemyStats enemy in enemiesToRelease)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReleaseObject(enemy.gameObject);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }

        activeEnemies.Clear();
    }
}
