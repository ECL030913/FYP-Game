using UnityEngine;

/// <summary>
/// Converts an existing reinforced enemy instance into a temporary Elite boss
/// without changing the teammate's pool or creating a separate art asset.
/// </summary>
public class StageEnemyModifier : MonoBehaviour
{
    private const float BossMaxHealth = 100f;
    private const float BossDamage = 60f;

    private Vector3 originalScale;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;
    private EnemyStats enemyStats;

    private void Awake()
    {
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        enemyStats = GetComponent<EnemyStats>();
    }

    private void OnEnable()
    {
        ResetVisualsAndStats();
    }

    public void ConfigureAsBoss()
    {
        transform.localScale = originalScale * 1.8f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.9f, 0.2f, 0.3f, 1f);
        }

        // Keep the boss at its intended baseline strength even though it
        // reuses the reinforced enemy prefab and its base stats may change.
        enemyStats?.ApplyStageStats(BossMaxHealth, BossDamage);
    }

    private void ResetVisualsAndStats()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        enemyStats?.ResetRuntimeStats();
    }
}
