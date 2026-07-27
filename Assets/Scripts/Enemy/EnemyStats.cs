using UnityEngine;

public class EnemyStats : MonoBehaviour, IPoolable
{
    public EnemyScriptableObject enemyData;

    float currentMoveSpeed;
    float currentHealth;
    float currentDamage;

    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private float hitFlashRate = 24f;

    SpriteRenderer enemySprite;
    float hitFlashTimer;
    bool isHitFlashing;

    private void Awake()
    {
        ResetRuntimeStats();

        if (GetComponent<EnemyAI>() == null)
        {
            gameObject.AddComponent<EnemyAI>();
        }

        enemySprite = GetComponent<SpriteRenderer>();

    }
    void Update()
    {
        HandleHitFeedback();
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;

        PlayHitFeedback();

        if (currentHealth <= 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        EnemySpawner es = FindAnyObjectByType<EnemySpawner>();

        if (es != null)
        {
            es.OnEnemyKilled();
        }

        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReleaseObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*private void OnDestroy()
    {
        EnemySpawner es = FindAnyObjectByType<EnemySpawner>();
        if (es != null && Application.isPlaying)
        {
            es.OnEnemyKilled();
        }
    }*/


    public void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            PlayerStats player = col.gameObject.GetComponent<PlayerStats>();

            if (player != null)
            {
                player.TakeDamage(currentDamage);
            }
        }
    }

    void OnEnable()
    {
        if (!EnemySpawner.activeEnemies.Contains(this))
        {
            EnemySpawner.activeEnemies.Add(this);
        }
    }

    void OnDisable()
    {
        EnemySpawner.activeEnemies.Remove(this);
    }

    public void OnGetFromPool()
    {
        ResetRuntimeStats();
        ResetHitFeedback();
    }

    public void OnReturnToPool()
    {
        ResetRuntimeStats();
        ResetHitFeedback();
    }

    public void ApplyStageMultipliers(float healthMultiplier, float damageMultiplier)
    {
        currentHealth = enemyData.MaxHealth * Mathf.Max(1f, healthMultiplier);
        currentDamage = enemyData.Damage * Mathf.Max(1f, damageMultiplier);
    }

    public void ApplyStageStats(float health, float damage)
    {
        currentHealth = Mathf.Max(1f, health);
        currentDamage = Mathf.Max(0f, damage);
    }

    public void ResetRuntimeStats()
    {
        if (enemyData == null)
        {
            return;
        }

        currentMoveSpeed = enemyData.MoveSpeed;
        currentHealth = enemyData.MaxHealth;
        currentDamage = enemyData.Damage;
    }

    void PlayHitFeedback()
    {
        if (enemySprite == null)
        {
            return;
        }

        hitFlashTimer = hitFlashDuration;
        isHitFlashing = true;
    }

    void HandleHitFeedback()
    {
        if (!isHitFlashing)
        {
            return;
        }

        hitFlashTimer -= Time.deltaTime;

        if (hitFlashTimer <= 0f)
        {
            isHitFlashing = false;

            if (enemySprite != null)
            {
                enemySprite.enabled = true;
            }

            return;
        }

        if (enemySprite != null)
        {
            enemySprite.enabled = Mathf.FloorToInt(hitFlashTimer * hitFlashRate) % 2 == 0;
        }
    }
    void ResetHitFeedback()
    {
        isHitFlashing = false;
        hitFlashTimer = 0f;

        if (enemySprite != null)
        {
            enemySprite.enabled = true;
        }
    }
}
