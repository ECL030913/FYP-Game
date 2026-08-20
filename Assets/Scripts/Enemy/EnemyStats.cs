using UnityEngine;

public class EnemyStats : MonoBehaviour, IPoolable
{
    public EnemyScriptableObject enemyData;

    float currentMoveSpeed;
    float currentHealth;
    float currentMaxHealth;
    float currentDamage;
    int currentExperienceReward;
    int currentCoinReward;

    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.11f;
    [SerializeField, Range(0f, 1f)] private float hitFlashStrength = 0.82f;

    SpriteRenderer enemySprite;
    Color preHitColour = Color.white;
    float hitFlashTimer;
    bool isHitFlashing;
    bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => currentMaxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        ResetRuntimeStats();

        enemySprite = GetComponent<SpriteRenderer>();

        if (GetComponent<EnemyHealthBar>() == null)
        {
            gameObject.AddComponent<EnemyHealthBar>();
        }

    }
    void Update()
    {
        HandleHitFeedback();
    }

    public void TakeDamage(float dmg)
    {
        if (isDead || dmg <= 0f)
        {
            return;
        }

        currentHealth -= dmg;

        PlayHitFeedback();
        DamageNumberPopup.Spawn(dmg, GetDamageNumberPosition());

        if (currentHealth <= 0)
        {
            Kill();
        }
    }

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        LootPickup.Spawn(LootType.Experience, currentExperienceReward, transform.position);
        LootPickup.Spawn(LootType.Coin, currentCoinReward, transform.position);
        EnemySpawner es = FindAnyObjectByType<EnemySpawner>();

        if (es != null)
        {
            StageEnemyModifier modifier = GetComponent<StageEnemyModifier>();
            if (modifier != null && modifier.IsFinalBoss)
            {
                es.OnFinalBossKilled(this);
            }
            else
            {
                es.OnEnemyKilled();
            }
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
        isDead = false;
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
        isDead = false;
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
        currentMaxHealth = enemyData.MaxHealth * Mathf.Max(1f, healthMultiplier);
        currentHealth = currentMaxHealth;
        currentDamage = enemyData.Damage * Mathf.Max(1f, damageMultiplier);
    }

    public void ApplyStageStats(float health, float damage)
    {
        currentMaxHealth = Mathf.Max(1f, health);
        currentHealth = currentMaxHealth;
        currentDamage = Mathf.Max(0f, damage);
    }

    public void ApplyRewards(int experience, int coins)
    {
        currentExperienceReward = Mathf.Max(0, experience);
        currentCoinReward = Mathf.Max(0, coins);
    }

    public void ResetRuntimeStats()
    {
        if (enemyData == null)
        {
            return;
        }

        currentMoveSpeed = enemyData.MoveSpeed;
        currentMaxHealth = enemyData.MaxHealth;
        currentHealth = currentMaxHealth;
        currentDamage = enemyData.Damage;
        currentExperienceReward = enemyData.ExperienceReward;
        currentCoinReward = enemyData.CoinReward;
    }

    void PlayHitFeedback()
    {
        if (enemySprite == null)
        {
            return;
        }

        // Capture the current stage tint only at the start of a flash. This
        // preserves the red Elite tint and the final boss's own colouring.
        if (!isHitFlashing)
        {
            preHitColour = enemySprite.color;
        }

        hitFlashTimer = hitFlashDuration;
        isHitFlashing = true;
        float strongestOtherChannel = Mathf.Max(preHitColour.g, preHitColour.b);
        bool alreadyRed = preHitColour.r > 0.55f
            && preHitColour.r - strongestOtherChannel > 0.22f;
        Color impactColour = alreadyRed
            ? new Color(1f, 1f, 0.62f, preHitColour.a)
            : new Color(1f, 0.08f, 0.08f, preHitColour.a);
        enemySprite.color = Color.Lerp(preHitColour, impactColour, hitFlashStrength);
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
                enemySprite.color = preHitColour;
            }
        }
    }

    Vector3 GetDamageNumberPosition()
    {
        if (enemySprite == null)
        {
            return transform.position + Vector3.up;
        }

        Bounds bounds = enemySprite.bounds;
        return new Vector3(bounds.center.x, bounds.max.y + 0.24f, transform.position.z);
    }

    void ResetHitFeedback()
    {
        if (isHitFlashing && enemySprite != null)
        {
            enemySprite.color = preHitColour;
        }

        isHitFlashing = false;
        hitFlashTimer = 0f;
    }
}
