using System.Collections.Generic;
using UnityEngine;

public enum ShopOfferKind
{
    Weapon,
    HealthPotion,
    UpgradePotion
}

/// <summary>
/// A physical shop display with a solid stone base and a separate proximity
/// trigger. It delegates input to PlayerInteractionController and transactions
/// to StageManager.
/// </summary>
public class ShopPedestal : MonoBehaviour, IPlayerInteractable
{
    private const float ShopPixelsPerUnit = 128f;
    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private StageManager stageManager;
    private ShopOfferKind offerKind;
    private WeaponType weaponType;
    private int price;
    private float healPercent;
    private CircleCollider2D interactionTrigger;
    private SpriteRenderer itemRenderer;
    private PlayerInteractionController registeredController;
    private bool focused;

    public Transform InteractionTransform => transform;
    public bool IsInteractionAvailable => isActiveAndEnabled
        && interactionTrigger != null
        && interactionTrigger.enabled;
    public ShopOfferKind OfferKind => offerKind;
    public WeaponType WeaponType => weaponType;
    public int Price => price;
    public float HealPercent => healPercent;

    public static ShopPedestal CreateWeapon(
        Transform parent,
        Vector2 position,
        WeaponType configuredWeaponType,
        StageManager manager)
    {
        WeaponDefinition definition = WeaponCatalog.Get(configuredWeaponType);
        ShopPedestal pedestal = CreateBase(
            parent,
            position,
            $"{definition.DisplayName} Pedestal",
            WeaponCatalog.GetIcon(configuredWeaponType),
            manager);
        pedestal.offerKind = ShopOfferKind.Weapon;
        pedestal.weaponType = configuredWeaponType;
        pedestal.price = definition.Price;
        return pedestal;
    }

    public static ShopPedestal CreateHealthPotion(
        Transform parent,
        Vector2 position,
        int configuredPrice,
        float configuredHealPercent,
        StageManager manager)
    {
        ShopPedestal pedestal = CreateBase(
            parent,
            position,
            "Health Potion Pedestal",
            LoadSprite("Shop/HealthPotion"),
            manager);
        pedestal.offerKind = ShopOfferKind.HealthPotion;
        pedestal.price = Mathf.Max(0, configuredPrice);
        pedestal.healPercent = Mathf.Clamp01(configuredHealPercent);
        return pedestal;
    }

    public static ShopPedestal CreateUpgradePotion(
        Transform parent,
        Vector2 position,
        int configuredPrice,
        StageManager manager)
    {
        // The generated source is intentionally high-resolution pixel art.
        // Its custom PPU and slightly smaller display scale preserve the same
        // on-pedestal visual size as the existing 256 px health potion.
        ShopPedestal pedestal = CreateBase(
            parent,
            position,
            "Prismatic Upgrade Potion Pedestal",
            LoadSprite("Shop/UpgradePotion", 650f),
            manager,
            0.42f);
        pedestal.offerKind = ShopOfferKind.UpgradePotion;
        pedestal.price = Mathf.Max(0, configuredPrice);
        return pedestal;
    }

    public void SetInteractionFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (focused)
        {
            RefreshDetails();
        }
        else
        {
            Module1Ui.EnsureForScene().HideShopItemDetails();
        }
    }

    public void Interact(PlayerInteractionController controller)
    {
        if (IsInteractionAvailable)
        {
            stageManager?.RequestShopPurchase(this);
        }
    }

    public void RefreshAvailability()
    {
        if (itemRenderer != null && stageManager != null)
        {
            bool permanentlyUnavailable = stageManager.IsShopOfferConsumed(this);
            itemRenderer.color = permanentlyUnavailable
                ? new Color(0.45f, 0.48f, 0.55f, 0.72f)
                : Color.white;
        }

        if (focused)
        {
            RefreshDetails();
        }
    }

    private static ShopPedestal CreateBase(
        Transform parent,
        Vector2 position,
        string objectName,
        Sprite itemSprite,
        StageManager manager,
        float itemDisplayScale = 0.62f)
    {
        GameObject pedestalObject = new GameObject(objectName);
        pedestalObject.transform.SetParent(parent, false);
        pedestalObject.transform.position = position;

        SpriteRenderer pedestalRenderer = pedestalObject.AddComponent<SpriteRenderer>();
        pedestalRenderer.sprite = LoadSprite("Shop/Pedestal");
        pedestalRenderer.sortingOrder = 5;

        // The artwork has two separated masses: the lower stone column and the
        // raised display slab. A single low collider only blocked the column,
        // allowing the player's feet collider to enter and stand inside the
        // upper slab. Keep both solids separate so the gap and silhouette still
        // match the pixel art while the whole physical pedestal is blocked.
        BoxCollider2D columnCollider = pedestalObject.AddComponent<BoxCollider2D>();
        columnCollider.isTrigger = false;
        columnCollider.size = new Vector2(1.8f, 1.05f);
        columnCollider.offset = new Vector2(0f, -0.45f);

        BoxCollider2D displaySlabCollider = pedestalObject.AddComponent<BoxCollider2D>();
        displaySlabCollider.isTrigger = false;
        displaySlabCollider.size = new Vector2(1.65f, 0.72f);
        displaySlabCollider.offset = new Vector2(0f, 0.46f);

        CircleCollider2D trigger = pedestalObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.1f;

        GameObject itemObject = new GameObject("Displayed Item");
        itemObject.transform.SetParent(pedestalObject.transform, false);
        itemObject.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        itemObject.transform.localScale = Vector3.one * itemDisplayScale;
        SpriteRenderer itemRenderer = itemObject.AddComponent<SpriteRenderer>();
        itemRenderer.sprite = itemSprite;
        itemRenderer.sortingOrder = 6;

        ShopPedestal pedestal = pedestalObject.AddComponent<ShopPedestal>();
        pedestal.stageManager = manager;
        pedestal.interactionTrigger = trigger;
        pedestal.itemRenderer = itemRenderer;
        return pedestal;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
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
        focused = false;
    }

    private void RefreshDetails()
    {
        if (stageManager == null)
        {
            return;
        }

        stageManager.GetShopOfferPresentation(
            this,
            out string title,
            out string details,
            out string action,
            out Color actionColour);
        Module1Ui.EnsureForScene().ShowShopItemDetails(title, details, action, actionColour);
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        return LoadSprite(resourcePath, ShopPixelsPerUnit);
    }

    private static Sprite LoadSprite(string resourcePath, float pixelsPerUnit)
    {
        string cacheKey = $"{resourcePath}@{pixelsPerUnit:0.##}";
        if (SpriteCache.TryGetValue(cacheKey, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError($"Shop art missing at Resources/{resourcePath}.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
        SpriteCache[cacheKey] = sprite;
        return sprite;
    }
}
