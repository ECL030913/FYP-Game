using System.Collections.Generic;
using UnityEngine;

public enum LootType
{
    Experience,
    Coin
}

public class LootPickup : MonoBehaviour
{
    private const float PixelsPerUnit = 128f;
    private const float AttractionRange = 2.5f;
    private const float CollectionRange = 0.32f;
    private const float AttractionSpeed = 6f;

    private static readonly List<LootPickup> ActivePickups = new List<LootPickup>();
    private static readonly Dictionary<LootType, Sprite> SpriteCache = new Dictionary<LootType, Sprite>();

    private LootType lootType;
    private int value;
    private Transform player;
    private bool collected;

    public static void Spawn(LootType type, int value, Vector2 position)
    {
        if (value <= 0)
        {
            return;
        }

        GameObject pickupObject = new GameObject(type == LootType.Coin ? "Coin Pickup" : "Experience Pickup");
        pickupObject.transform.position = position + Random.insideUnitCircle * 0.22f;
        pickupObject.transform.localScale = Vector3.one * (type == LootType.Coin ? 0.42f : 0.36f);

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetSprite(type);
        renderer.sortingOrder = 16;

        CircleCollider2D trigger = pickupObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.42f;

        Rigidbody2D body = pickupObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        LootPickup pickup = pickupObject.AddComponent<LootPickup>();
        pickup.lootType = type;
        pickup.value = value;
    }

    public static void ClearAll()
    {
        LootPickup[] snapshot = ActivePickups.ToArray();
        foreach (LootPickup pickup in snapshot)
        {
            if (pickup != null)
            {
                Destroy(pickup.gameObject);
            }
        }

        ActivePickups.Clear();
    }

    private void OnEnable()
    {
        ActivePickups.Add(this);
    }

    private void OnDisable()
    {
        ActivePickups.Remove(this);
    }

    private void Update()
    {
        if (collected || Time.timeScale <= 0f)
        {
            return;
        }

        if (player == null)
        {
            PlayerProgression progression = FindAnyObjectByType<PlayerProgression>();
            player = progression != null ? progression.transform : null;
        }

        if (player == null)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= CollectionRange)
        {
            Collect(player.GetComponent<PlayerProgression>());
            return;
        }

        if (distance <= AttractionRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                AttractionSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!collected && other.CompareTag("Player"))
        {
            Collect(other.GetComponent<PlayerProgression>());
        }
    }

    private void Collect(PlayerProgression progression)
    {
        if (collected || progression == null)
        {
            return;
        }

        collected = true;
        if (lootType == LootType.Coin)
        {
            progression.AddCoins(value);
        }
        else
        {
            progression.AddExperience(value);
        }

        Destroy(gameObject);
    }

    private static Sprite GetSprite(LootType type)
    {
        if (SpriteCache.TryGetValue(type, out Sprite sprite))
        {
            return sprite;
        }

        string resourcePath = type == LootType.Coin ? "Pickups/Coin" : "Pickups/Experience";
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError($"Loot sprite missing at Resources/{resourcePath}.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        SpriteCache[type] = sprite;
        return sprite;
    }
}
