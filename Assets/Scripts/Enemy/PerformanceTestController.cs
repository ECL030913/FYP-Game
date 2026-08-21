using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Configures an EnemySpawner and the live PlayerWeaponSystem for a repeatable
/// pooling-versus-no-pooling performance test. Attach this component to the
/// PerformanceTest scene. Pooling is toggled through the single master switch
/// on ObjectPoolManager (poolingEnabled), so this compares the real gameplay
/// path with pooling on vs off rather than two different code paths.
/// </summary>
public class PerformanceTestController : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int testEnemyCount = 50;
    [SerializeField] private int maxConcurrentEnemies = 50;
    [SerializeField] private bool usePooling = true;
    [SerializeField] private float testDuration = 30f;
    [SerializeField] private float spawnInterval = 0.15f;
    [SerializeField] private float weaponCooldownOverride = 0.1f;

    private EnemySpawner spawner;
    private PlayerWeaponSystem playerWeaponSystem;
    private float elapsed;
    private bool testRunning;
    private float fpsAccumulator;
    private int fpsFrameCount;
    private float minFps = float.MaxValue;
    private float maxFps;
    private float avgFps;
    private float currentFps;
    private float cooldownOverrideTimer;

    private void Awake()
    {
        // EnemySpawner.Start rejects an empty wave list. Supply a harmless
        // placeholder so it can initialize its player and spawn-point state
        // before this controller installs the actual test wave.
        EnemySpawner sceneSpawner = FindAnyObjectByType<EnemySpawner>();
        if (sceneSpawner == null || sceneSpawner.waves != null && sceneSpawner.waves.Count > 0)
        {
            return;
        }

        sceneSpawner.waves = new List<EnemySpawner.Wave>
        {
            new EnemySpawner.Wave
            {
                waveName = "Performance Test Initialization",
                enemyGroups = new List<EnemySpawner.EnemyGroup>(),
                waveQuota = 0,
                spawnInterval = 0f,
                spawnCount = 0
            }
        };
    }

    private void Start()
    {
        StartCoroutine(InitAfterOneFrame());
    }

    private IEnumerator InitAfterOneFrame()
    {
        // Let EnemySpawner.Start initialize its player and spawn-point references first.
        yield return null;

        ConfigureSpawner();
    }

    private void ConfigureSpawner()
    {
        spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogError("PerformanceTest: EnemySpawner not found in the scene.");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("PerformanceTest: Assign an enemy prefab before running the test.");
            return;
        }

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.poolingEnabled = usePooling;
        }
        else
        {
            Debug.LogWarning("PerformanceTest: no ObjectPoolManager in the scene — pooling toggle has no effect and every spawn falls back to Instantiate/Destroy.");
        }

        playerWeaponSystem = FindAnyObjectByType<PlayerWeaponSystem>();
        if (playerWeaponSystem == null)
        {
            Debug.LogWarning("PerformanceTest: no PlayerWeaponSystem in the scene — only enemy pooling is being tested, not weapon projectile/effect pooling. Add a Player object (e.g. an instance of the GameplayCore prefab) to this scene to also exercise RuntimeWeaponProjectile/WeaponVisualEffect pooling.");
        }

        int enemyCount = Mathf.Max(0, testEnemyCount);
        spawner.maxEnemiesAllowed = Mathf.Max(1, maxConcurrentEnemies);
        spawner.waveInterval = 0f;

        EnemySpawner.EnemyGroup enemyGroup = new EnemySpawner.EnemyGroup
        {
            enemyName = "Test Enemy",
            enemyCount = enemyCount,
            spawnCount = 0,
            enemyPrefab = enemyPrefab,
            isBoss = false,
            isFinalBoss = false
        };

        EnemySpawner.Wave wave = new EnemySpawner.Wave
        {
            waveName = "Stress Test",
            enemyGroups = new List<EnemySpawner.EnemyGroup> { enemyGroup },
            waveQuota = enemyCount,
            spawnInterval = Mathf.Max(0f, spawnInterval),
            spawnCount = 0
        };

        spawner.waves = new List<EnemySpawner.Wave> { wave };
        spawner.currentWaveCount = 0;
        spawner.enabled = true;

        elapsed = 0f;
        fpsAccumulator = 0f;
        fpsFrameCount = 0;
        minFps = float.MaxValue;
        maxFps = 0f;
        avgFps = 0f;
        currentFps = 0f;
        cooldownOverrideTimer = 0f;
        ApplyCooldownOverride();
        testRunning = true;
    }

    private void Update()
    {
        if (!testRunning)
        {
            return;
        }

        spawner.enemiesAlive = EnemySpawner.activeEnemies
            .Count(enemy => enemy != null && enemy.gameObject.activeInHierarchy);

        cooldownOverrideTimer += Time.deltaTime;
        if (cooldownOverrideTimer >= 0.5f)
        {
            cooldownOverrideTimer = 0f;
            ApplyCooldownOverride();
        }

        elapsed += Time.unscaledDeltaTime;
        currentFps = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
        minFps = Mathf.Min(minFps, currentFps);
        maxFps = Mathf.Max(maxFps, currentFps);
        fpsAccumulator += currentFps;
        fpsFrameCount++;

        if (elapsed < testDuration)
        {
            return;
        }

        testRunning = false;
        spawner.StopCurrentStage();
        avgFps = fpsFrameCount > 0 ? fpsAccumulator / fpsFrameCount : 0f;

        string mode = usePooling ? "WITH POOL" : "NO POOL";
        Debug.Log(
            $"[PerformanceTest] RESULT | Mode: {mode} | Enemies: {testEnemyCount} | " +
            $"AvgFPS: {avgFps:F1} | MinFPS: {minFps:F1} | MaxFPS: {maxFps:F1}");
    }

    /// <summary>
    /// Forces the equipped weapon to fire roughly every weaponCooldownOverride
    /// seconds, regardless of its own base cooldown stat, by driving the same
    /// cooldownMultiplier the real upgrade system uses
    /// (definition.Cooldown * cooldownMultiplier == weaponCooldownOverride).
    /// This keeps the stress test on the exact same code path real players use
    /// instead of reflecting into a private field.
    /// </summary>
    private void ApplyCooldownOverride()
    {
        if (playerWeaponSystem == null)
        {
            playerWeaponSystem = FindAnyObjectByType<PlayerWeaponSystem>();
            if (playerWeaponSystem == null)
            {
                return;
            }
        }

        WeaponDefinition definition = WeaponCatalog.Get(playerWeaponSystem.EquippedWeapon);
        float baseCooldown = Mathf.Max(0.01f, definition.Cooldown);
        float overrideCooldown = Mathf.Max(0f, weaponCooldownOverride);

        RunManager.EnsureInstance().Data.cooldownMultiplier = overrideCooldown / baseCooldown;
    }

    private void OnGUI()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(new Rect(10f, 10f, 280f, testRunning ? 205f : 175f), GUIContent.none);
        GUI.color = previousColor;

        string mode = usePooling ? "WITH POOL" : "NO POOL";
        if (testRunning)
        {
            GUI.Label(new Rect(20f, 20f, 250f, 25f), "=== PERFORMANCE TEST ===");
            GUI.Label(new Rect(20f, 45f, 250f, 25f), $"Mode: {mode}");
            GUI.Label(new Rect(20f, 70f, 250f, 25f), $"Enemies: {testEnemyCount}");
            GUI.Label(new Rect(20f, 95f, 250f, 25f), $"Alive: {(spawner != null ? spawner.enemiesAlive : 0)}");
            GUI.Label(new Rect(20f, 120f, 250f, 25f), $"Current FPS: {currentFps:F1}");
            GUI.Label(new Rect(20f, 145f, 250f, 25f), $"Min FPS: {minFps:F1}");
            GUI.Label(new Rect(20f, 170f, 250f, 25f), $"Max FPS: {maxFps:F1}");
            GUI.Label(new Rect(20f, 195f, 250f, 25f), $"Time: {elapsed:F1} / {testDuration:F0}s");
            return;
        }

        GUI.Label(new Rect(20f, 20f, 250f, 25f), "=== TEST COMPLETE ===");
        GUI.Label(new Rect(20f, 45f, 250f, 25f), $"Mode: {mode}");
        GUI.Label(new Rect(20f, 70f, 250f, 25f), $"Enemies spawned: {testEnemyCount}");
        GUI.Label(new Rect(20f, 95f, 250f, 25f), $"Avg FPS: {avgFps:F1}");
        GUI.Label(new Rect(20f, 120f, 250f, 25f), $"Min FPS: {minFps:F1}");
        GUI.Label(new Rect(20f, 145f, 250f, 25f), $"Max FPS: {maxFps:F1}");
        GUI.Label(new Rect(20f, 170f, 250f, 25f), "Open Unity Profiler to see GC.Alloc data");
    }
}
