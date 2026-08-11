using UnityEngine;

/// <summary>
/// Reuses the existing reinforced enemy prefab while giving Elite and final
/// bosses distinct visuals and combat values. Every change is restored when
/// the pooled object is enabled again as a regular enemy.
/// </summary>
public class StageEnemyModifier : MonoBehaviour
{
    private const float EliteBossBaseHealth = 260f;
    private const float EliteBossBaseDamage = 42f;
    private const float EliteBossHealthGrowthPerStage = 0.14f;
    private const float EliteBossDamageGrowthPerStage = 0.055f;
    private const float FinalBossMaxHealth = 1800f;
    private const float FinalBossDamage = 70f;
    private const float FinalBossPixelsPerUnit = 256f;

    private const int FinalBossAnimationFrameCount = 6;
    private const float FinalBossAnimationFrameDuration = 0.13f;

    private static Sprite[] finalBossSprites;

    private Vector3 originalScale;
    private Color originalColor;
    private Sprite originalSprite;
    private bool originalAnimatorEnabled;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private SpriteRenderer spriteRenderer;
    private EnemyStats enemyStats;
    private Animator animator;
    private BoxCollider2D boxCollider;
    private bool finalBossMode;
    private float finalBossAnimationTimer;
    private int finalBossAnimationFrame;

    public bool IsFinalBoss => finalBossMode;

    private void Awake()
    {
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        originalSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        enemyStats = GetComponent<EnemyStats>();
        animator = GetComponent<Animator>();
        originalAnimatorEnabled = animator != null && animator.enabled;
        boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
        {
            originalColliderSize = boxCollider.size;
            originalColliderOffset = boxCollider.offset;
        }
    }

    private void OnEnable()
    {
        ResetVisualsAndStats();
    }

    private void Update()
    {
        if (!finalBossMode || spriteRenderer == null)
        {
            return;
        }

        Sprite[] frames = GetFinalBossSprites();
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        finalBossAnimationTimer += Time.deltaTime;
        if (finalBossAnimationTimer < FinalBossAnimationFrameDuration)
        {
            return;
        }

        finalBossAnimationTimer -= FinalBossAnimationFrameDuration;
        finalBossAnimationFrame = (finalBossAnimationFrame + 1) % frames.Length;
        spriteRenderer.sprite = frames[finalBossAnimationFrame];
    }

    public void ConfigureAsBoss(int stageIndex)
    {
        ResetVisualsAndStats();
        transform.localScale = originalScale * 1.8f;

        int growthStage = Mathf.Max(1, stageIndex) - 1;
        float scaledHealth = EliteBossBaseHealth
            * (1f + growthStage * EliteBossHealthGrowthPerStage);
        float scaledDamage = EliteBossBaseDamage
            * (1f + growthStage * EliteBossDamageGrowthPerStage);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.9f, 0.2f, 0.3f, 1f);
        }

        enemyStats?.ApplyStageStats(scaledHealth, scaledDamage);
        enemyStats?.ApplyRewards(30 + stageIndex * 4, 14 + stageIndex * 2);
    }

    public void ConfigureAsFinalBoss()
    {
        ResetVisualsAndStats();
        transform.localScale = originalScale * 1.6f;

        if (animator != null)
        {
            // The existing bat Animator would overwrite the dedicated final
            // boss sprite every frame, so it is disabled only for this form.
            animator.enabled = false;
        }

        if (spriteRenderer != null)
        {
            Sprite[] bossSprites = GetFinalBossSprites();
            if (bossSprites != null && bossSprites.Length > 0)
            {
                spriteRenderer.sprite = bossSprites[0];
            }

            spriteRenderer.color = Color.white;
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(1.65f, 1.35f);
            boxCollider.offset = new Vector2(0f, -0.08f);
        }

        enemyStats?.ApplyStageStats(FinalBossMaxHealth, FinalBossDamage);
        enemyStats?.ApplyRewards(100, 50);
        finalBossMode = true;
        finalBossAnimationTimer = 0f;
        finalBossAnimationFrame = 0;
    }

    private void ResetVisualsAndStats()
    {
        finalBossMode = false;
        finalBossAnimationTimer = 0f;
        finalBossAnimationFrame = 0;
        transform.localScale = originalScale;

        if (animator != null)
        {
            animator.enabled = originalAnimatorEnabled;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = originalSprite;
            spriteRenderer.color = originalColor;
        }

        if (boxCollider != null)
        {
            boxCollider.size = originalColliderSize;
            boxCollider.offset = originalColliderOffset;
        }

        enemyStats?.ResetRuntimeStats();
    }

    private static Sprite[] GetFinalBossSprites()
    {
        if (finalBossSprites != null)
        {
            return finalBossSprites;
        }

        finalBossSprites = new Sprite[FinalBossAnimationFrameCount];
        for (int i = 0; i < FinalBossAnimationFrameCount; i++)
        {
            string path = $"Enemies/FinalBossAnimations/FinalBoss_Frame_{i:00}";
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogError($"Final boss animation frame missing at Resources/{path}.");
                finalBossSprites = null;
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            finalBossSprites[i] = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                FinalBossPixelsPerUnit);
        }

        return finalBossSprites;
    }
}
