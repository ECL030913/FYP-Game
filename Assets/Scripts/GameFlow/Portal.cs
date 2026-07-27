using UnityEngine;

/// <summary>
/// A portal slot is created when a stage begins with its destination concealed.
/// Clearing the stage reveals the destination on the same object, ensuring the
/// active portal can never appear at a different position from its empty frame.
/// </summary>
public class Portal : MonoBehaviour
{
    private const float PortalPixelsPerUnit = 256f;

    private static Sprite placeholderSprite;
    private static Sprite emptySprite;
    private static Sprite combatSprite;
    private static Sprite eliteSprite;
    private static Sprite shopSprite;

    private StageType stageType;
    private StageManager stageManager;
    private bool selected = true;
    private bool requiresPlayerExit;
    private CircleCollider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    private GameObject labelObject;
    private TextMesh label;

    public static Portal CreateHidden(StageType stageType, Vector2 position, StageManager stageManager)
    {
        GameObject portalObject = new GameObject("Hidden Portal");
        portalObject.transform.position = position;

        SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetEmptySprite();
        renderer.color = Color.white;
        renderer.sortingOrder = 10;

        CircleCollider2D trigger = portalObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.72f;
        trigger.enabled = false;

        Portal portal = portalObject.AddComponent<Portal>();
        portal.stageType = stageType;
        portal.stageManager = stageManager;
        portal.triggerCollider = trigger;
        portal.spriteRenderer = renderer;
        portal.CreateLabel();
        portal.SetLabelVisible(false);
        return portal;
    }

    /// <summary>
    /// Reveals the destination without moving or replacing the portal object.
    /// </summary>
    public void Reveal()
    {
        gameObject.name = $"{stageType} Portal";
        spriteRenderer.sprite = GetStageSprite(stageType);
        label.text = stageType.ToString();
        SetLabelVisible(true);
        requiresPlayerExit = IsPlayerOverlapping();
        SetInteractable(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (selected || requiresPlayerExit || !other.CompareTag("Player"))
        {
            return;
        }

        selected = true;
        stageManager.SelectPortal(stageType);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (requiresPlayerExit && other.CompareTag("Player"))
        {
            requiresPlayerExit = false;
        }
    }

    public void SetInteractable(bool interactable)
    {
        selected = !interactable;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = interactable;
        }
    }

    private bool IsPlayerOverlapping()
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(
            transform.position,
            triggerCollider.radius);

        foreach (Collider2D overlap in overlaps)
        {
            if (overlap != null && overlap.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void CreateLabel()
    {
        labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        label = labelObject.AddComponent<TextMesh>();
        label.text = stageType.ToString();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.14f;
        label.fontSize = 48;
        label.color = Color.white;

        MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 11;
        }
    }

    private void SetLabelVisible(bool visible)
    {
        if (labelObject != null)
        {
            labelObject.SetActive(visible);
        }
    }

    private static Sprite GetEmptySprite()
    {
        emptySprite ??= LoadPortalSprite("Portals/Portal_Empty_V1");
        return emptySprite != null ? emptySprite : GetPlaceholderSprite();
    }

    private static Sprite GetStageSprite(StageType type)
    {
        switch (type)
        {
            case StageType.Combat:
                combatSprite ??= LoadPortalSprite("Portals/Portal_Combat_V1");
                return combatSprite != null ? combatSprite : GetPlaceholderSprite();
            case StageType.Elite:
                eliteSprite ??= LoadPortalSprite("Portals/Portal_Elite_V1");
                return eliteSprite != null ? eliteSprite : GetPlaceholderSprite();
            case StageType.Shop:
                shopSprite ??= LoadPortalSprite("Portals/Portal_Shop_V1");
                return shopSprite != null ? shopSprite : GetPlaceholderSprite();
            default:
                return GetPlaceholderSprite();
        }
    }

    private static Sprite LoadPortalSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError($"Portal texture could not be loaded from Resources/{resourcePath}.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PortalPixelsPerUnit);
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (placeholderSprite == null)
        {
            placeholderSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        return placeholderSprite;
    }
}
