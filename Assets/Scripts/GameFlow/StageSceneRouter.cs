using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central mapping between logical stage types and their dedicated Unity scenes.
/// RunData is saved before this router is called, so a newly loaded scene can
/// reconstruct the player and stage from persistent state.
/// </summary>
public static class StageSceneRouter
{
    public const string MenuSceneName = "Menu";
    public const string CombatSceneName = "Combat";
    public const string EliteSceneName = "Elite";
    public const string BossSceneName = "Boss";
    public const string ShopSceneName = "Shop";

    public static string GetSceneName(StageType stageType)
    {
        return stageType switch
        {
            StageType.Combat => CombatSceneName,
            StageType.Elite => EliteSceneName,
            StageType.Shop => ShopSceneName,
            StageType.Boss => BossSceneName,
            _ => CombatSceneName
        };
    }

    public static bool IsGameplayScene(Scene scene)
    {
        return scene.name == CombatSceneName
            || scene.name == EliteSceneName
            || scene.name == BossSceneName
            || scene.name == ShopSceneName;
    }

    public static bool CanLoadStage(StageType stageType)
    {
        return Application.CanStreamedLevelBeLoaded(GetSceneName(stageType));
    }

    public static bool CanLoadMenu()
    {
        return Application.CanStreamedLevelBeLoaded(MenuSceneName);
    }

    public static AsyncOperation LoadStageAsync(StageType stageType)
    {
        string sceneName = GetSceneName(stageType);
        return LoadSceneAsyncIfAvailable(sceneName);
    }

    public static AsyncOperation LoadMenuAsync()
    {
        return LoadSceneAsyncIfAvailable(MenuSceneName);
    }

    private static AsyncOperation LoadSceneAsyncIfAvailable(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' is not available. Check the enabled Build Settings scenes.");
            return null;
        }

        Time.timeScale = 1f;
        try
        {
            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"Could not load scene '{sceneName}': {exception.Message}");
            return null;
        }
    }
}
