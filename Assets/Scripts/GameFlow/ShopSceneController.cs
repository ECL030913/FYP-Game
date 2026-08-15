using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the walkable shop from scene-owned anchors. Portals occupy the upper
/// arena while displays occupy the lower arena, giving both trigger types a
/// large spatial safety margin in addition to centralized input arbitration.
/// </summary>
public class ShopSceneController : MonoBehaviour
{
    private const int HealthPotionPrice = 12;
    private const float HealthPotionHealPercent = 0.35f;

    private static readonly Vector2[] FallbackPositions =
    {
        new Vector2(-7.5f, -4.5f),
        new Vector2(-2.5f, -4.5f),
        new Vector2(2.5f, -4.5f),
        new Vector2(7.5f, -4.5f),
        new Vector2(0f, -7.4f)
    };

    private readonly List<ShopPedestal> pedestals = new List<ShopPedestal>();
    private bool initialized;

    public static ShopSceneController EnsureForScene()
    {
        ShopSceneController existing = FindAnyObjectByType<ShopSceneController>();
        if (existing != null)
        {
            return existing;
        }

        return new GameObject("Shop Scene Controller").AddComponent<ShopSceneController>();
    }

    public void Initialize(StageManager stageManager, GameplaySceneDefinition definition)
    {
        if (initialized)
        {
            RefreshAll();
            return;
        }

        initialized = true;
        IReadOnlyList<Transform> anchors = definition != null
            ? definition.ShopPedestalAnchors
            : null;

        IReadOnlyList<WeaponType> weaponTypes = WeaponCatalog.AllTypes;
        for (int i = 0; i < weaponTypes.Count; i++)
        {
            pedestals.Add(ShopPedestal.CreateWeapon(
                transform,
                GetPosition(anchors, i),
                weaponTypes[i],
                stageManager));
        }

        pedestals.Add(ShopPedestal.CreateHealthPotion(
            transform,
            GetPosition(anchors, 4),
            HealthPotionPrice,
            HealthPotionHealPercent,
            stageManager));
        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (ShopPedestal pedestal in pedestals)
        {
            pedestal?.RefreshAvailability();
        }
    }

    private static Vector2 GetPosition(IReadOnlyList<Transform> anchors, int index)
    {
        if (anchors != null && index < anchors.Count && anchors[index] != null)
        {
            return anchors[index].position;
        }

        return FallbackPositions[Mathf.Clamp(index, 0, FallbackPositions.Length - 1)];
    }
}
