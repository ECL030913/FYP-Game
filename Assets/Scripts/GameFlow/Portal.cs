using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A portal is rendered as three independent layers: a completely static stone
/// frame, a moving energy field, and an icon. Only the latter two scale during
/// reveal, so the frame can never appear to grow or wobble.
/// </summary>
public class Portal : MonoBehaviour, IPlayerInteractable
{
    private const float PortalPixelsPerUnit = 256f;
    private const float RevealDuration = 0.7f;
    private const float IdleFrameDuration = 0.14f;

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
    private static Sprite placeholderSprite;
    private static Sprite confettiSprite;

    private StageType stageType;
    private StageManager stageManager;
    private bool selected = true;
    private CircleCollider2D interactionTrigger;
    private PlayerInteractionController registeredController;
    private SpriteRenderer frameRenderer;
    private SpriteRenderer backdropRenderer;
    private SpriteRenderer energyRenderer;
    private SpriteRenderer iconRenderer;
    private Transform energyTransform;
    private Transform iconTransform;
    private GameObject labelObject;
    private TextMesh label;
    private Sprite[] energyFrames;
    private Sprite[] iconFrames;

    public Transform InteractionTransform => transform;
    public bool IsInteractionAvailable => isActiveAndEnabled
        && !selected
        && interactionTrigger != null
        && interactionTrigger.enabled;

    public static Portal CreateHidden(StageType stageType, Vector2 position, StageManager stageManager)
    {
        GameObject portalObject = new GameObject("Hidden Portal");
        portalObject.transform.position = position;

        SpriteRenderer renderer = portalObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Portals/Portal_Empty_V1");
        renderer.color = Color.white;
        renderer.sortingOrder = 11;

        // A solid collider prevents the player from standing inside the stone
        // structure. Interaction uses a separate, larger trigger.
        BoxCollider2D solidCollider = portalObject.AddComponent<BoxCollider2D>();
        solidCollider.isTrigger = false;
        solidCollider.size = new Vector2(1.55f, 2.05f);
        solidCollider.offset = new Vector2(0f, -0.04f);

        CircleCollider2D trigger = portalObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.45f;
        trigger.enabled = false;

        Portal portal = portalObject.AddComponent<Portal>();
        portal.stageType = stageType;
        portal.stageManager = stageManager;
        portal.interactionTrigger = trigger;
        portal.frameRenderer = renderer;
        portal.CreateVisualLayers();
        portal.CreateLabel();
        portal.SetLabelVisible(false);
        return portal;
    }

    public void Reveal()
    {
        StopAllCoroutines();
        gameObject.name = $"{stageType} Portal";
        selected = true;
        registeredController?.Unregister(this);
        registeredController = null;
        interactionTrigger.enabled = false;
        SetLabelVisible(false);

        frameRenderer.sprite = LoadSprite($"Portals/Layers/{stageType}/Portal_{stageType}_Frame");
        backdropRenderer.sprite = LoadSprite("Portals/Layers/Portal_Doorway_Backdrop");
        energyFrames = LoadFrames(
            $"Portals/Layers/{stageType}/Portal_{stageType}_Energy_",
            4);
        iconFrames = LoadFrames(
            $"Portals/Layers/{stageType}/Portal_{stageType}_Icon_",
            stageType == StageType.Boss ? 6 : 1);

        backdropRenderer.enabled = true;
        energyRenderer.enabled = true;
        iconRenderer.enabled = true;
        energyRenderer.sprite = energyFrames[0];
        iconRenderer.sprite = iconFrames[0];
        energyTransform.localScale = Vector3.one * 0.04f;
        iconTransform.localScale = Vector3.one * 0.04f;
        StartCoroutine(PlayRevealAndIdle());
    }

    public void SetInteractable(bool interactable)
    {
        selected = !interactable;
        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = interactable;
        }

        if (!interactable)
        {
            registeredController?.Unregister(this);
            registeredController = null;
            SetPortalLabel(false);
            FindAnyObjectByType<Module1Ui>()?.HidePortalDetails();
        }
    }

    public void SetInteractionFocus(bool focused)
    {
        SetPortalLabel(focused);
        if (focused)
        {
            Module1Ui.EnsureForScene().ShowPortalDetails(stageType);
        }
        else
        {
            FindAnyObjectByType<Module1Ui>()?.HidePortalDetails();
        }
    }

    public void Interact(PlayerInteractionController controller)
    {
        if (!IsInteractionAvailable)
        {
            return;
        }

        selected = true;
        interactionTrigger.enabled = false;
        controller?.Unregister(this);
        registeredController = null;
        stageManager.SelectPortal(stageType);
    }

    private IEnumerator PlayRevealAndIdle()
    {
        float elapsed = 0f;
        while (elapsed < RevealDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / RevealDuration);
            float scale = Mathf.SmoothStep(0.04f, 1f, progress);
            energyTransform.localScale = Vector3.one * scale;
            iconTransform.localScale = Vector3.one * scale;
            yield return null;
        }

        energyTransform.localScale = Vector3.one;
        iconTransform.localScale = Vector3.one;
        selected = false;
        interactionTrigger.enabled = true;
        SetLabelVisible(true);
        SetPortalLabel(false);

        if (stageType == StageType.End)
        {
            SpawnConfettiBurst();
        }

        int energyIndex = 0;
        int iconIndex = 0;
        int idleTicks = 0;
        while (true)
        {
            yield return new WaitForSeconds(IdleFrameDuration);
            energyIndex = (energyIndex + 1) % energyFrames.Length;
            energyRenderer.sprite = energyFrames[energyIndex];

            if (stageType == StageType.Boss && iconFrames.Length > 1)
            {
                iconIndex = (iconIndex + 1) % iconFrames.Length;
                iconRenderer.sprite = iconFrames[iconIndex];
            }

            idleTicks++;
            if (stageType == StageType.End && idleTicks % 9 == 0)
            {
                SpawnConfettiBurst();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (selected || !other.CompareTag("Player"))
        {
            return;
        }

        registeredController = other.GetComponent<PlayerInteractionController>();
        registeredController?.Register(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerInteractionController controller = other.GetComponent<PlayerInteractionController>();
        controller?.Unregister(this);
        if (registeredController == controller)
        {
            registeredController = null;
        }
    }

    private void OnDisable()
    {
        registeredController?.Unregister(this);
        registeredController = null;
    }

    private void CreateVisualLayers()
    {
        backdropRenderer = CreateLayer("Doorway Backdrop", 8, out _);
        energyRenderer = CreateLayer("Energy", 9, out energyTransform);
        iconRenderer = CreateLayer("Icon", 10, out iconTransform);
        backdropRenderer.enabled = false;
        energyRenderer.enabled = false;
        iconRenderer.enabled = false;
    }

    private SpriteRenderer CreateLayer(string objectName, int sortingOrder, out Transform layerTransform)
    {
        GameObject layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(transform, false);
        layerObject.transform.localPosition = Vector3.zero;
        layerTransform = layerObject.transform;

        SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.color = Color.white;
        return renderer;
    }

    private void CreateLabel()
    {
        labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.LowerCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.075f;
        label.fontSize = 42;
        label.lineSpacing = 0.88f;
        label.richText = false;
        label.color = PixelUiTheme.GetStageAccent(stageType);

        Font portalFont = PixelUiTheme.DisplayFont;
        if (portalFont != null)
        {
            label.font = portalFont;
        }

        MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = 12;
            if (portalFont != null)
            {
                labelRenderer.sharedMaterial = portalFont.material;
            }
        }
    }

    private void SetPortalLabel(bool showInteraction)
    {
        if (label == null)
        {
            return;
        }

        string displayName = stageType == StageType.End ? "Finish" : stageType.ToString();
        label.text = showInteraction
            ? $"{displayName}\nPress E to enter"
            : displayName;
    }

    private void SetLabelVisible(bool visible)
    {
        if (labelObject != null)
        {
            labelObject.SetActive(visible);
        }
    }

    private void SpawnConfettiBurst()
    {
        Color[] colours =
        {
            new Color(1f, 0.25f, 0.22f),
            new Color(0.2f, 0.8f, 1f),
            new Color(0.25f, 1f, 0.45f),
            new Color(1f, 0.85f, 0.15f),
            new Color(1f, 0.35f, 0.85f)
        };

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 localStart = new Vector2(side * 0.55f, Random.Range(-0.35f, 0.35f));
                Vector2 velocity = new Vector2(side * Random.Range(0.45f, 0.9f), Random.Range(0.5f, 1.15f));
                PortalConfettiParticle.Create(
                    transform,
                    localStart,
                    velocity,
                    colours[Random.Range(0, colours.Length)],
                    GetConfettiSprite());
            }
        }
    }

    private static Sprite[] LoadFrames(string prefix, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = LoadSprite($"{prefix}{i:00}");
        }

        return frames;
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        if (SpriteCache.TryGetValue(resourcePath, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError($"Portal texture could not be loaded from Resources/{resourcePath}.");
            return GetPlaceholderSprite();
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PortalPixelsPerUnit);
        SpriteCache[resourcePath] = sprite;
        return sprite;
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

    private static Sprite GetConfettiSprite()
    {
        if (confettiSprite == null)
        {
            confettiSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        return confettiSprite;
    }
}

public class PortalConfettiParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;

    public static void Create(
        Transform parent,
        Vector2 localPosition,
        Vector2 velocity,
        Color colour,
        Sprite sprite)
    {
        GameObject particleObject = new GameObject("Portal Confetti");
        particleObject.transform.SetParent(parent, false);
        particleObject.transform.localPosition = localPosition;
        particleObject.transform.localScale = new Vector3(0.055f, 0.12f, 1f);

        SpriteRenderer renderer = particleObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = colour;
        renderer.sortingOrder = 12;

        PortalConfettiParticle particle = particleObject.AddComponent<PortalConfettiParticle>();
        particle.velocity = velocity;
        particle.lifetime = 1.15f;
    }

    private void Update()
    {
        transform.localPosition += (Vector3)(velocity * Time.deltaTime);
        velocity += Vector2.down * (1.8f * Time.deltaTime);
        transform.Rotate(0f, 0f, 280f * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
