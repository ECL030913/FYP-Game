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
        if (!StageSceneRouter.IsGameplayScene(scene))
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
        GamePauseManager.EnsureForScene();

        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
        if (player != null)
        {
            if (player.GetComponent<PlayerProgression>() == null)
            {
                player.gameObject.AddComponent<PlayerProgression>();
            }

            if (player.GetComponent<PlayerWeaponSystem>() == null)
            {
                player.gameObject.AddComponent<PlayerWeaponSystem>();
            }

            if (player.GetComponent<PlayerInteractionController>() == null)
            {
                player.gameObject.AddComponent<PlayerInteractionController>();
            }
        }

        if (Object.FindAnyObjectByType<StageManager>() == null)
        {
            spawner.gameObject.AddComponent<StageManager>();
        }
    }
}
