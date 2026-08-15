using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Keeps the current run alive across stage transitions and mirrors it to disk so a
/// player can continue a run after restarting the application.
/// </summary>
public class RunManager : MonoBehaviour
{
    [Serializable]
    private class RunSaveData
    {
        public int currentStageIndex;
        public int currentRoundIndex;
        public int currentStageType;
        public int eliteStagesCompleted;
        public int nonEliteStagesSinceLastElite;
        public int playerLevel;
        public int currentExperience;
        public int experienceToNextLevel;
        public int coins;
        public int equippedWeapon;
        public bool shopWeaponPurchased;
        public bool shopPotionPurchased;
        public float savedPlayerHealth;
        public float maxHealthBonus;
        public float moveSpeedBonus;
        public float weaponDamageMultiplier;
        public float cooldownMultiplier;
        public float attackRangeMultiplier;
    }

    public static RunManager Instance { get; private set; }

    public RunData Data { get; private set; }
    public bool HasSavedRun { get; private set; }
    public bool IsRunReady { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "module1_run_save.json");

    public static RunManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("Run Manager");
        return managerObject.AddComponent<RunManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Data = ScriptableObject.CreateInstance<RunData>();
        Data.hideFlags = HideFlags.DontSave;
        LoadSavedRun();
    }

    public void BeginNewRun()
    {
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        float startingHealth = player != null ? player.BaseMaxHealth : 100f;

        Data.ResetForNewRun(startingHealth);
        HasSavedRun = true;
        IsRunReady = true;
        SaveRun();
    }

    public void ContinueRun()
    {
        if (!HasSavedRun)
        {
            BeginNewRun();
            return;
        }

        IsRunReady = true;
    }

    public void SavePlayerState(PlayerStats player)
    {
        if (!IsRunReady || player == null)
        {
            return;
        }

        Data.savedPlayerHealth = player.currentHealth;
        SaveRun();
    }

    /// <summary>
    /// Death or successful completion ends the current run. Removing its save
    /// prevents Continue from reopening a terminal run.
    /// </summary>
    public void EndRun()
    {
        IsRunReady = false;
        HasSavedRun = false;

        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunManager could not clear the completed run: {exception.Message}");
        }
    }

    public void PrepareNextStage(StageType nextStageType, PlayerStats player)
    {
        Data.isNewRun = false;

        if (player != null)
        {
            player.Heal(player.maxHealth * 0.2f);
            Data.savedPlayerHealth = player.currentHealth;
        }

        Data.currentStageIndex++;
        if (nextStageType == StageType.Combat
            || nextStageType == StageType.Elite
            || nextStageType == StageType.Boss)
        {
            Data.currentRoundIndex++;
        }

        Data.currentStageType = nextStageType;
        if (nextStageType == StageType.Shop)
        {
            Data.shopWeaponPurchased = false;
            Data.shopPotionPurchased = false;
        }

        if (nextStageType == StageType.Elite)
        {
            Data.eliteStagesCompleted++;
            Data.nonEliteStagesSinceLastElite = 0;
        }
        else if (Data.eliteStagesCompleted > 0)
        {
            Data.nonEliteStagesSinceLastElite++;
        }

        SaveRun();
    }

    public void SaveRun()
    {
        if (!IsRunReady)
        {
            return;
        }

        RunSaveData save = new RunSaveData
        {
            currentStageIndex = Data.currentStageIndex,
            currentRoundIndex = Data.currentRoundIndex,
            currentStageType = (int)Data.currentStageType,
            eliteStagesCompleted = Data.eliteStagesCompleted,
            nonEliteStagesSinceLastElite = Data.nonEliteStagesSinceLastElite,
            playerLevel = Data.playerLevel,
            currentExperience = Data.currentExperience,
            experienceToNextLevel = Data.experienceToNextLevel,
            coins = Data.coins,
            equippedWeapon = (int)Data.equippedWeapon,
            shopWeaponPurchased = Data.shopWeaponPurchased,
            shopPotionPurchased = Data.shopPotionPurchased,
            savedPlayerHealth = Data.savedPlayerHealth,
            maxHealthBonus = Data.maxHealthBonus,
            moveSpeedBonus = Data.moveSpeedBonus,
            weaponDamageMultiplier = Data.weaponDamageMultiplier,
            cooldownMultiplier = Data.cooldownMultiplier,
            attackRangeMultiplier = Data.attackRangeMultiplier
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            HasSavedRun = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunManager could not save the current run: {exception.Message}");
        }
    }

    private void LoadSavedRun()
    {
        if (!File.Exists(SavePath))
        {
            HasSavedRun = false;
            return;
        }

        try
        {
            RunSaveData save = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(SavePath));
            if (save == null)
            {
                return;
            }

            bool hasProgressionData = save.playerLevel > 0 || save.experienceToNextLevel > 0;

            Data.currentStageType = Enum.IsDefined(typeof(StageType), save.currentStageType)
                ? (StageType)save.currentStageType
                : StageType.Combat;
            Data.currentStageIndex = Mathf.Clamp(save.currentStageIndex, 1, StageManager.MaxStageCount);
            Data.currentRoundIndex = Mathf.Max(1, save.currentRoundIndex);
            if (save.currentRoundIndex <= 0)
            {
                Data.currentRoundIndex = Data.currentStageType == StageType.Shop
                    ? Mathf.Max(1, Data.currentStageIndex - 1)
                    : Data.currentStageIndex;
            }

            Data.eliteStagesCompleted = Mathf.Max(0, save.eliteStagesCompleted);
            if (Data.currentStageType == StageType.Elite && Data.eliteStagesCompleted == 0)
            {
                // Migration safeguard for saves created before this counter
                // existed: an active Elite stage must count as an Elite visit.
                Data.eliteStagesCompleted = 1;
            }
            Data.nonEliteStagesSinceLastElite = Mathf.Max(0, save.nonEliteStagesSinceLastElite);
            Data.playerLevel = Mathf.Max(1, save.playerLevel);
            Data.currentExperience = Mathf.Max(0, save.currentExperience);
            // Recalculate this value so saves from the previous +15 XP curve
            // migrate to the new, slower +20-per-level progression.
            Data.experienceToNextLevel = 30 + Mathf.Max(0, Data.playerLevel - 1) * 20;
            Data.coins = Mathf.Max(0, save.coins);
            Data.equippedWeapon = hasProgressionData
                && Enum.IsDefined(typeof(WeaponType), save.equippedWeapon)
                ? (WeaponType)save.equippedWeapon
                : WeaponType.RangedPierce;
            Data.shopWeaponPurchased = save.shopWeaponPurchased;
            Data.shopPotionPurchased = save.shopPotionPurchased;
            Data.savedPlayerHealth = Mathf.Max(0f, save.savedPlayerHealth);
            Data.maxHealthBonus = Mathf.Clamp(save.maxHealthBonus, 0f, 120f);
            Data.moveSpeedBonus = Mathf.Clamp(save.moveSpeedBonus, 0f, 2.1f);
            Data.weaponDamageMultiplier = Mathf.Clamp(save.weaponDamageMultiplier, 1f, 1.8f);
            Data.cooldownMultiplier = save.cooldownMultiplier > 0f
                ? Mathf.Clamp(save.cooldownMultiplier, 0.6f, 1f)
                : 1f;
            Data.attackRangeMultiplier = Mathf.Clamp(save.attackRangeMultiplier, 1f, 1.75f);
            HasSavedRun = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunManager could not load the saved run: {exception.Message}");
            HasSavedRun = false;
        }
    }
}
