using UnityEngine;

public class KnifeController : WeaponController
{

    public bool useObjectPooling = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    protected override void Attack()
    {
        base.Attack();

        // Find the nearest enemy
        Transform nearestEnemy = FindNearestEnemy();

        // Do not fire if there are no enemies
        if (nearestEnemy == null) return;

        // Calculate the direction from the player to the enemy
        Vector3 direction = (nearestEnemy.position - transform.position).normalized;

        GameObject spawnedKnife;

        if (useObjectPooling && ObjectPoolManager.Instance != null)
        {
            spawnedKnife = ObjectPoolManager.Instance.GetObject(
                weaponData.Prefab,
                transform.position,
                Quaternion.identity
            );
        }
        else
        {
            spawnedKnife = Instantiate(weaponData.Prefab, transform.position, Quaternion.identity);
        }

        KnifeBehavior knife = spawnedKnife.GetComponent<KnifeBehavior>();

        if (knife != null)
        {
            knife.DirectionChecker(direction);
        }


    }

    Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (EnemyStats enemy in EnemySpawner.activeEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }
}
  