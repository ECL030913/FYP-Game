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
        public int currentStageType;
        public int eliteStagesCompleted;
        public int nonEliteStagesSinceLastElite;
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

        FindAnyObjectByType<StageManager>()?.BeginCurrentStage();
    }

    public void ContinueRun()
    {
        if (!HasSavedRun)
        {
            BeginNewRun();
            return;
        }

        IsRunReady = true;
        FindAnyObjectByType<StageManager>()?.BeginCurrentStage();
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
    /// A death ends the current run. Removing its save prevents Continue from
    /// reopening a run whose player health is already zero.
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
        if (player != null)
        {
            player.Heal(player.maxHealth * 0.2f);
            Data.savedPlayerHealth = player.currentHealth;
        }

        Data.currentStageIndex++;
        Data.currentStageType = nextStageType;

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
            currentStageType = (int)Data.currentStageType,
            eliteStagesCompleted = Data.eliteStagesCompleted,
            nonEliteStagesSinceLastElite = Data.nonEliteStagesSinceLastElite,
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

            Data.currentStageIndex = Mathf.Max(1, save.currentStageIndex);
            Data.currentStageType = Enum.IsDefined(typeof(StageType), save.currentStageType)
                ? (StageType)save.currentStageType
                : StageType.Combat;
            Data.eliteStagesCompleted = Mathf.Max(0, save.eliteStagesCompleted);
            if (Data.currentStageType == StageType.Elite && Data.eliteStagesCompleted == 0)
            {
                // Migration safeguard for saves created before this counter
                // existed: an active Elite stage must count as an Elite visit.
                Data.eliteStagesCompleted = 1;
            }
            Data.nonEliteStagesSinceLastElite = Mathf.Max(0, save.nonEliteStagesSinceLastElite);
            Data.savedPlayerHealth = Mathf.Max(0f, save.savedPlayerHealth);
            Data.maxHealthBonus = Mathf.Max(0f, save.maxHealthBonus);
            Data.moveSpeedBonus = Mathf.Max(0f, save.moveSpeedBonus);
            Data.weaponDamageMultiplier = Mathf.Max(1f, save.weaponDamageMultiplier);
            Data.cooldownMultiplier = Mathf.Clamp(save.cooldownMultiplier, 0.35f, 1f);
            Data.attackRangeMultiplier = Mathf.Max(1f, save.attackRangeMultiplier);
            HasSavedRun = true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"RunManager could not load the saved run: {exception.Message}");
            HasSavedRun = false;
        }
    }
}
