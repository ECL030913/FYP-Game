using UnityEngine;

/// <summary>
/// Run-scoped difficulty. Normal deliberately remains zero so saves created
/// before difficulty selection existed migrate to the standard experience.
/// </summary>
public enum GameDifficulty
{
    Normal = 0,
    Easy = 1,
    Hard = 2
}

public readonly struct DifficultyDefinition
{
    public DifficultyDefinition(
        GameDifficulty difficulty,
        string displayName,
        string playerLabel,
        string description,
        float healthMultiplier,
        float damageMultiplier,
        float movementSpeedMultiplier,
        float experienceMultiplier,
        float coinMultiplier)
    {
        Difficulty = difficulty;
        DisplayName = displayName;
        PlayerLabel = playerLabel;
        Description = description;
        HealthMultiplier = healthMultiplier;
        DamageMultiplier = damageMultiplier;
        MovementSpeedMultiplier = movementSpeedMultiplier;
        ExperienceMultiplier = experienceMultiplier;
        CoinMultiplier = coinMultiplier;
    }

    public GameDifficulty Difficulty { get; }
    public string DisplayName { get; }
    public string PlayerLabel { get; }
    public string Description { get; }
    public float HealthMultiplier { get; }
    public float DamageMultiplier { get; }
    public float MovementSpeedMultiplier { get; }
    public float ExperienceMultiplier { get; }
    public float CoinMultiplier { get; }

    public string GetMultiplierSummary()
    {
        return $"Enemy HP {ToPercent(HealthMultiplier)}  •  Damage {ToPercent(DamageMultiplier)}  •  Speed {ToPercent(MovementSpeedMultiplier)}\n"
            + $"Experience {ToPercent(ExperienceMultiplier)}  •  Coins {ToPercent(CoinMultiplier)}";
    }

    private static string ToPercent(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }
}

public static class DifficultyCatalog
{
    public static readonly GameDifficulty[] DisplayOrder =
    {
        GameDifficulty.Easy,
        GameDifficulty.Normal,
        GameDifficulty.Hard
    };

    public static DifficultyDefinition Get(GameDifficulty difficulty)
    {
        return difficulty switch
        {
            GameDifficulty.Easy => new DifficultyDefinition(
                GameDifficulty.Easy,
                "EASY",
                "NEWCOMER",
                "A forgiving mode for first-time players. Enemies are weaker and progression is faster.",
                0.8f,
                0.75f,
                0.9f,
                1.25f,
                1.25f),
            GameDifficulty.Hard => new DifficultyDefinition(
                GameDifficulty.Hard,
                "HARD",
                "PRO",
                "A demanding mode with stronger enemies and tighter progression rewards.",
                1.3f,
                1.25f,
                1.1f,
                0.85f,
                0.8f),
            _ => new DifficultyDefinition(
                GameDifficulty.Normal,
                "NORMAL",
                "STANDARD",
                "The intended balanced experience with standard enemies and rewards.",
                1f,
                1f,
                1f,
                1f,
                1f)
        };
    }
}
