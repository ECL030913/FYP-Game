using UnityEngine;

[CreateAssetMenu(fileName = "RunData", menuName = "ScriptableObjects/Run Data")]
public class RunData : ScriptableObject
{
    [Header("Progress")]
    public int currentStageIndex = 1;
    public int currentRoundIndex = 1;
    public StageType currentStageType = StageType.Combat;
    public int eliteStagesCompleted;
    public int nonEliteStagesSinceLastElite;

    [Header("Progression")]
    public int playerLevel = 1;
    public int currentExperience;
    public int experienceToNextLevel = 30;
    public int coins;
    public WeaponType equippedWeapon = WeaponType.RangedPierce;

    [Header("Player State")]
    public float savedPlayerHealth;
    public bool isNewRun;
    public float maxHealthBonus;
    public float moveSpeedBonus;
    public float weaponDamageMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public float attackRangeMultiplier = 1f;

    public void ResetForNewRun(float startingHealth)
    {
        currentStageIndex = 1;
        currentRoundIndex = 1;
        currentStageType = StageType.Combat;
        eliteStagesCompleted = 0;
        nonEliteStagesSinceLastElite = 0;
        playerLevel = 1;
        currentExperience = 0;
        experienceToNextLevel = 30;
        coins = 0;
        equippedWeapon = WeaponType.RangedPierce;
        savedPlayerHealth = startingHealth;
        isNewRun = true;
        maxHealthBonus = 0f;
        moveSpeedBonus = 0f;
        weaponDamageMultiplier = 1f;
        cooldownMultiplier = 1f;
        attackRangeMultiplier = 1f;
    }
}
