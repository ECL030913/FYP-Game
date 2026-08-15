using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-owned placement data. The reusable GameplayCore contains systems and
/// actors, while each scene owns its environment and interaction anchors.
/// </summary>
public class GameplaySceneDefinition : MonoBehaviour
{
    private const float MinimumShopInteractionSeparation = 4f;

    [SerializeField] private StageType stageType;
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform singlePortalSlot;
    [SerializeField] private Transform leftPortalSlot;
    [SerializeField] private Transform rightPortalSlot;
    [SerializeField] private Transform[] shopPedestalAnchors;

    public StageType StageType => stageType;
    public Vector2 PlayerSpawnPosition => playerSpawn != null
        ? playerSpawn.position
        : Vector2.zero;
    public IReadOnlyList<Transform> ShopPedestalAnchors => shopPedestalAnchors;

    public Vector2 GetPortalPosition(int index, int portalCount)
    {
        if (portalCount <= 1 && singlePortalSlot != null)
        {
            return singlePortalSlot.position;
        }

        Transform slot = index == 0 ? leftPortalSlot : rightPortalSlot;
        if (slot != null)
        {
            return slot.position;
        }

        Vector2 center = MapBoundary.Instance != null
            ? MapBoundary.Instance.GetCenter()
            : Vector2.zero;
        return portalCount <= 1
            ? center + Vector2.up * 3.5f
            : center + new Vector2(index == 0 ? -3.25f : 3.25f, 3.5f);
    }

    public void Configure(
        StageType configuredStageType,
        Transform configuredPlayerSpawn,
        Transform configuredSinglePortalSlot,
        Transform configuredLeftPortalSlot,
        Transform configuredRightPortalSlot,
        Transform[] configuredShopPedestalAnchors)
    {
        stageType = configuredStageType;
        playerSpawn = configuredPlayerSpawn;
        singlePortalSlot = configuredSinglePortalSlot;
        leftPortalSlot = configuredLeftPortalSlot;
        rightPortalSlot = configuredRightPortalSlot;
        shopPedestalAnchors = configuredShopPedestalAnchors;
    }

    private void OnValidate()
    {
        if (stageType != StageType.Shop || shopPedestalAnchors == null)
        {
            return;
        }

        foreach (Transform pedestal in shopPedestalAnchors)
        {
            if (pedestal == null)
            {
                continue;
            }

            ValidatePortalDistance(pedestal, singlePortalSlot);
            ValidatePortalDistance(pedestal, leftPortalSlot);
            ValidatePortalDistance(pedestal, rightPortalSlot);
        }
    }

    private static void ValidatePortalDistance(Transform pedestal, Transform portal)
    {
        if (portal != null
            && Vector2.Distance(pedestal.position, portal.position) < MinimumShopInteractionSeparation)
        {
            Debug.LogWarning(
                $"Shop anchor '{pedestal.name}' is too close to portal slot '{portal.name}'. "
                + $"Keep at least {MinimumShopInteractionSeparation:0.0} world units between them.",
                pedestal);
        }
    }
}
