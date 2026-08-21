using System.Collections.Generic;
using UnityEngine;

public class RuntimeWeaponProjectile : MonoBehaviour, IPoolable
{
    private const string PoolKey = "RuntimeWeaponProjectile";

    private readonly HashSet<EnemyStats> hitEnemies = new HashSet<EnemyStats>();
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float explosionRadius;
    private int remainingHits;
    private bool explosive;
    private bool finished;

    public static RuntimeWeaponProjectile Create(
        Sprite sprite,
        Vector2 position,
        Vector2 direction,
        float damage,
        float speed,
        float lifetime,
        int pierce,
        float explosionRadius,
        float visualScale)
    {
        GameObject projectileObject;

        // Pooled path: reuse a previously-released projectile instead of
        // allocating/destroying a GameObject on every shot. Falls back to a
        // one-off object if the pool manager isn't in the scene.
        if (ObjectPoolManager.Instance != null)
        {
            projectileObject = ObjectPoolManager.Instance.GetPooledObject(
                PoolKey,
                BuildProjectileObject,
                position,
                Quaternion.identity);
        }
        else
        {
            projectileObject = BuildProjectileObject();
            projectileObject.transform.position = position;
        }

        projectileObject.transform.localScale = Vector3.one * visualScale;

        RuntimeWeaponProjectile projectile = projectileObject.GetComponent<RuntimeWeaponProjectile>();
        SpriteRenderer renderer = projectileObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        // Re-arm the collider in case this instance is coming straight from
        // the pool without going through OnGetFromPool (e.g. pooling disabled).
        CircleCollider2D trigger = projectileObject.GetComponent<CircleCollider2D>();
        if (trigger != null)
        {
            trigger.enabled = true;
        }

        projectile.hitEnemies.Clear();
        projectile.direction = direction.normalized;
        projectile.damage = damage;
        projectile.speed = speed;
        projectile.lifetime = Mathf.Max(0.05f, lifetime);
        projectile.remainingHits = Mathf.Max(1, pierce);
        projectile.explosionRadius = explosionRadius;
        projectile.explosive = explosionRadius > 0f;
        projectile.finished = false;

        float angle = Mathf.Atan2(projectile.direction.y, projectile.direction.x) * Mathf.Rad2Deg;
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return projectile;
    }

    /// <summary>
    /// Builds the fixed component layout for a projectile. Called by the pool
    /// only when it needs to grow (or directly as a fallback when there is no
    /// ObjectPoolManager in the scene) — never once per shot.
    /// </summary>
    private static GameObject BuildProjectileObject()
    {
        GameObject projectileObject = new GameObject("Runtime Weapon Projectile");

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 18;

        CircleCollider2D trigger = projectileObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.2f;

        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        projectileObject.AddComponent<RuntimeWeaponProjectile>();

        return projectileObject;
    }

    public void OnGetFromPool()
    {
        finished = false;

        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        if (trigger != null)
        {
            trigger.enabled = true;
        }
    }

    public void OnReturnToPool()
    {
        CircleCollider2D trigger = GetComponent<CircleCollider2D>();
        if (trigger != null)
        {
            trigger.enabled = false;
        }
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            if (explosive)
            {
                Explode();
            }
            else
            {
                Finish();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (finished || !other.CompareTag("Enemy"))
        {
            return;
        }

        EnemyStats enemy = other.GetComponent<EnemyStats>();
        if (enemy == null)
        {
            return;
        }

        if (explosive)
        {
            // Pass the collider that triggered the explosion explicitly. The
            // projectile used to add this enemy to hitEnemies first, causing
            // the area scan below to skip the unit at the blast centre.
            Explode(enemy);
            return;
        }

        if (!hitEnemies.Add(enemy))
        {
            return;
        }

        enemy.TakeDamage(damage);
        remainingHits--;
        if (remainingHits <= 0)
        {
            Finish();
        }
    }

    private void Explode(EnemyStats directHit = null)
    {
        if (finished)
        {
            return;
        }

        // A direct hit is guaranteed to receive the blast once, even if its
        // collider is only touching the edge of the overlap query this frame.
        if (directHit != null && !directHit.IsDead && hitEnemies.Add(directHit))
        {
            directHit.TakeDamage(damage);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
            {
                continue;
            }

            EnemyStats enemy = hit.GetComponent<EnemyStats>();
            if (enemy != null && hitEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);
            }
        }

        WeaponVisualEffect.Create(
            WeaponCatalog.GetAttackSprite(WeaponType.RangedArea),
            transform.position,
            explosionRadius * 1.1f,
            0.22f);
        Finish();
    }

    private void Finish()
    {
        if (finished)
        {
            return;
        }

        finished = true;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReleaseObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

public class WeaponVisualEffect : MonoBehaviour, IPoolable
{
    private const string PoolKey = "WeaponVisualEffect";

    private float remainingLifetime;
    private bool finished;

    public static WeaponVisualEffect Create(Sprite sprite, Vector2 position, float scale, float lifetime)
    {
        GameObject effectObject;

        if (ObjectPoolManager.Instance != null)
        {
            effectObject = ObjectPoolManager.Instance.GetPooledObject(
                PoolKey,
                BuildEffectObject,
                position,
                Quaternion.identity);
        }
        else
        {
            effectObject = BuildEffectObject();
            effectObject.transform.position = position;
        }

        effectObject.transform.localScale = Vector3.one * scale;
        effectObject.transform.rotation = Quaternion.identity;

        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        WeaponVisualEffect effect = effectObject.GetComponent<WeaponVisualEffect>();
        effect.remainingLifetime = lifetime;
        effect.finished = false;
        return effect;
    }

    private static GameObject BuildEffectObject()
    {
        GameObject effectObject = new GameObject("Weapon Visual Effect");

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 17;

        effectObject.AddComponent<WeaponVisualEffect>();

        return effectObject;
    }

    public void OnGetFromPool()
    {
        finished = false;
    }

    public void OnReturnToPool()
    {
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;
        transform.Rotate(0f, 0f, 420f * Time.deltaTime);
        if (remainingLifetime <= 0f)
        {
            finished = true;

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReleaseObject(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
