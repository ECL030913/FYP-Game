using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Module 1's procedural director. It listens for a combat wave to finish,
/// creates constrained portal choices, and reconstructs the next node in the
/// same arena scene.
/// </summary>
public class StageManager : MonoBehaviour
{
    private const float MaxHealthUpgradeAmount = 25f;
    private const float MoveSpeedUpgradeAmount = 0.75f;
    private const float DamageUpgradeAmount = 0.2f;
    private const float AttackSpeedMultiplier = 0.85f;
    private const float RangeUpgradeAmount = 0.25f;
    private const float ShopHealPercent = 0.3f;
    private const float PortalBoundaryPadding = 1.5f;

    private EnemySpawner enemySpawner;
    private PlayerStats player;
    private Module1Ui ui;
    private StageGenerationConfig generationConfig;
    private readonly List<Portal> activePortals = new List<Portal>();
    private bool isAwaitingPortalChoice;

    private void OnEnable()
    {
        GameEvents.WaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        GameEvents.WaveCleared -= HandleWaveCleared;
    }

    private void Start()
    {
        CacheSceneReferences();
        RunManager runManager = RunManager.EnsureInstance();

        if (runManager.IsRunReady)
        {
            BeginCurrentStage();
            return;
        }

        if (enemySpawner != null)
        {
            enemySpawner.enabled = false;
        }

        // The dedicated Menu scene now owns New Game and Continue. If Game is
        // opened directly without a prepared run, return to the real menu
        // instead of showing the old runtime placeholder.
        SceneManager.LoadScene("Menu");
    }

    public void BeginCurrentStage()
    {
        CacheSceneReferences();
        RunManager runManager = RunManager.EnsureInstance();
        if (!runManager.IsRunReady || player == null || enemySpawner == null)
        {
            return;
        }

        ClearPortals();
        isAwaitingPortalChoice = false;
        ui.HideAllPanels();
        ApplyRunDataToPlayer(runManager.Data);
        ui.UpdateStageHud(runManager.Data.currentRoundIndex, runManager.Data.currentStageType);
        ui.ShowStageMessage(string.Empty);

        if (runManager.Data.currentStageType == StageType.Shop)
        {
            GameEvents.ResetGlobalAggro();
            enemySpawner.enabled = false;
            ui.ShowShop(GetShopChoices());
            return;
        }

        GameEvents.ResetGlobalAggro();
        enemySpawner.ConfigureStage(runManager.Data.currentStageType, runManager.Data.currentRoundIndex);
        enemySpawner.enabled = true;
    }

    public void SelectPortal(StageType selectedStageType)
    {
        if (!isAwaitingPortalChoice)
        {
            return;
        }

        isAwaitingPortalChoice = false;
        SetPortalsInteractable(false);
        ClearPortals();
        RunManager runManager = RunManager.EnsureInstance();
        runManager.PrepareNextStage(selectedStageType, player);

        // Reuse the same map and rebuild its stage state directly. This avoids
        // reloading the scene and depending on asynchronous runtime setup.
        BeginCurrentStage();
    }

    /// <summary>
    /// Handles the terminal state of a run. The active stage is stopped before
    /// the UI is shown, so enemies and portals cannot keep progressing while
    /// the player decides whether to retry or quit.
    /// </summary>
    public void HandlePlayerDeath(PlayerStats deadPlayer)
    {
        CacheSceneReferences();
        if (deadPlayer == null || deadPlayer != player)
        {
            return;
        }

        isAwaitingPortalChoice = false;
        SetPortalsInteractable(false);
        ClearPortals();
        GameEvents.ResetGlobalAggro();
        enemySpawner?.StopCurrentStage();
        ClearActiveProjectiles();
        RunManager.EnsureInstance().EndRun();

        ui.HideAllPanels();
        ui.ShowStageMessage("You Died");
        ui.ShowDeathMenu();
    }

    /// <summary>
    /// Retry is intentionally a new run: the player returns to the arena
    /// centre with base stats and Stage 1 Combat begins immediately.
    /// </summary>
    public void RetryFromDeath()
    {
        CacheSceneReferences();
        if (player == null)
        {
            return;
        }

        isAwaitingPortalChoice = false;
        SetPortalsInteractable(false);
        ClearPortals();
        GameEvents.ResetGlobalAggro();
        enemySpawner?.StopCurrentStage();
        ClearActiveProjectiles();

        Vector2 respawnPosition = MapBoundary.Instance != null
            ? MapBoundary.Instance.GetCenter()
            : Vector2.zero;
        player.ReviveAt(respawnPosition);
        RunManager.EnsureInstance().BeginNewRun();
    }

    public void PurchaseUpgrade(ShopUpgradeType upgrade)
    {
        RunManager runManager = RunManager.EnsureInstance();
        RunData data = runManager.Data;

        switch (upgrade)
        {
            case ShopUpgradeType.Heal:
                player.Heal(player.maxHealth * ShopHealPercent);
                break;
            case ShopUpgradeType.MaxHealth:
                data.maxHealthBonus += MaxHealthUpgradeAmount;
                ApplyRunDataToPlayer(data);
                break;
            case ShopUpgradeType.MoveSpeed:
                data.moveSpeedBonus += MoveSpeedUpgradeAmount;
                ApplyRunDataToPlayer(data);
                break;
            case ShopUpgradeType.WeaponDamage:
                data.weaponDamageMultiplier += DamageUpgradeAmount;
                break;
            case ShopUpgradeType.AttackSpeed:
                data.cooldownMultiplier = Mathf.Max(0.35f, data.cooldownMultiplier * AttackSpeedMultiplier);
                break;
            case ShopUpgradeType.AttackRange:
                data.attackRangeMultiplier += RangeUpgradeAmount;
                break;
        }

        runManager.SavePlayerState(player);
        runManager.SaveRun();
        ui.HideShop();
        SpawnPortalChoices();
    }

    public string GetUpgradeLabel(ShopUpgradeType upgrade)
    {
        return upgrade switch
        {
            ShopUpgradeType.Heal => "Restore 30% Health",
            ShopUpgradeType.MaxHealth => "+25 Maximum Health",
            ShopUpgradeType.MoveSpeed => "+0.75 Move Speed",
            ShopUpgradeType.WeaponDamage => "+20% Weapon Damage",
            ShopUpgradeType.AttackSpeed => "+15% Attack Speed",
            ShopUpgradeType.AttackRange => "+25% Attack Range",
            _ => upgrade.ToString()
        };
    }

    private void HandleWaveCleared()
    {
        RunManager runManager = RunManager.Instance;
        if (runManager == null || !runManager.IsRunReady || runManager.Data.currentStageType == StageType.Shop)
        {
            return;
        }

        if (enemySpawner != null)
        {
            enemySpawner.enabled = false;
        }

        SpawnPortalChoices();
    }

    private void SpawnPortalChoices()
    {
        if (isAwaitingPortalChoice)
        {
            return;
        }

        List<StageType> choices = GetPortalChoices();
        if (choices.Count == 0)
        {
            return;
        }

        isAwaitingPortalChoice = true;
        ui.ShowStageMessage("Choose your next portal");

        Vector2[] directions =
        {
            Vector2.up,
            new Vector2(-0.866f, -0.5f),
            new Vector2(0.866f, -0.5f)
        };

        Vector2 origin = player != null ? player.transform.position : Vector2.zero;
        for (int i = 0; i < choices.Count; i++)
        {
            Vector2 portalPosition = origin + directions[i] * 4.5f;
            if (MapBoundary.Instance != null)
            {
                portalPosition = MapBoundary.Instance.ClampPosition(portalPosition, PortalBoundaryPadding);
            }

            Portal portal = Portal.Create(choices[i], portalPosition, this);
            activePortals.Add(portal);
        }
    }

    private List<StageType> GetPortalChoices()
    {
        RunData data = RunManager.EnsureInstance().Data;
        List<StageType> available = new List<StageType> { StageType.Combat };

        if (data.currentStageType != StageType.Shop)
        {
            available.Add(StageType.Shop);
        }

        if (CanOfferElite(data))
        {
            available.Add(StageType.Elite);
        }

        int desiredCount = Mathf.Min(
            Mathf.Clamp(generationConfig.portalChoicesPerWave, 1, 2),
            available.Count);
        List<StageType> choices = new List<StageType>();
        List<StageType> uniquePool = new List<StageType>(available);

        while (choices.Count < desiredCount && uniquePool.Count > 0)
        {
            StageType selected = PickWeightedStage(uniquePool);
            choices.Add(selected);
            uniquePool.Remove(selected);
        }

        return choices;
    }

    private bool CanOfferElite(RunData data)
    {
        if (data.eliteStagesCompleted == 0)
        {
            // Stage 1 is always Combat. Once it is cleared, an Elite portal
            // may lead directly to Stage 2.
            return data.currentStageIndex >= generationConfig.firstEliteStageIndex - 1;
        }

        return data.nonEliteStagesSinceLastElite >= generationConfig.minimumNonEliteStagesBetweenElites;
    }

    private StageType PickWeightedStage(IReadOnlyList<StageType> candidates)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += GetStageWeight(candidates[i]);
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= GetStageWeight(candidates[i]);
            if (roll <= 0f)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private float GetStageWeight(StageType stageType)
    {
        return stageType switch
        {
            StageType.Combat => generationConfig.combatWeight,
            StageType.Shop => generationConfig.shopWeight,
            StageType.Elite => generationConfig.eliteWeight,
            _ => 1f
        };
    }

    private List<ShopUpgradeType> GetShopChoices()
    {
        List<ShopUpgradeType> pool = new List<ShopUpgradeType>
        {
            ShopUpgradeType.Heal,
            ShopUpgradeType.MaxHealth,
            ShopUpgradeType.MoveSpeed,
            ShopUpgradeType.WeaponDamage,
            ShopUpgradeType.AttackSpeed,
            ShopUpgradeType.AttackRange
        };

        List<ShopUpgradeType> choices = new List<ShopUpgradeType>();
        while (choices.Count < 3)
        {
            int index = Random.Range(0, pool.Count);
            choices.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return choices;
    }

    private void ApplyRunDataToPlayer(RunData data)
    {
        player.ApplyRunData(data);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement?.SetRuntimeSpeedBonus(data.moveSpeedBonus);
    }

    private void ClearPortals()
    {
        foreach (Portal portal in activePortals)
        {
            if (portal != null)
            {
                Destroy(portal.gameObject);
            }
        }

        activePortals.Clear();
    }

    private void SetPortalsInteractable(bool interactable)
    {
        foreach (Portal portal in activePortals)
        {
            if (portal != null)
            {
                portal.SetInteractable(interactable);
            }
        }
    }

    private static void ClearActiveProjectiles()
    {
        ProjectileWeaponController[] projectiles = Object.FindObjectsByType<ProjectileWeaponController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (ProjectileWeaponController projectile in projectiles)
        {
            if (projectile == null)
            {
                continue;
            }

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReleaseObject(projectile.gameObject);
            }
            else
            {
                Destroy(projectile.gameObject);
            }
        }
    }

    private void CacheSceneReferences()
    {
        enemySpawner ??= FindAnyObjectByType<EnemySpawner>();
        player ??= FindAnyObjectByType<PlayerStats>();
        ui ??= Module1Ui.EnsureForScene();
        generationConfig ??= Resources.Load<StageGenerationConfig>("StageGenerationConfig");

        if (generationConfig == null)
        {
            generationConfig = ScriptableObject.CreateInstance<StageGenerationConfig>();
        }
    }
}
