using UnityEngine;

// A base script for all projectile behavior
public class ProjectileWeaponController : MonoBehaviour, IPoolable
{
    public WeaponScriptableObject weaponData;

    protected Vector3 direction;
    public float destroyAfterSeconds;

    Vector3 originalScale;

    [SerializeField]
    float visualRotationOffset = 0f;

    float lifetimeTimer;
    bool isReleased;
    Collider2D projectileCollider;
    float baseDestroyAfterSeconds;

    //current stats
    protected float currentDamage;
    protected float currentSpeed;
    protected float currentCooldownDuration;
    protected int currentPierce;


    void Awake()
    {
        baseDestroyAfterSeconds = destroyAfterSeconds;
        ApplyRuntimeModifiers();

        projectileCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;
    }

    protected virtual void Start()
    {
       
    }

    protected virtual void Update()
    {
        lifetimeTimer += Time.deltaTime;

        if (lifetimeTimer >= destroyAfterSeconds)
        {
            Release();
        }
    }
    public void DirectionChecker(Vector3 dir)
    {
        direction = dir.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffset);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        //Refference the script from the collided collider and deal demage using TakeDemage()
        if (col.CompareTag("Enemy"))
        {
            EnemyStats enemy = col.GetComponent<EnemyStats>();
            enemy.TakeDamage(currentDamage);
            ReducedPierce();
        }
    }

    void ReducedPierce()
    {
        currentPierce--;

        if (currentPierce < 0)
        {
            Release();
        }
    }

    public virtual void OnGetFromPool()
    {
        ApplyRuntimeModifiers();

        lifetimeTimer = 0f;
        isReleased = false;
        direction = Vector3.zero;

        transform.rotation = Quaternion.identity;

        if (projectileCollider != null)
        {
            projectileCollider.enabled = true;
        }
    }

    public virtual void OnReturnToPool()
    {
        direction = Vector3.zero;
        lifetimeTimer = 0f;
        isReleased = true;

        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }
    }

    protected void Release()
    {
        if (isReleased)
        {
            return;
        }

        isReleased = true;

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReleaseObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyRuntimeModifiers()
    {
        float damageMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.weaponDamageMultiplier
            : 1f;
        float rangeMultiplier = RunManager.Instance != null
            ? RunManager.Instance.Data.attackRangeMultiplier
            : 1f;

        currentDamage = weaponData.Damage * damageMultiplier;
        currentSpeed = weaponData.Speed;
        currentCooldownDuration = weaponData.CooldownDuration;
        currentPierce = weaponData.Pierce;
        destroyAfterSeconds = baseDestroyAfterSeconds * rangeMultiplier;
    }
}
