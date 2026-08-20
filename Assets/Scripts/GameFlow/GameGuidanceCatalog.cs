using System.Collections.Generic;

public readonly struct GuidePage
{
    public GuidePage(string title, string body)
    {
        Title = title;
        Body = body;
    }

    public string Title { get; }
    public string Body { get; }
}

/// <summary>
/// Single source of truth for first-run help, the reusable user guide, room
/// objectives and portal descriptions.
/// </summary>
public static class GameGuidanceCatalog
{
    public static IReadOnlyList<GuidePage> GetTutorialPages(
        string nickname,
        GameDifficulty difficulty = GameDifficulty.Normal)
    {
        string playerName = RunManager.NormalizeNickname(nickname);
        DifficultyDefinition difficultyDefinition = DifficultyCatalog.Get(difficulty);
        return new[]
        {
            new GuidePage(
                "WELCOME",
                $"You are {playerName}. Difficulty: {difficultyDefinition.DisplayName} ({difficultyDefinition.PlayerLabel}). "
                + "You are starting in Stage 1: Combat.\n\n"
                + "Clear 10 stages, grow stronger and defeat the Final Boss to complete the run."),
            new GuidePage(
                "CONTROLS",
                "WASD  •  Move\nE  •  Interact with portals and shop displays\nESC  •  Pause and view run statistics\n\n"
                + "Your equipped weapon attacks nearby enemies automatically."),
            new GuidePage(
                "COMBAT & LEVEL UP",
                "Defeat enemies to collect experience and coins. When the XP bar fills, combat pauses and you choose one permanent upgrade.\n\n"
                + "Keep moving to avoid contact damage. Stronger enemies provide greater rewards."),
            new GuidePage(
                "ROOMS & PORTALS",
                "Combat  •  Defeat all enemies\nElite  •  Fight a mini-boss and reinforcements\nShop  •  Buy weapons, healing or an upgrade choice\nBoss  •  Face the Final Boss\n\n"
                + "Portals activate after a room is cleared. Approach one, read its description and press E to enter."),
            new GuidePage(
                "WEAPON ROLES",
                "Rune Knife  •  Safe ranged piercing damage; your starting weapon\n"
                + "Frost Cleaver  •  High-damage melee crowd clearing\n"
                + "Crimson Lance  •  Highest close-range boss damage\n"
                + "Ember Cannon  •  Safer ranged explosions for groups\n\n"
                + "Approach a Shop display to compare its full purpose, trade-off and current upgraded statistics before buying.")
        };
    }

    public static IReadOnlyList<GuidePage> GetUserGuidePages(
        string nickname = "Player",
        GameDifficulty difficulty = GameDifficulty.Normal)
    {
        return GetTutorialPages(nickname, difficulty);
    }

    public static string GetStageObjective(StageType stageType, bool roomCleared)
    {
        if (roomCleared)
        {
            return stageType == StageType.Boss
                ? "FINAL BOSS DEFEATED  •  Enter the End Portal to complete the run"
                : "ROOM CLEARED  •  Approach a portal, read its description and press E";
        }

        return stageType switch
        {
            StageType.Combat => "OBJECTIVE: Defeat all enemies  •  WASD to move  •  Attacks are automatic",
            StageType.Elite => "OBJECTIVE: Defeat the Elite boss and all reinforcements",
            StageType.Shop => "SHOP: Approach a display to inspect it  •  Press E to buy  •  Use a portal to continue",
            StageType.Boss => "FINAL OBJECTIVE: Defeat the Final Boss  •  Its defeat ends the battle",
            StageType.End => "RUN COMPLETE: Enter the End Portal",
            _ => string.Empty
        };
    }

    public static string GetPortalDescription(StageType stageType)
    {
        return stageType switch
        {
            StageType.Combat => "Standard battle room. Defeat every enemy to reveal the next portals.",
            StageType.Elite => "High-risk room with a mini-boss and stronger enemies. Offers greater rewards.",
            StageType.Shop => "Safe room with weapon displays, healing and repeatable upgrade potions. No enemies spawn.",
            StageType.Boss => "Final challenge. Defeat the Final Boss to clear the tenth stage.",
            StageType.End => "The run is complete. Enter to view the victory screen.",
            _ => string.Empty
        };
    }
}
