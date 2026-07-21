using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds the Module 1 controller components at runtime. This avoids replacing
/// the teammate's existing scene objects and makes the integration reversible.
/// </summary>
public static class Module1Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallModuleOne()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        InstallForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForScene(Scene scene)
    {
        if (scene.name != "Game")
        {
            return;
        }

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            return;
        }

        RunManager.EnsureInstance();
        Module1Ui.EnsureForScene();

        if (spawner.GetComponent<StageManager>() == null)
        {
            spawner.gameObject.AddComponent<StageManager>();
        }
    }
}
