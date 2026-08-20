using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Short-lived screen-space damage number anchored to a world position. It
/// uses the same pixel font as the HUD and continues fading if gameplay is
/// paused immediately after the hit.
/// </summary>
public class DamageNumberPopup : MonoBehaviour
{
    private const float Lifetime = 0.68f;
    private const float RiseDistance = 62f;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private Camera worldCamera;
    private Vector3 worldPosition;
    private Vector2 pixelOffset;
    private float elapsed;

    public static void Spawn(float damage, Vector3 position)
    {
        if (damage <= 0f)
        {
            return;
        }

        Module1Ui moduleUi = Module1Ui.EnsureForScene();
        Canvas canvas = moduleUi != null ? moduleUi.GetComponent<Canvas>() : null;
        if (canvas == null)
        {
            return;
        }

        GameObject popupObject = new GameObject(
            "Enemy Damage Number",
            typeof(RectTransform),
            typeof(Text),
            typeof(CanvasGroup),
            typeof(DamageNumberPopup));
        popupObject.transform.SetParent(canvas.transform, false);
        popupObject.transform.SetAsLastSibling();

        RectTransform popupRect = popupObject.GetComponent<RectTransform>();
        popupRect.sizeDelta = new Vector2(180f, 48f);

        Text text = popupObject.GetComponent<Text>();
        text.text = FormatDamage(damage);
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        PixelUiTheme.StyleText(text, 24, new Color(1f, 0.76f, 0.22f, 1f), true);

        DamageNumberPopup popup = popupObject.GetComponent<DamageNumberPopup>();
        popup.rectTransform = popupRect;
        popup.canvasRect = canvas.transform as RectTransform;
        popup.canvasGroup = popupObject.GetComponent<CanvasGroup>();
        popup.worldCamera = Camera.main != null
            ? Camera.main
            : FindAnyObjectByType<Camera>();
        popup.worldPosition = position;
        popup.pixelOffset = new Vector2(Random.Range(-14f, 14f), 0f);
        popup.UpdateScreenPosition();
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Lifetime);
        pixelOffset.y = Mathf.Lerp(0f, RiseDistance, progress);
        UpdateScreenPosition();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, progress);
        }

        float punchScale = progress < 0.14f
            ? Mathf.Lerp(0.72f, 1.12f, progress / 0.14f)
            : Mathf.Lerp(1.12f, 1f, (progress - 0.14f) / 0.86f);
        transform.localScale = Vector3.one * punchScale;

        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateScreenPosition()
    {
        if (rectTransform == null || canvasRect == null || worldCamera == null)
        {
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                null,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + pixelOffset;
        }
    }

    private static string FormatDamage(float damage)
    {
        float rounded = Mathf.Round(damage);
        return Mathf.Abs(damage - rounded) < 0.05f
            ? rounded.ToString("0")
            : damage.ToString("0.0");
    }
}
