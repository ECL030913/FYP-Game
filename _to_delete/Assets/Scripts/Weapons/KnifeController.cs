using UnityEngine;

public class KnifeController : WeaponController
{

    public bool useObjectPooling = true;
    [SerializeField] private Transform[] firePoints;

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

        Transform owner = pm != null ? pm.transform : transform;
        Vector3 origin = owner.position;

        // Calculate the direction from the player to the enemy
        Vector3 direction = (nearestEnemy.position - origin).normalized;
        Transform firePoint = GetBestFirePoint(direction, origin);
        Vector3 spawnPosition = firePoint != null ? firePoint.position : origin;

        GameObject spawnedKnife;

        if (useObjectPooling && ObjectPoolManager.Instance != null)
        {
            spawnedKnife = ObjectPoolManager.Instance.GetObject(
                weaponData.Prefab,
                spawnPosition,
                Quaternion.identity
            );
        }
        else
        {
            spawnedKnife = Instantiate(weaponData.Prefab, spawnPosition, Quaternion.identity);
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
    private Transform GetBestFirePoint(Vector2 direction, Vector2 origin)
    {
        if (firePoints == null || firePoints.Length == 0)
        {
            return null;
        }

        Transform bestPoint = null;
        float bestDot = -1f;

        foreach (Transform point in firePoints)
        {
            if (point == null)
            {
                continue;
            }

            Vector2 pointDirection = ((Vector2)point.position - origin).normalized;
            float dot = Vector2.Dot(pointDirection, direction.normalized);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestPoint = point;
            }
        }

        return bestPoint;
    }
}
  
