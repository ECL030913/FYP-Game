using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    MeleeArea,
    MeleePierce,
    RangedPierce,
    RangedArea
}

public readonly struct WeaponDefinition
{
    public WeaponDefinition(
        WeaponType type,
        string displayName,
        string description,
        float damage,
        float cooldown,
        float range,
        int pierce,
        float projectileSpeed,
        float areaRadius,
        int price,
        string resourcePrefix)
    {
        Type = type;
        DisplayName = displayName;
        Description = description;
        Damage = damage;
        Cooldown = cooldown;
        Range = range;
        Pierce = pierce;
        ProjectileSpeed = projectileSpeed;
        AreaRadius = areaRadius;
        Price = price;
        ResourcePrefix = resourcePrefix;
    }

    public WeaponType Type { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public float Damage { get; }
    public float Cooldown { get; }
    public float Range { get; }
    public int Pierce { get; }
    public float ProjectileSpeed { get; }
    public float AreaRadius { get; }
    public int Price { get; }
    public string ResourcePrefix { get; }
}

public static class WeaponCatalog
{
    private const float PixelsPerUnit = 128f;

    private static readonly WeaponType[] WeaponTypes =
    {
        WeaponType.MeleeArea,
        WeaponType.MeleePierce,
        WeaponType.RangedPierce,
        WeaponType.RangedArea
    };

    private static readonly Dictionary<WeaponType, WeaponDefinition> Definitions = new Dictionary<WeaponType, WeaponDefinition>
    {
        {
            WeaponType.MeleeArea,
            new WeaponDefinition(
                WeaponType.MeleeArea,
                "Frost Cleaver",
                "High damage around the player; short reach.",
                24f,
                0.8f,
                1.25f,
                99,
                0f,
                1.25f,
                18,
                "MeleeArea")
        },
        {
            WeaponType.MeleePierce,
            new WeaponDefinition(
                WeaponType.MeleePierce,
                "Crimson Lance",
                "Highest single-target damage; short piercing thrust.",
                45f,
                1f,
                1.8f,
                4,
                8f,
                0f,
                32,
                "MeleePierce")
        },
        {
            WeaponType.RangedPierce,
            new WeaponDefinition(
                WeaponType.RangedPierce,
                "Rune Knife",
                "Long-range projectile that pierces several enemies.",
                30f,
                0.9f,
                8f,
                3,
                12f,
                0f,
                26,
                "RangedPierce")
        },
        {
            WeaponType.RangedArea,
            new WeaponDefinition(
                WeaponType.RangedArea,
                "Ember Cannon",
                "Safer ranged attack with low damage and a wide blast.",
                16f,
                1.1f,
                7f,
                1,
                8f,
                1.8f,
                22,
                "RangedArea")
        }
    };

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public static IReadOnlyList<WeaponType> AllTypes => WeaponTypes;

    public static WeaponDefinition Get(WeaponType type)
    {
        return Definitions[type];
    }

    public static string GetStatsText(WeaponType type)
    {
        WeaponDefinition definition = Get(type);
        switch (type)
        {
            case WeaponType.MeleeArea:
                return $"Damage {definition.Damage:0} | Cooldown {definition.Cooldown:0.00}s | Radius {definition.AreaRadius:0.00}";
            case WeaponType.MeleePierce:
                return $"Damage {definition.Damage:0} | Cooldown {definition.Cooldown:0.00}s | Pierce {definition.Pierce} | Reach {definition.Range:0.0}";
            case WeaponType.RangedPierce:
                return $"Damage {definition.Damage:0} | Cooldown {definition.Cooldown:0.00}s | Pierce {definition.Pierce} | Speed {definition.ProjectileSpeed:0}";
            case WeaponType.RangedArea:
                return $"Damage {definition.Damage:0} | Cooldown {definition.Cooldown:0.00}s | Blast {definition.AreaRadius:0.0} | Speed {definition.ProjectileSpeed:0}";
            default:
                return definition.DisplayName;
        }
    }

    public static string GetCategory(WeaponType type)
    {
        return type switch
        {
            WeaponType.MeleeArea => "MELEE AREA",
            WeaponType.MeleePierce => "MELEE PIERCING",
            WeaponType.RangedPierce => "RANGED PIERCING",
            WeaponType.RangedArea => "RANGED AREA",
            _ => type.ToString().ToUpperInvariant()
        };
    }

    public static string GetDetailedDescription(WeaponType type)
    {
        return type switch
        {
            WeaponType.MeleeArea =>
                "Purpose: Fast crowd clearing around the player.\n"
                + "Strength: High damage against groups in one swing.\n"
                + "Trade-off: Very short reach requires risky close combat.",
            WeaponType.MeleePierce =>
                "Purpose: High single-target damage for Elite enemies and bosses.\n"
                + "Strength: Highest damage and pierces enemies in a straight thrust.\n"
                + "Trade-off: Short reach and narrow attack direction.",
            WeaponType.RangedPierce =>
                "Purpose: Safe, reliable boss damage from a distance.\n"
                + "Strength: Long-range projectiles pass through several enemies.\n"
                + "Trade-off: Limited crowd control when enemies surround the player.",
            WeaponType.RangedArea =>
                "Purpose: Safely clear groups with an explosive projectile.\n"
                + "Strength: Damages every enemy inside the blast radius.\n"
                + "Trade-off: Lowest direct damage and a slower attack cycle.",
            _ => Get(type).Description
        };
    }

    public static Sprite GetIcon(WeaponType type)
    {
        return LoadSprite($"Weapons/{Get(type).ResourcePrefix}_Icon");
    }

    public static Sprite GetAttackSprite(WeaponType type)
    {
        return LoadSprite($"Weapons/{Get(type).ResourcePrefix}_Attack");
    }

    private static Sprite LoadSprite(string path)
    {
        if (SpriteCache.TryGetValue(path, out Sprite cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogError($"Weapon art missing at Resources/{path}.");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        SpriteCache[path] = sprite;
        return sprite;
    }
}
