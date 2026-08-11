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
    public const int MaxStageCount = 10;

    private const float MaxHealthUpgradeAmount = 15f;
    private const float MoveSpeedUpgradeAmount = 0.3f;
    private const float DamageUpgradeAmount = 0.1f;
    private const float AttackSpeedMultiplier = 0.9f;
    private const float RangeUpgradeAmount = 0.15f;
    private const float ShopHealPercent = 0.25f;
    private const float MaximumMoveSpeedBonus = 2.1f;
    private const float MaximumMaxHealthBonus = 120f;
    private const float MaximumDamageMultiplier = 1.8f;
    private const float MinimumCooldownMultiplier = 0.6f;
    private const float MaximumRangeMultiplier = 1.75f;
    private const float PortalBoundaryPadding = 1.5f;

    private EnemySpawner enemySpawner;
    private PlayerStats player;
    private Module1Ui ui;
    private StageGenerationConfig generationConfig;
    private readonly List<Portal> activePortals = new List<Portal>();
    private readonly List<StageType> pendingPortalChoices = new List<StageType>();
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
        LootPickup.ClearAll();
        isAwaitingPortalChoice = false;
        GamePauseManager.EnsureForScene().ResumeAll();
        ui.HideAllPanels();
        ApplyRunDataToPlayer(runManager.Data);
        if (runManager.Data.isNewRun)
        {
            // isNewRun is only a one-time full-health initialization flag. It
            // must be consumed before an in-session return to the main menu,
            // otherwise Continue would incorrectly heal Stage 1 to full.
            runManager.Data.isNewRun = false;
            runManager.SavePlayerState(player);
        }
        ui.UpdateStageHud(runManager.Data.currentStageIndex, MaxStageCount, runManager.Data.currentStageType);
        ui.ShowStageMessage(string.Empty);
        PrepareHiddenPortalSlots();

        if (runManager.Data.currentStageType == StageType.Shop)
        {
            GameEvents.ResetGlobalAggro();
            enemySpawner.enabled = false;
            GamePauseManager.EnsureForScene().Pause("Shop");
            ui.ShowWeaponShop(WeaponCatalog.AllTypes);
            return;
        }

        GameEvents.ResetGlobalAggro();
        enemySpawner.ConfigureStage(runManager.Data.currentStageType, runManager.Data.currentStageIndex);
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
        LootPickup.ClearAll();

        if (selectedStageType == StageType.End)
        {
            CompleteRun();
            return;
        }

        RunManager runManager = RunManager.EnsureInstance();
        runManager.PrepareNextStage(selectedStageType, player);

        // Reuse the same map and rebuild its stage state directly. This avoids
        // reloading the scene and depending on asynchronous runtime setup.
        BeginCurrentStage();
    }

    private void CompleteRun()
    {
        GamePauseManager.Instance?.ResumeAll();
        GameEvents.ResetGlobalAggro();
        enemySpawner?.StopCurrentStage();
        ClearActiveProjectiles();
        LootPickup.ClearAll();
        RunManager.EnsureInstance().EndRun();

        ui.HideAllPanels();
        ui.ShowStageMessage("Run Complete");
        ui.ShowVictoryMenu();
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
        LootPickup.ClearAll();
        GamePauseManager.Instance?.ResumeAll();
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
        LootPickup.ClearAll();
        GamePauseManager.Instance?.ResumeAll();

        Vector2 respawnPosition = MapBoundary.Instance != null
            ? MapBoundary.Instance.GetCenter()
            : Vector2.zero;
        player.ReviveAt(respawnPosition);
        RunManager.EnsureInstance().BeginNewRun();
    }

    public void ApplyLevelUpgrade(ShopUpgradeType upgrade)
    {
        RunManager runManager = RunManager.EnsureInstance();
        RunData data = runManager.Data;

        switch (upgrade)
        {
            case ShopUpgradeType.Heal:
                player.Heal(player.maxHealth * ShopHealPercent);
                break;
            case ShopUpgradeType.MaxHealth:
                data.maxHealthBonus = Mathf.Min(
                    MaximumMaxHealthBonus,
                    data.maxHealthBonus + MaxHealthUpgradeAmount);
                ApplyRunDataToPlayer(data);
                break;
            case ShopUpgradeType.MoveSpeed:
                data.moveSpeedBonus = Mathf.Min(
                    MaximumMoveSpeedBonus,
                    data.moveSpeedBonus + MoveSpeedUpgradeAmount);
                ApplyRunDataToPlayer(data);
                break;
            case ShopUpgradeType.WeaponDamage:
                data.weaponDamageMultiplier = Mathf.Min(
                    MaximumDamageMultiplier,
                    data.weaponDamageMultiplier + DamageUpgradeAmount);
                break;
            case ShopUpgradeType.AttackSpeed:
                data.cooldownMultiplier = Mathf.Max(
                    MinimumCooldownMultiplier,
                    data.cooldownMultiplier * AttackSpeedMultiplier);
                break;
            case ShopUpgradeType.AttackRange:
                data.attackRangeMultiplier = Mathf.Min(
                    MaximumRangeMultiplier,
                    data.attackRangeMultiplier + RangeUpgradeAmount);
                break;
        }

        runManager.SavePlayerState(player);
        runManager.SaveRun();
        player.GetComponent<PlayerProgression>()?.RefreshHud();
    }

    public void PurchaseWeapon(WeaponType weaponType)
    {
        RunManager runManager = RunManager.EnsureInstance();
        if (runManager.Data.equippedWeapon == weaponType)
        {
            ui.ShowStageMessage("That weapon is already equipped.");
            return;
        }

        WeaponDefinition definition = WeaponCatalog.Get(weaponType);
        PlayerProgression progression = player.GetComponent<PlayerProgression>();
        if (progression == null || !progression.TrySpendCoins(definition.Price))
        {
            ui.ShowStageMessage("Not enough coins for that weapon.");
            return;
        }

        runManager.Data.equippedWeapon = weaponType;
        player.GetComponent<PlayerWeaponSystem>()?.Equip(weaponType);
        runManager.SavePlayerState(player);
        FinishShop();
    }

    public void LeaveShop()
    {
        FinishShop();
    }

    private void FinishShop()
    {
        ui.HideShop();
        ui.ShowStageMessage(string.Empty);
        GamePauseManager.Instance?.Resume("Shop");
        RevealPortalChoices();
    }

    public string GetUpgradeLabel(ShopUpgradeType upgrade)
    {
        return upgrade switch
        {
            ShopUpgradeType.Heal => "Restore 25% Health",
            ShopUpgradeType.MaxHealth => "+15 Maximum Health",
            ShopUpgradeType.MoveSpeed => "+0.30 Move Speed",
            ShopUpgradeType.WeaponDamage => "+10% Weapon Damage",
            ShopUpgradeType.AttackSpeed => "+10% Attack Speed",
            ShopUpgradeType.AttackRange => "+15% Attack Range",
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

        RevealPortalChoices();
    }

    /// <summary>
    /// Resolves the next-stage choices when the current stage begins, but only
    /// displays empty, disabled frames. Their destinations remain concealed
    /// until RevealPortalChoices is called.
    /// </summary>
    private void PrepareHiddenPortalSlots()
    {
        pendingPortalChoices.Clear();
        pendingPortalChoices.AddRange(GetPortalChoices());
        if (pendingPortalChoices.Count == 0)
        {
            return;
        }

        Vector2 center = MapBoundary.Instance != null
            ? MapBoundary.Instance.GetCenter()
            : Vector2.zero;

        for (int i = 0; i < pendingPortalChoices.Count; i++)
        {
            Vector2 portalPosition = GetPortalSlotPosition(center, i, pendingPortalChoices.Count);
            if (MapBoundary.Instance != null)
            {
                portalPosition = MapBoundary.Instance.ClampPosition(portalPosition, PortalBoundaryPadding);
            }

            Portal portal = Portal.CreateHidden(
                pendingPortalChoices[i],
                portalPosition,
                this);
            activePortals.Add(portal);
        }
    }

    private void RevealPortalChoices()
    {
        if (isAwaitingPortalChoice || activePortals.Count == 0)
        {
            return;
        }

        isAwaitingPortalChoice = true;
        ui.ShowStageMessage("Choose your next portal");

        foreach (Portal portal in activePortals)
        {
            if (portal != null)
            {
                portal.Reveal();
            }
        }
    }

    private static Vector2 GetPortalSlotPosition(Vector2 center, int index, int portalCount)
    {
        const float verticalOffset = 3.5f;
        const float horizontalOffset = 3.25f;

        if (portalCount <= 1)
        {
            return center + Vector2.up * verticalOffset;
        }

        float xOffset = index == 0 ? -horizontalOffset : horizontalOffset;
        return center + new Vector2(xOffset, verticalOffset);
    }

    private List<StageType> GetPortalChoices()
    {
        RunData data = RunManager.EnsureInstance().Data;

        // Stage 9 always leads to the dedicated final Boss room. After the
        // Stage 10 boss is defeated, the same hidden slot becomes the End portal.
        if (data.currentStageIndex >= MaxStageCount)
        {
            return new List<StageType> { StageType.End };
        }

        if (data.currentStageIndex == MaxStageCount - 1)
        {
            return new List<StageType> { StageType.Boss };
        }

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
            StageType.Boss => 1f,
            StageType.End => 1f,
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
        player.GetComponent<PlayerWeaponSystem>()?.Equip(data.equippedWeapon, false);
        player.GetComponent<PlayerProgression>()?.RefreshHud();
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
        pendingPortalChoices.Clear();
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
            FindObjectsInactive.Exclude);

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

        RuntimeWeaponProjectile[] runtimeProjectiles = Object.FindObjectsByType<RuntimeWeaponProjectile>(
            FindObjectsInactive.Exclude);
        foreach (RuntimeWeaponProjectile projectile in runtimeProjectiles)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }

        WeaponVisualEffect[] effects = Object.FindObjectsByType<WeaponVisualEffect>(
            FindObjectsInactive.Exclude);
        foreach (WeaponVisualEffect effect in effects)
        {
            if (effect != null)
            {
                Destroy(effect.gameObject);
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
