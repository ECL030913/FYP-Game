#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot, repeatable project setup for the four themed gameplay scenes and
/// their shared GameplayCore prefab. It intentionally uses Unity's serialization
/// APIs rather than editing scene YAML by hand.
/// </summary>
public static class GameplaySceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Game.unity";
    private const string CorePrefabPath = "Assets/Prefab/Gameplay/GameplayCore.prefab";
    private const string CombatScenePath = "Assets/Scenes/Combat.unity";
    private const string EliteScenePath = "Assets/Scenes/Elite.unity";
    private const string BossScenePath = "Assets/Scenes/Boss.unity";
    private const string ShopScenePath = "Assets/Scenes/Shop.unity";

    private static readonly HashSet<string> CoreRootNames = new HashSet<string>
    {
        "Main Camera",
        "EventSystem",
        "Enemy Spawner",
        "ObjectPollManager",
        "ObjectPoolManager",
        "Player",
        "MapBoundary",
        "Canvas"
    };

    [MenuItem("Tools/FYP/Build Gameplay Scenes")]
    public static void BuildGameplayScenes()
    {
        EnsureFolder("Assets/Prefab");
        EnsureFolder("Assets/Prefab/Gameplay");
        ConfigureTexture("Assets/Resources/Environments/EliteFloor.png", false, 64f);
        ConfigureTexture("Assets/Resources/Environments/BossFloor.png", false, 64f);
        ConfigureTexture("Assets/Resources/Environments/ShopFloor.png", false, 64f);
        ConfigureTexture("Assets/Resources/Shop/Pedestal.png", true, 128f);
        ConfigureTexture("Assets/Resources/Shop/HealthPotion.png", true, 128f);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        EnsureEnemyAiOnPrefab("Assets/Prefab/Enemies/Bat.prefab");
        EnsureEnemyAiOnPrefab("Assets/Prefab/Enemies/Black Bat.prefab");

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject coreRoot = BuildCoreFromSource(sourceScene);
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            coreRoot,
            CorePrefabPath,
            InteractionMode.AutomatedAction);

        BuildCombatEnvironment(sourceScene);
        EditorSceneManager.SaveScene(sourceScene, CombatScenePath);

        BuildThemedScene(EliteScenePath, StageType.Elite, "Assets/Resources/Environments/EliteFloor.png");
        BuildThemedScene(BossScenePath, StageType.Boss, "Assets/Resources/Environments/BossFloor.png");
        BuildThemedScene(ShopScenePath, StageType.Shop, "Assets/Resources/Environments/ShopFloor.png");
        DisableBuildProfileSceneOverride();
        // Unity 6 routes EditorBuildSettings.scenes to the active Build Profile
        // while its scene override is enabled. Disable the override first so
        // this writes the global list that the profile now inherits.
        ConfigureBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("GameplaySceneBuilder completed Combat, Elite, Boss, Shop and GameplayCore.");
    }

    private static GameObject BuildCoreFromSource(Scene scene)
    {
        GameObject coreRoot = new GameObject("GameplayCore");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (CoreRootNames.Contains(root.name))
            {
                root.transform.SetParent(coreRoot.transform, true);
            }
        }

        CameraMovement cameraMovement = coreRoot.GetComponentInChildren<CameraMovement>(true);
        if (cameraMovement != null)
        {
            cameraMovement.backgroundTilemap = null;
            EditorUtility.SetDirty(cameraMovement);
        }

        PlayerStats player = coreRoot.GetComponentInChildren<PlayerStats>(true);
        if (player != null)
        {
            EnsureComponent<PlayerProgression>(player.gameObject);
            EnsureComponent<PlayerWeaponSystem>(player.gameObject);
            EnsureComponent<PlayerInteractionController>(player.gameObject);
        }

        EnsureComponent<StageManager>(coreRoot);
        return coreRoot;
    }

    private static void BuildCombatEnvironment(Scene scene)
    {
        GameObject environmentRoot = new GameObject("EnvironmentRoot");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == environmentRoot || root.name == "GameplayCore")
            {
                continue;
            }

            if (root.name == "BackgroundGrid" || root.name == "Global Light 2D")
            {
                root.transform.SetParent(environmentRoot.transform, true);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        ConfigureSceneDefinition(environmentRoot, StageType.Combat);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void BuildThemedScene(string scenePath, StageType stageType, string floorAssetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            if (!AssetDatabase.CopyAsset(CombatScenePath, scenePath))
            {
                throw new IOException($"Could not create scene at {scenePath}.");
            }
        }
        else
        {
            // Overwrite only the scene contents so its existing .meta file and
            // GUID remain stable for build profiles and teammate references.
            File.Copy(
                Path.GetFullPath(CombatScenePath),
                Path.GetFullPath(scenePath),
                true);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceSynchronousImport);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject environmentRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "EnvironmentRoot");
        if (environmentRoot == null)
        {
            throw new MissingReferenceException($"EnvironmentRoot missing from {scenePath}.");
        }

        Transform backgroundGrid = environmentRoot.transform.Find("BackgroundGrid");
        if (backgroundGrid != null)
        {
            Object.DestroyImmediate(backgroundGrid.gameObject);
        }

        Transform existingFloor = environmentRoot.transform.Find("Themed Floor");
        if (existingFloor != null)
        {
            Object.DestroyImmediate(existingFloor.gameObject);
        }

        Sprite floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(floorAssetPath);
        if (floorSprite == null)
        {
            throw new MissingReferenceException($"Floor sprite could not be imported from {floorAssetPath}.");
        }

        GameObject floor = new GameObject("Themed Floor");
        floor.transform.SetParent(environmentRoot.transform, false);
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
        renderer.sprite = floorSprite;
        renderer.sortingOrder = -100;
        float scaleX = 26f / floorSprite.bounds.size.x;
        float scaleY = 19.5f / floorSprite.bounds.size.y;
        floor.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        ConfigureSceneDefinition(environmentRoot, stageType);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureSceneDefinition(GameObject environmentRoot, StageType stageType)
    {
        GameplaySceneDefinition oldDefinition = environmentRoot.GetComponent<GameplaySceneDefinition>();
        if (oldDefinition != null)
        {
            Object.DestroyImmediate(oldDefinition);
        }

        Transform anchorsRoot = environmentRoot.transform.Find("Stage Anchors");
        if (anchorsRoot != null)
        {
            Object.DestroyImmediate(anchorsRoot.gameObject);
        }

        anchorsRoot = new GameObject("Stage Anchors").transform;
        anchorsRoot.SetParent(environmentRoot.transform, false);
        Transform playerSpawn = CreateAnchor(anchorsRoot, "Player Spawn", new Vector2(0f, -0.5f));
        Transform singlePortal = CreateAnchor(anchorsRoot, "Portal Slot Single", new Vector2(0f, 5.1f));
        Transform leftPortal = CreateAnchor(anchorsRoot, "Portal Slot Left", new Vector2(-3.6f, 5.1f));
        Transform rightPortal = CreateAnchor(anchorsRoot, "Portal Slot Right", new Vector2(3.6f, 5.1f));

        Transform shopAnchorsRoot = new GameObject("Shop Pedestal Anchors").transform;
        shopAnchorsRoot.SetParent(anchorsRoot, false);
        Vector2[] positions =
        {
            new Vector2(-7.5f, -4.5f),
            new Vector2(-2.5f, -4.5f),
            new Vector2(2.5f, -4.5f),
            new Vector2(7.5f, -4.5f),
            new Vector2(0f, -7.4f)
        };
        Transform[] shopAnchors = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            shopAnchors[i] = CreateAnchor(
                shopAnchorsRoot,
                i < 4 ? $"Weapon Pedestal {i + 1}" : "Health Potion Pedestal",
                positions[i]);
        }

        GameplaySceneDefinition definition = environmentRoot.AddComponent<GameplaySceneDefinition>();
        definition.Configure(
            stageType,
            playerSpawn,
            singlePortal,
            leftPortal,
            rightPortal,
            shopAnchors);
        EditorUtility.SetDirty(definition);
    }

    private static Transform CreateAnchor(Transform parent, string name, Vector2 position)
    {
        Transform anchor = new GameObject(name).transform;
        anchor.SetParent(parent, false);
        anchor.position = position;
        return anchor;
    }

    private static void EnsureEnemyAiOnPrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponent<EnemyAI>() == null)
            {
                root.AddComponent<EnemyAI>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void ConfigureTexture(string assetPath, bool hasAlpha, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new MissingReferenceException($"TextureImporter missing for {assetPath}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = hasAlpha;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void ConfigureBuildScenes()
    {
        string[] paths =
        {
            "Assets/Scenes/Menu.unity",
            CombatScenePath,
            EliteScenePath,
            BossScenePath,
            ShopScenePath
        };
        EditorBuildSettings.scenes = paths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static void DisableBuildProfileSceneOverride()
    {
        Object profile = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Build Profiles/Windows.asset");
        if (profile == null)
        {
            return;
        }

        SerializedObject serializedProfile = new SerializedObject(profile);
        SerializedProperty overrideProperty = serializedProfile.FindProperty("m_OverrideGlobalSceneList");
        if (overrideProperty != null)
        {
            overrideProperty.boolValue = false;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
