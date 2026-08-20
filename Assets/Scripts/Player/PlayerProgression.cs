using System.Collections.Generic;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    private const int BaseExperienceRequirement = 30;
    private const int ExperienceIncreasePerLevel = 20;

    private readonly List<ShopUpgradeType> upgradePool = new List<ShopUpgradeType>
    {
        ShopUpgradeType.Heal,
        ShopUpgradeType.MaxHealth,
        ShopUpgradeType.MoveSpeed,
        ShopUpgradeType.WeaponDamage,
        ShopUpgradeType.AttackSpeed,
        ShopUpgradeType.AttackRange
    };

    private int pendingLevelUps;
    private bool levelUpPanelOpen;

    private void Start()
    {
        RefreshHud();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0 || RunManager.Instance == null || !RunManager.Instance.IsRunReady)
        {
            return;
        }

        RunData data = RunManager.Instance.Data;
        data.currentExperience += amount;
        while (data.currentExperience >= data.experienceToNextLevel)
        {
            data.currentExperience -= data.experienceToNextLevel;
            data.playerLevel++;
            data.experienceToNextLevel = CalculateRequirement(data.playerLevel);
            pendingLevelUps++;
        }

        RunManager.Instance.SaveRun();
        RefreshHud();
        if (pendingLevelUps > 0 && !levelUpPanelOpen)
        {
            ShowNextLevelUp();
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0 || RunManager.Instance == null || !RunManager.Instance.IsRunReady)
        {
            return;
        }

        RunManager.Instance.Data.coins += amount;
        RunManager.Instance.SaveRun();
        RefreshHud();
    }

    public bool TrySpendCoins(int amount)
    {
        if (RunManager.Instance == null || amount < 0 || RunManager.Instance.Data.coins < amount)
        {
            return false;
        }

        RunManager.Instance.Data.coins -= amount;
        RunManager.Instance.SaveRun();
        RefreshHud();
        return true;
    }

    /// <summary>
    /// Grants one upgrade selection without changing level or experience.
    /// Used by the repeatable Prismatic Upgrade Potion in the Shop.
    /// </summary>
    public void GrantBonusUpgradeChoice()
    {
        if (RunManager.Instance == null || !RunManager.Instance.IsRunReady)
        {
            return;
        }

        pendingLevelUps++;
        if (!levelUpPanelOpen)
        {
            ShowNextLevelUp();
        }
    }

    public void SelectUpgrade(ShopUpgradeType upgrade)
    {
        if (!levelUpPanelOpen)
        {
            return;
        }

        FindAnyObjectByType<StageManager>()?.ApplyLevelUpgrade(upgrade);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        if (pendingLevelUps > 0)
        {
            ShowNextLevelUp();
            return;
        }

        levelUpPanelOpen = false;
        Module1Ui.EnsureForScene().HideLevelUp();
        GamePauseManager.Instance?.Resume("LevelUp");
        RefreshHud();
    }

    public void RefreshHud()
    {
        if (RunManager.Instance == null)
        {
            return;
        }

        RunData data = RunManager.Instance.Data;
        WeaponDefinition weapon = WeaponCatalog.Get(data.equippedWeapon);
        Module1Ui.EnsureForScene().UpdateProgressHud(
            data.playerLevel,
            data.currentExperience,
            data.experienceToNextLevel,
            data.coins,
            weapon.DisplayName);
    }

    private void ShowNextLevelUp()
    {
        levelUpPanelOpen = true;
        GamePauseManager.EnsureForScene().Pause("LevelUp");
        Module1Ui.EnsureForScene().ShowLevelUp(GetRandomUpgradeChoices());
    }

    private List<ShopUpgradeType> GetRandomUpgradeChoices()
    {
        List<ShopUpgradeType> available = new List<ShopUpgradeType>(upgradePool);
        List<ShopUpgradeType> choices = new List<ShopUpgradeType>();
        while (choices.Count < 3 && available.Count > 0)
        {
            int index = Random.Range(0, available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }

        return choices;
    }

    private static int CalculateRequirement(int level)
    {
        return BaseExperienceRequirement + Mathf.Max(0, level - 1) * ExperienceIncreasePerLevel;
    }
}
