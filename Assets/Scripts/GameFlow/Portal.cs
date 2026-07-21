using UnityEngine;

/// <summary>
/// Runtime-built placeholder portal. Its color and label make the procedural
/// choices understandable before final portal artwork is supplied.
/// </summary>
public class Portal : MonoBehaviour
{
    private static Sprite placeholderSprite;

    private StageType stageType;
    private StageManager stageManager;
    private bool selected;
    private CircleCollider2D triggerCollider;

    public static Portal Create(StageType stageType, Vector2 position, StageManager stageManager)
    {
        GameObject portalObject = new GameObject($"{stageType} Portal");
        portalObject.transform.position = position;

        SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPlaceholderSprite();
        renderer.color = GetPortalColor(stageType);
        renderer.sortingOrder = 10;
        portalObject.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

        CircleCollider2D trigger = portalObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;

        Portal portal = portalObject.AddComponent<Portal>();
        portal.stageType = stageType;
        portal.stageManager = stageManager;
        portal.triggerCollider = trigger;
        portal.CreateLabel();
        return portal;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (selected || !other.CompareTag("Player"))
        {
            return;
        }

        selected = true;
        stageManager.SelectPortal(stageType);
    }

    public void SetInteractable(bool interactable)
    {
        selected = !interactable;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = interactable;
        }
    }

    private void CreateLabel()
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = stageType.ToString();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.18f;
        label.fontSize = 48;
        label.color = Color.white;

        MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 11;
        }
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

    private static Color GetPortalColor(StageType stageType)
    {
        return stageType switch
        {
            StageType.Combat => new Color(0.25f, 0.65f, 1f, 0.95f),
            StageType.Elite => new Color(0.95f, 0.25f, 0.35f, 0.95f),
            StageType.Shop => new Color(0.3f, 0.9f, 0.45f, 0.95f),
            _ => Color.white
        };
    }
}
