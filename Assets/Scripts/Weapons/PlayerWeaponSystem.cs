using UnityEngine;

public class PlayerWeaponSystem : MonoBehaviour
{
    private PlayerStats playerStats;
    private float nextAttackTime;
    private WeaponType equippedWeapon;
    private bool hasEquippedWeapon;

    public WeaponType EquippedWeapon => equippedWeapon;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Start()
    {
        RunData data = RunManager.EnsureInstance().Data;
        Equip(data.equippedWeapon, false);
    }

    private void Update()
    {
        if (playerStats == null || playerStats.IsDead || Time.timeScale <= 0f)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        WeaponDefinition definition = WeaponCatalog.Get(equippedWeapon);
        EnemyStats target = FindNearestEnemy();
        if (target == null)
        {
            return;
        }

        float rangeMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.attackRangeMultiplier
            : 1f;
        float effectiveRange = definition.Range * rangeMultiplier;
        Vector2 origin = transform.position;
        Vector2 targetPoint = GetClosestTargetPoint(target, origin);
        float targetDistance = Vector2.Distance(origin, targetPoint);
        // Do not attack a target the current weapon cannot reach. For ranged
        // weapons the same effective range also determines projectile travel
        // distance below, so range upgrades expand both acquisition and flight.
        if (targetDistance > effectiveRange)
        {
            return;
        }

        Attack(target, targetPoint, definition, effectiveRange);
        float cooldownMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.cooldownMultiplier
            : 1f;
        nextAttackTime = Time.time + definition.Cooldown * cooldownMultiplier;
    }

    public void Equip(WeaponType weaponType, bool save = true)
    {
        bool weaponChanged = !hasEquippedWeapon || equippedWeapon != weaponType;
        equippedWeapon = weaponType;
        hasEquippedWeapon = true;

        // Reapplying run data must not shorten the current attack interval.
        // A real weapon change starts a complete interval for the new weapon.
        if (weaponChanged)
        {
            WeaponDefinition definition = WeaponCatalog.Get(weaponType);
            float cooldownMultiplier = RunManager.Instance != null
                ? RunManager.Instance.Data.cooldownMultiplier
                : 1f;
            nextAttackTime = Time.time + definition.Cooldown * cooldownMultiplier;
        }

        if (save && RunManager.Instance != null)
        {
            RunManager.Instance.Data.equippedWeapon = weaponType;
            RunManager.Instance.SaveRun();
        }

        GetComponent<PlayerProgression>()?.RefreshHud();
    }

    private void Attack(
        EnemyStats target,
        Vector2 targetPoint,
        WeaponDefinition definition,
        float effectiveRange)
    {
        float damageMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.weaponDamageMultiplier
            : 1f;
        float damage = definition.Damage * damageMultiplier;
        Vector2 origin = transform.position;
        Vector2 direction = (targetPoint - origin).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = ((Vector2)target.transform.position - origin).normalized;
        }

        switch (equippedWeapon)
        {
            case WeaponType.MeleeArea:
                PerformMeleeArea(origin, damage, definition.AreaRadius * GetRangeMultiplier());
                break;
            case WeaponType.MeleePierce:
                RuntimeWeaponProjectile.Create(
                    WeaponCatalog.GetAttackSprite(equippedWeapon),
                    origin + direction * 0.35f,
                    direction,
                    damage,
                    definition.ProjectileSpeed,
                    effectiveRange / definition.ProjectileSpeed,
                    definition.Pierce,
                    0f,
                    0.58f);
                break;
            case WeaponType.RangedPierce:
                RuntimeWeaponProjectile.Create(
                    WeaponCatalog.GetAttackSprite(equippedWeapon),
                    origin + direction * 0.35f,
                    direction,
                    damage,
                    definition.ProjectileSpeed,
                    effectiveRange / definition.ProjectileSpeed,
                    definition.Pierce,
                    0f,
                    0.34f);
                break;
            case WeaponType.RangedArea:
                RuntimeWeaponProjectile.Create(
                    WeaponCatalog.GetAttackSprite(equippedWeapon),
                    origin + direction * 0.35f,
                    direction,
                    damage,
                    definition.ProjectileSpeed,
                    effectiveRange / definition.ProjectileSpeed,
                    1,
                    definition.AreaRadius * GetRangeMultiplier(),
                    0.3f);
                break;
        }
    }

    private void PerformMeleeArea(Vector2 origin, float damage, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy"))
            {
                continue;
            }

            hit.GetComponent<EnemyStats>()?.TakeDamage(damage);
        }

        WeaponVisualEffect.Create(
            WeaponCatalog.GetAttackSprite(WeaponType.MeleeArea),
            origin,
            radius,
            0.2f);
    }

    private static float GetRangeMultiplier()
    {
        return RunManager.Instance != null
            ? RunManager.Instance.Data.attackRangeMultiplier
            : 1f;
    }

    private EnemyStats FindNearestEnemy()
    {
        EnemyStats nearest = null;
        float shortestDistance = float.PositiveInfinity;
        for (int i = EnemySpawner.activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyStats enemy = EnemySpawner.activeEnemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
            {
                continue;
            }

            float distance = Vector2.Distance(
                transform.position,
                GetClosestTargetPoint(enemy, transform.position));
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Measures weapon reach against the enemy's collision surface instead of
    /// its transform centre. This keeps large Elite enemies attackable as soon
    /// as their visible body enters melee range.
    /// </summary>
    private static Vector2 GetClosestTargetPoint(EnemyStats enemy, Vector2 origin)
    {
        Collider2D targetCollider = enemy != null
            ? enemy.GetComponent<Collider2D>()
            : null;
        if (targetCollider != null && targetCollider.enabled)
        {
            return targetCollider.ClosestPoint(origin);
        }

        return enemy != null ? (Vector2)enemy.transform.position : origin;
    }
}
