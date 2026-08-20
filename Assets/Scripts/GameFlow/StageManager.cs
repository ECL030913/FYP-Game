using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Module 1's procedural director. It listens for a combat wave to finish,
/// creates constrained portal choices, saves the run, and routes the player to
/// a dedicated scene for the selected stage type.
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
    private GameplaySceneDefinition sceneDefinition;
    private ShopSceneController shopSceneController;
    private readonly List<Portal> activePortals = new List<Portal>();
    private readonly List<StageType> pendingPortalChoices = new List<StageType>();
    private bool isAwaitingPortalChoice;
    private bool isTransitioning;

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
            if (sceneDefinition != null
                && sceneDefinition.StageType != runManager.Data.currentStageType)
            {
                isTransitioning = true;
                if (enemySpawner != null)
                {
                    enemySpawner.enabled = false;
                }

                StageSceneRouter.LoadStageAsync(runManager.Data.currentStageType);
                return;
            }

            BeginCurrentStage();
            return;
        }

        if (enemySpawner != null)
        {
            enemySpawner.enabled = false;
        }

        // Gameplay scenes require a prepared run from New Game or Continue.
        StageSceneRouter.LoadMenuAsync();
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
        isTransitioning = false;
        GamePauseManager.EnsureForScene().ResumeAll();
        ui.HideAllPanels();
        PlacePlayerAtSceneSpawn();
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
            enemySpawner.StopCurrentStage();
            RevealPortalChoices();
            shopSceneController = ShopSceneController.EnsureForScene();
            shopSceneController.Initialize(this, sceneDefinition);
            ui.ShowStageMessage("Shop: inspect a display or use a portal to continue");
            return;
        }

        GameEvents.ResetGlobalAggro();
        enemySpawner.ConfigureStage(runManager.Data.currentStageType, runManager.Data.currentStageIndex);
        enemySpawner.enabled = true;
    }

    public void SelectPortal(StageType selectedStageType)
    {
        if (!isAwaitingPortalChoice || isTransitioning)
        {
            return;
        }

        // Validate before mutating or saving the run. Scene loading starts only
        // after the old stage has finished releasing enemies, projectiles and
        // pooled objects; starting it earlier can race scene activation against
        // old-scene cleanup and leave the transition permanently stalled.
        if (selectedStageType != StageType.End
            && !StageSceneRouter.CanLoadStage(selectedStageType))
        {
            SetPortalsInteractable(true);
            ui.ShowStageMessage($"The {selectedStageType} scene is unavailable.");
            return;
        }

        isTransitioning = true;
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
        PrepareForSceneExit();
        StartCoroutine(LoadStageAfterCleanup(selectedStageType));
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
        if (player == null || isTransitioning)
        {
            return;
        }

        if (!StageSceneRouter.CanLoadStage(StageType.Combat))
        {
            ui.ShowStageMessage("The Combat scene is unavailable.");
            return;
        }

        isTransitioning = true;
        isAwaitingPortalChoice = false;
        SetPortalsInteractable(false);
        ClearPortals();
        GameEvents.ResetGlobalAggro();
        enemySpawner?.StopCurrentStage();
        ClearActiveProjectiles();
        LootPickup.ClearAll();
        GamePauseManager.Instance?.ResumeAll();

        RunManager.EnsureInstance().BeginNewRun();
        PrepareForSceneExit();
        StartCoroutine(LoadStageAfterCleanup(StageType.Combat));
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

    public void RequestShopPurchase(ShopPedestal pedestal)
    {
        if (pedestal == null
            || isTransitioning
            || RunManager.Instance == null
            || RunManager.Instance.Data.currentStageType != StageType.Shop)
        {
            return;
        }

        if (!CanPurchaseShopOffer(pedestal, out string unavailableReason, out _))
        {
            ui.ShowStageMessage(unavailableReason);
            pedestal.RefreshAvailability();
            return;
        }

        GetShopOfferPresentation(
            pedestal,
            out string title,
            out string details,
            out _,
            out _);
        GamePauseManager.EnsureForScene().Pause("ShopPurchase");
        ui.ShowShopPurchaseConfirmation(
            $"Buy {title}?",
            details,
            () => ConfirmShopPurchase(pedestal),
            CancelShopPurchase);
    }

    public void GetShopOfferPresentation(
        ShopPedestal pedestal,
        out string title,
        out string details,
        out string action,
        out Color actionColour)
    {
        RunData data = RunManager.EnsureInstance().Data;
        if (pedestal.OfferKind == ShopOfferKind.Weapon)
        {
            WeaponDefinition definition = WeaponCatalog.Get(pedestal.WeaponType);
            title = $"{definition.DisplayName}  •  {pedestal.Price} coins";
            details = $"{definition.Description}\n{BuildEffectiveWeaponStats(definition, data)}\nCurrent coins: {data.coins}";
        }
        else if (pedestal.OfferKind == ShopOfferKind.HealthPotion)
        {
            title = $"Health Potion  •  {pedestal.Price} coins";
            details = $"Restore {pedestal.HealPercent * 100f:0}% of maximum health.\n"
                + $"Current HP: {player.currentHealth:0} / {player.maxHealth:0}  •  Current coins: {data.coins}";
        }
        else
        {
            title = $"Prismatic Upgrade Potion  •  {pedestal.Price} coins";
            details = "Gain one immediate three-choice permanent upgrade. Level and XP are unchanged.\n"
                + $"Repeatable in this Shop  •  Current coins: {data.coins}";
        }

        if (CanPurchaseShopOffer(pedestal, out string unavailableReason, out Color unavailableColour))
        {
            action = "Press E to buy";
            actionColour = new Color(0.35f, 1f, 0.5f);
        }
        else
        {
            action = unavailableReason;
            actionColour = unavailableColour;
        }
    }

    public bool IsShopOfferConsumed(ShopPedestal pedestal)
    {
        if (pedestal == null || RunManager.Instance == null)
        {
            return true;
        }

        RunData data = RunManager.Instance.Data;
        if (pedestal.OfferKind == ShopOfferKind.Weapon)
        {
            return data.shopWeaponPurchased || data.equippedWeapon == pedestal.WeaponType;
        }

        return pedestal.OfferKind == ShopOfferKind.HealthPotion
            && data.shopPotionPurchased;
    }

    private void ConfirmShopPurchase(ShopPedestal pedestal)
    {
        string message;
        bool purchased;
        switch (pedestal.OfferKind)
        {
            case ShopOfferKind.Weapon:
                purchased = TryCompleteWeaponPurchase(
                    pedestal.WeaponType,
                    pedestal.Price,
                    out message);
                break;
            case ShopOfferKind.HealthPotion:
                purchased = TryCompletePotionPurchase(
                    pedestal.Price,
                    pedestal.HealPercent,
                    out message);
                break;
            default:
                purchased = TryCompleteUpgradePotionPurchase(
                    pedestal.Price,
                    out message);
                break;
        }

        GamePauseManager.Instance?.Resume("ShopPurchase");
        ui.ShowStageMessage(message);
        shopSceneController?.RefreshAll();
        if (purchased)
        {
            player.GetComponent<PlayerProgression>()?.RefreshHud();
        }
    }

    private void CancelShopPurchase()
    {
        GamePauseManager.Instance?.Resume("ShopPurchase");
        ui.ShowStageMessage(string.Empty);
        shopSceneController?.RefreshAll();
    }

    private bool TryCompleteWeaponPurchase(WeaponType weaponType, int price, out string message)
    {
        RunManager runManager = RunManager.EnsureInstance();
        RunData data = runManager.Data;
        if (data.shopWeaponPurchased)
        {
            message = "A weapon has already been purchased in this Shop.";
            return false;
        }

        if (data.equippedWeapon == weaponType)
        {
            message = "That weapon is already equipped.";
            return false;
        }

        PlayerProgression progression = player.GetComponent<PlayerProgression>();
        if (progression == null || !progression.TrySpendCoins(price))
        {
            message = "Not enough coins for that weapon.";
            return false;
        }

        data.equippedWeapon = weaponType;
        data.shopWeaponPurchased = true;
        player.GetComponent<PlayerWeaponSystem>()?.Equip(weaponType);
        runManager.SavePlayerState(player);
        message = $"Equipped {WeaponCatalog.Get(weaponType).DisplayName}.";
        return true;
    }

    private bool TryCompletePotionPurchase(int price, float healPercent, out string message)
    {
        RunManager runManager = RunManager.EnsureInstance();
        RunData data = runManager.Data;
        if (data.shopPotionPurchased)
        {
            message = "The potion has already been purchased in this Shop.";
            return false;
        }

        if (player.currentHealth >= player.maxHealth - 0.01f)
        {
            message = "Health is already full.";
            return false;
        }

        PlayerProgression progression = player.GetComponent<PlayerProgression>();
        if (progression == null || !progression.TrySpendCoins(price))
        {
            message = "Not enough coins for the potion.";
            return false;
        }

        player.Heal(player.maxHealth * Mathf.Clamp01(healPercent));
        data.shopPotionPurchased = true;
        runManager.SavePlayerState(player);
        message = "Health restored.";
        return true;
    }

    private bool TryCompleteUpgradePotionPurchase(int price, out string message)
    {
        PlayerProgression progression = player != null
            ? player.GetComponent<PlayerProgression>()
            : null;
        if (progression == null || !progression.TrySpendCoins(price))
        {
            message = "Not enough coins for the upgrade potion.";
            return false;
        }

        progression.GrantBonusUpgradeChoice();
        RunManager.Instance?.SavePlayerState(player);
        RunManager.Instance?.SaveRun();
        message = "Choose one bonus permanent upgrade.";
        return true;
    }

    private bool CanPurchaseShopOffer(
        ShopPedestal pedestal,
        out string unavailableReason,
        out Color unavailableColour)
    {
        RunData data = RunManager.EnsureInstance().Data;
        unavailableColour = new Color(1f, 0.35f, 0.32f);

        if (pedestal.OfferKind == ShopOfferKind.Weapon)
        {
            if (data.shopWeaponPurchased)
            {
                unavailableReason = "Weapon purchase already used in this Shop";
                unavailableColour = new Color(0.68f, 0.72f, 0.78f);
                return false;
            }

            if (data.equippedWeapon == pedestal.WeaponType)
            {
                unavailableReason = "Currently equipped";
                unavailableColour = new Color(0.68f, 0.82f, 1f);
                return false;
            }
        }
        else if (pedestal.OfferKind == ShopOfferKind.HealthPotion)
        {
            if (data.shopPotionPurchased)
            {
                unavailableReason = "Potion already purchased in this Shop";
                unavailableColour = new Color(0.68f, 0.72f, 0.78f);
                return false;
            }

            if (player == null || player.currentHealth >= player.maxHealth - 0.01f)
            {
                unavailableReason = "Health is already full";
                unavailableColour = new Color(0.68f, 0.82f, 1f);
                return false;
            }
        }

        if (data.coins < pedestal.Price)
        {
            unavailableReason = $"Not enough coins ({data.coins} / {pedestal.Price})";
            return false;
        }

        unavailableReason = string.Empty;
        return true;
    }

    private static string BuildEffectiveWeaponStats(WeaponDefinition definition, RunData data)
    {
        float damage = definition.Damage * data.weaponDamageMultiplier;
        float cooldown = definition.Cooldown * data.cooldownMultiplier;
        float range = definition.Range * data.attackRangeMultiplier;
        float area = definition.AreaRadius * data.attackRangeMultiplier;
        string common = $"Damage {damage:0.0}  •  Cooldown {cooldown:0.00}s";

        return definition.Type switch
        {
            WeaponType.MeleeArea => $"{common}  •  Radius {area:0.00}",
            WeaponType.MeleePierce => $"{common}  •  Reach {range:0.00}  •  Pierce {definition.Pierce}",
            WeaponType.RangedPierce => $"{common}  •  Range {range:0.0}  •  Pierce {definition.Pierce}  •  Speed {definition.ProjectileSpeed:0}",
            WeaponType.RangedArea => $"{common}  •  Range {range:0.0}  •  Blast {area:0.00}  •  Speed {definition.ProjectileSpeed:0}",
            _ => common
        };
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
            Vector2 portalPosition = sceneDefinition != null
                ? sceneDefinition.GetPortalPosition(i, pendingPortalChoices.Count)
                : GetPortalSlotPosition(center, i, pendingPortalChoices.Count);
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

    private void ApplyRunDataToPlayer(RunData data)
    {
        player.ApplyRunData(data);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        movement?.SetRuntimeSpeedBonus(data.moveSpeedBonus);
        player.GetComponent<PlayerWeaponSystem>()?.Equip(data.equippedWeapon, false);
        player.GetComponent<PlayerProgression>()?.RefreshHud();
    }

    private void PlacePlayerAtSceneSpawn()
    {
        if (player == null || sceneDefinition == null)
        {
            return;
        }

        Vector2 spawnPosition = sceneDefinition.PlayerSpawnPosition;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            player.transform.position = spawnPosition;
        }
    }

    private void PrepareForSceneExit()
    {
        GamePauseManager.Instance?.ResumeAll();
        GameEvents.ResetGlobalAggro();
        enemySpawner?.StopCurrentStage();
        ClearActiveProjectiles();
        LootPickup.ClearAll();
        ui?.HideAllPanels();

        PlayerMovement movement = player != null
            ? player.GetComponent<PlayerMovement>()
            : null;
        if (movement != null)
        {
            movement.enabled = false;
        }
    }

    private IEnumerator LoadStageAfterCleanup(StageType stageType)
    {
        // Destroy() and pool releases complete at the end of the current frame.
        // Waiting one frame keeps those operations out of the scene loader's
        // activation/unload phase.
        yield return null;

        AsyncOperation operation = StageSceneRouter.LoadStageAsync(stageType);
        if (operation != null)
        {
            yield break;
        }

        // CanLoadStage was checked before the transition, so this is only an
        // unexpected loader failure. Return to Menu rather than leaving the
        // player disabled in a scene whose run data already points elsewhere.
        Debug.LogError($"Stage transition to {stageType} could not start.");
        StageSceneRouter.LoadMenuAsync();
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
        sceneDefinition ??= FindAnyObjectByType<GameplaySceneDefinition>();
        generationConfig ??= Resources.Load<StageGenerationConfig>("StageGenerationConfig");

        if (generationConfig == null)
        {
            generationConfig = ScriptableObject.CreateInstance<StageGenerationConfig>();
        }
    }
}
