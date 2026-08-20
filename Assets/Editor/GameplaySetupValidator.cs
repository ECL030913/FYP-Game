#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static integrity checks for the generated multi-scene gameplay setup. This
/// intentionally does not enter Play Mode; runtime behaviour remains part of
/// the project's manual Unity verification pass.
/// </summary>
public static class GameplaySetupValidator
{
    private const string CorePrefabPath = "Assets/Prefab/Gameplay/GameplayCore.prefab";

    private static readonly (string path, StageType stageType)[] GameplayScenes =
    {
        ("Assets/Scenes/Combat.unity", StageType.Combat),
        ("Assets/Scenes/Elite.unity", StageType.Elite),
        ("Assets/Scenes/Boss.unity", StageType.Boss),
        ("Assets/Scenes/Shop.unity", StageType.Shop)
    };

    [MenuItem("Tools/FYP/Validate Gameplay Setup")]
    public static void ValidateGameplaySetup()
    {
        List<string> errors = new List<string>();
        ValidateCorePrefab(errors);
        ValidateEnemyPrefabs(errors);
        ValidateBuildSettings(errors);
        ValidateUiAssets(errors);

        foreach ((string path, StageType stageType) in GameplayScenes)
        {
            ValidateScene(path, stageType, errors);
        }

        if (errors.Count > 0)
        {
            foreach (string error in errors)
            {
                Debug.LogError(error);
            }

            throw new BuildFailedException(
                $"Gameplay setup validation failed with {errors.Count} error(s).");
        }

        Debug.Log(
            "Gameplay setup validation passed: shared core, four stage scenes, "
            + "scene anchors, enemy prefab composition, and build lists are valid.");
    }

    private static void ValidateCorePrefab(ICollection<string> errors)
    {
        GameObject core = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
        if (core == null)
        {
            errors.Add($"Missing shared gameplay prefab: {CorePrefabPath}");
            return;
        }

        RequireExactlyOne<StageManager>(core, errors);
        RequireExactlyOne<PlayerStats>(core, errors);
        RequireExactlyOne<PlayerInteractionController>(core, errors);
        RequireExactlyOne<EnemySpawner>(core, errors);
        RequireExactlyOne<ObjectPoolManager>(core, errors);
        RequireExactlyOne<MapBoundary>(core, errors);
        RequireExactlyOne<Camera>(core, errors);
        CheckMissingScripts(core, CorePrefabPath, errors);
    }

    private static void ValidateEnemyPrefabs(ICollection<string> errors)
    {
        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets" });
        foreach (string prefabGuid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                errors.Add($"Missing enemy prefab: {path}");
                continue;
            }

            // Other prefabs do not need an FSM. Every actual enemy, however,
            // must declare its composition rather
            // than relying on EnemyStats to add behaviour at runtime.
            if (prefab.GetComponent<EnemyStats>() == null)
            {
                continue;
            }

            if (prefab.GetComponent<EnemyAI>() == null)
            {
                errors.Add($"{path} must contain EnemyAI as prefab composition.");
            }

            if (prefab.GetComponent<EnemyMovement>() == null)
            {
                errors.Add($"{path} must contain an IEnemyMotor implementation.");
            }

            CheckMissingScripts(prefab, path, errors);
        }
    }

    private static void ValidateBuildSettings(ICollection<string> errors)
    {
        string[] expected =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/Combat.unity",
            "Assets/Scenes/Elite.unity",
            "Assets/Scenes/Boss.unity",
            "Assets/Scenes/Shop.unity"
        };
        string[] actual = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (!actual.SequenceEqual(expected))
        {
            errors.Add(
                "Enabled Build Settings scenes must be Menu, Combat, Elite, Boss, Shop in that order. "
                + $"Actual: {string.Join(", ", actual)}");
        }

        Object profile = AssetDatabase.LoadMainAssetAtPath(
            "Assets/Settings/Build Profiles/Windows.asset");
        if (profile == null)
        {
            return;
        }

        SerializedObject serializedProfile = new SerializedObject(profile);
        SerializedProperty overrideProperty = serializedProfile.FindProperty(
            "m_OverrideGlobalSceneList");
        if (overrideProperty != null && overrideProperty.boolValue)
        {
            errors.Add("Windows Build Profile must inherit the validated global scene list.");
        }
    }

    private static void ValidateUiAssets(ICollection<string> errors)
    {
        Font displayFont = Resources.Load<Font>("Fonts/Silkscreen-Regular");
        if (displayFont == null)
        {
            errors.Add(
                "The Silkscreen display font must exist at Resources/Fonts/Silkscreen-Regular.");
        }

        Font bodyFont = Resources.Load<Font>("Fonts/PixelifySans");
        if (bodyFont == null)
        {
            errors.Add(
                "The Pixelify Sans body font must exist at Resources/Fonts/PixelifySans.");
        }

        if (Resources.Load<Texture2D>("Shop/UpgradePotion") == null)
        {
            errors.Add(
                "The repeatable upgrade potion art must exist at Resources/Shop/UpgradePotion.");
        }
    }

    private static void ValidateScene(
        string path,
        StageType expectedStageType,
        ICollection<string> errors)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            errors.Add($"Missing gameplay scene: {path}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        GameObject core = roots.FirstOrDefault(root => root.name == "GameplayCore");
        GameObject environment = roots.FirstOrDefault(root => root.name == "EnvironmentRoot");

        if (core == null)
        {
            errors.Add($"{path} is missing GameplayCore.");
        }
        else
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(core);
            if (prefabPath != CorePrefabPath)
            {
                errors.Add($"{path} GameplayCore is not linked to {CorePrefabPath}.");
            }

            CheckMissingScripts(core, path, errors);
        }

        if (environment == null)
        {
            errors.Add($"{path} is missing EnvironmentRoot.");
            return;
        }

        GameplaySceneDefinition definition = environment.GetComponent<GameplaySceneDefinition>();
        if (definition == null)
        {
            errors.Add($"{path} is missing GameplaySceneDefinition.");
            return;
        }

        if (definition.StageType != expectedStageType)
        {
            errors.Add(
                $"{path} declares {definition.StageType}, expected {expectedStageType}.");
        }

        if (expectedStageType == StageType.Combat)
        {
            if (environment.transform.Find("BackgroundGrid") == null)
            {
                errors.Add("Combat scene must retain the existing grass tilemap.");
            }
        }
        else if (environment.transform.Find("Themed Floor") == null)
        {
            errors.Add($"{path} is missing its themed floor.");
        }

        if (expectedStageType == StageType.Shop)
        {
            ValidateShopAnchors(definition, errors);
        }

        CheckMissingScripts(environment, path, errors);
    }

    private static void ValidateShopAnchors(
        GameplaySceneDefinition definition,
        ICollection<string> errors)
    {
        IReadOnlyList<Transform> pedestals = definition.ShopPedestalAnchors;
        if (pedestals == null || pedestals.Count != 6)
        {
            errors.Add(
                "Shop scene must define four weapon pedestals, one health potion pedestal, "
                + "and one repeatable upgrade potion pedestal.");
            return;
        }

        Vector2[] portals =
        {
            definition.GetPortalPosition(0, 1),
            definition.GetPortalPosition(0, 2),
            definition.GetPortalPosition(1, 2)
        };

        foreach (Transform pedestal in pedestals)
        {
            if (pedestal == null)
            {
                errors.Add("Shop scene contains a missing pedestal anchor.");
                continue;
            }

            foreach (Vector2 portal in portals)
            {
                if (Vector2.Distance(pedestal.position, portal) < 4f)
                {
                    errors.Add(
                        $"Shop pedestal '{pedestal.name}' is less than 4 units from a portal slot.");
                }
            }
        }
    }

    private static void RequireExactlyOne<T>(
        GameObject root,
        ICollection<string> errors) where T : Component
    {
        int count = root.GetComponentsInChildren<T>(true).Length;
        if (count != 1)
        {
            errors.Add(
                $"{CorePrefabPath} must contain exactly one {typeof(T).Name}; found {count}.");
        }
    }

    private static void CheckMissingScripts(
        GameObject root,
        string context,
        ICollection<string> errors)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                transform.gameObject);
            if (count > 0)
            {
                errors.Add(
                    $"{context} has {count} missing script(s) on '{transform.name}'.");
            }
        }
    }
}
#endif
