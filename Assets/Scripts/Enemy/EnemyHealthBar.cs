using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    private static Sprite centeredSprite;
    private static Sprite leftPivotSprite;

    private EnemyStats enemyStats;
    private SpriteRenderer enemyRenderer;
    private Transform barRoot;
    private Transform background;
    private Transform fill;

    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        enemyRenderer = GetComponent<SpriteRenderer>();
        CreateBar();
    }

    private void LateUpdate()
    {
        if (enemyStats == null || enemyRenderer == null || barRoot == null)
        {
            return;
        }

        float ratio = enemyStats.MaxHealth > 0f
            ? Mathf.Clamp01(enemyStats.CurrentHealth / enemyStats.MaxHealth)
            : 0f;
        bool visible = !enemyStats.IsDead && ratio > 0f;
        barRoot.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        Bounds bounds = enemyRenderer.bounds;
        float width = Mathf.Clamp(bounds.size.x * 0.9f, 0.7f, 3.2f);
        float height = Mathf.Clamp(width * 0.09f, 0.075f, 0.18f);

        barRoot.position = new Vector3(bounds.center.x, bounds.max.y + 0.18f, transform.position.z);
        barRoot.rotation = Quaternion.identity;
        Vector3 parentScale = transform.lossyScale;
        barRoot.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
            1f);

        background.localScale = new Vector3(width + 0.08f, height + 0.05f, 1f);
        fill.localScale = new Vector3(width * ratio, height, 1f);
        fill.localPosition = new Vector3(-width * 0.5f, 0f, 0f);
    }

    private void CreateBar()
    {
        barRoot = new GameObject("Enemy Health Bar").transform;
        barRoot.SetParent(transform, false);

        background = new GameObject("Background").transform;
        background.SetParent(barRoot, false);
        SpriteRenderer backgroundRenderer = background.gameObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = GetCenteredSprite();
        backgroundRenderer.color = new Color(0.04f, 0.04f, 0.05f, 0.95f);
        backgroundRenderer.sortingOrder = 100;

        fill = new GameObject("Fill").transform;
        fill.SetParent(barRoot, false);
        SpriteRenderer fillRenderer = fill.gameObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = GetLeftPivotSprite();
        fillRenderer.color = new Color(0.22f, 0.9f, 0.25f, 1f);
        fillRenderer.sortingOrder = 101;
    }

    private static Sprite GetCenteredSprite()
    {
        if (centeredSprite == null)
        {
            centeredSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        return centeredSprite;
    }

    private static Sprite GetLeftPivotSprite()
    {
        if (leftPivotSprite == null)
        {
            leftPivotSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0f, 0.5f),
                1f);
        }

        return leftPivotSprite;
    }
}
