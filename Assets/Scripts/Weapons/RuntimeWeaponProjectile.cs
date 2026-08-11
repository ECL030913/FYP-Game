using System.Collections.Generic;
using UnityEngine;

public class RuntimeWeaponProjectile : MonoBehaviour
{
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
        GameObject projectileObject = new GameObject("Runtime Weapon Projectile");
        projectileObject.transform.position = position;
        projectileObject.transform.localScale = Vector3.one * visualScale;

        SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 18;

        CircleCollider2D trigger = projectileObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.2f;

        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        RuntimeWeaponProjectile projectile = projectileObject.AddComponent<RuntimeWeaponProjectile>();
        projectile.direction = direction.normalized;
        projectile.damage = damage;
        projectile.speed = speed;
        projectile.lifetime = Mathf.Max(0.05f, lifetime);
        projectile.remainingHits = Mathf.Max(1, pierce);
        projectile.explosionRadius = explosionRadius;
        projectile.explosive = explosionRadius > 0f;

        float angle = Mathf.Atan2(projectile.direction.y, projectile.direction.x) * Mathf.Rad2Deg;
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return projectile;
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
        Destroy(gameObject);
    }
}

public class WeaponVisualEffect : MonoBehaviour
{
    private float remainingLifetime;

    public static WeaponVisualEffect Create(Sprite sprite, Vector2 position, float scale, float lifetime)
    {
        GameObject effectObject = new GameObject("Weapon Visual Effect");
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 17;

        WeaponVisualEffect effect = effectObject.AddComponent<WeaponVisualEffect>();
        effect.remainingLifetime = lifetime;
        return effect;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        transform.Rotate(0f, 0f, 420f * Time.deltaTime);
        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
