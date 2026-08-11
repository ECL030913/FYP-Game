using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages visual health bar and text display with animations.
/// Shows health value, pulsing effect at low health, and damage number popups.
/// </summary>
public class HealthUIDisplay : MonoBehaviour
{
    [Header("Health Bar")]
    public Image healthFillImage;
    
    [Header("Health Text")]
    public Text healthValueText;
    [SerializeField] private int healthTextFontSize = 18;
    [SerializeField] private Vector2 healthTextOffset = new Vector2(0f, 4f);
    
    [Header("Pulsing Effect")]
    [SerializeField] private float pulseThreshold = 0.3f; // Pulse when health < 30%
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseMinAlpha = 0.4f;
    
    [Header("Damage Popup")]
    [SerializeField] private Font damagePopupFont;
    [SerializeField] private int damagePopupFontSize = 32;
    [SerializeField] private float damagePopupDuration = 1.5f;
    [SerializeField] private float damagePopupRiseSpeed = 60f; // pixels per second
    
    private float currentHealthPercent;
    private bool isLowHealth;
    private Color originalHealthBarColor;
    private Canvas worldCanvas;
    private RectTransform healthFillRect;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        UpdatePulseEffect();
    }

    /// <summary>
    /// Updates health bar and text display
    /// </summary>
    public void UpdateDisplay(float currentHealth, float maxHealth)
    {
        Initialize();

        currentHealthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        currentHealthPercent = Mathf.Clamp01(currentHealthPercent);
        isLowHealth = currentHealthPercent < pulseThreshold;

        UpdateFillAmount();

        if (healthValueText != null)
        {
            healthValueText.text = $"{Mathf.Max(0, Mathf.RoundToInt(currentHealth))} / {Mathf.RoundToInt(maxHealth)}";
        }
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (healthFillImage == null)
        {
            Transform fill = transform.Find("Fill") ?? transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                healthFillImage = fill.GetComponent<Image>();
            }
        }

        if (healthFillImage == null)
        {
            Debug.LogWarning("HealthUIDisplay: healthFillImage is not assigned.");
            return;
        }

        healthFillRect = healthFillImage.rectTransform;
        originalHealthBarColor = healthFillImage.color;

        EnsureHealthValueText();

        initialized = true;
    }

    private void EnsureHealthValueText()
    {
        Font uiFont = damagePopupFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (healthValueText == null)
        {
            GameObject textObject = new GameObject("Health Value Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(transform, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            healthValueText = textObject.GetComponent<Text>();
            healthValueText.alignment = TextAnchor.MiddleCenter;
            healthValueText.color = Color.white;
            healthValueText.fontSize = Mathf.Max(14, healthTextFontSize);
            healthValueText.fontStyle = FontStyle.Bold;
            healthValueText.raycastTarget = false;
            healthValueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            healthValueText.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        healthValueText.font = uiFont;
    }

    private void UpdateFillAmount()
    {
        if (healthFillImage == null)
        {
            return;
        }

        if (healthFillImage.type == Image.Type.Filled)
        {
            healthFillImage.fillAmount = currentHealthPercent;
            return;
        }

        if (healthFillRect != null)
        {
            Vector3 scale = healthFillRect.localScale;
            scale.x = currentHealthPercent;
            healthFillRect.localScale = scale;
        }
    }

    /// <summary>
    /// Pulsing animation when health is low
    /// </summary>
    private void UpdatePulseEffect()
    {
        if (healthFillImage == null)
        {
            return;
        }

        if (!isLowHealth)
        {
            healthFillImage.color = originalHealthBarColor;
            return;
        }

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(pulseMinAlpha, 1f, pulse);

        Color pulsedColor = originalHealthBarColor;
        pulsedColor.a = alpha;
        healthFillImage.color = pulsedColor;
    }

    /// <summary>
    /// Shows a damage popup number that floats up and fades
    /// </summary>
    public void ShowDamagePopup(float damage, Vector3 worldPosition)
    {
        if (damagePopupFont == null)
        {
            damagePopupFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // Get or create world canvas
        if (worldCanvas == null)
        {
            worldCanvas = FindAnyObjectByType<Canvas>();
            if (worldCanvas == null || worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Create a world space canvas if needed
                GameObject canvasObj = new GameObject("Damage Popup Canvas");
                worldCanvas = canvasObj.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Create damage text object
        GameObject popupObject = new GameObject("Damage Popup", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
        popupObject.transform.SetParent(worldCanvas.transform, false);

        RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200f, 60f);
        
        // Position at screen position relative to world position
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        rectTransform.position = screenPos;

        Text text = popupObject.GetComponent<Text>();
        text.text = $"-{Mathf.RoundToInt(damage)} HP";
        text.font = damagePopupFont;
        text.fontSize = damagePopupFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.2f, 0.2f, 1f); // Red color
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        CanvasGroup canvasGroup = popupObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Start popup animation
        StartCoroutine(AnimateDamagePopup(popupObject, rectTransform, canvasGroup, damagePopupDuration));
    }

    /// <summary>
    /// Animates damage popup floating up and fading out
    /// </summary>
    private System.Collections.IEnumerator AnimateDamagePopup(GameObject popupObject, RectTransform rectTransform, CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        Vector2 startPos = rectTransform.position;

        while (elapsed < duration)
        {
            // Popups are UI feedback and should finish even while gameplay is
            // paused by the Shop, level-up selection, or ESC menu.
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Move up
            Vector2 newPos = startPos;
            newPos.y += damagePopupRiseSpeed * elapsed;
            rectTransform.position = newPos;

            // Fade out
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);

            yield return null;
        }

        Destroy(popupObject);
    }

    /// <summary>
    /// Shows a healing popup (green color, positive number)
    /// </summary>
    public void ShowHealingPopup(float amount, Vector3 worldPosition)
    {
        if (damagePopupFont == null)
        {
            damagePopupFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (worldCanvas == null)
        {
            worldCanvas = FindAnyObjectByType<Canvas>();
            if (worldCanvas == null || worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                GameObject canvasObj = new GameObject("Healing Popup Canvas");
                worldCanvas = canvasObj.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        GameObject popupObject = new GameObject("Healing Popup", typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
        popupObject.transform.SetParent(worldCanvas.transform, false);

        RectTransform rectTransform = popupObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200f, 60f);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);
        rectTransform.position = screenPos;

        Text text = popupObject.GetComponent<Text>();
        text.text = $"+{Mathf.RoundToInt(amount)} HP";
        text.font = damagePopupFont;
        text.fontSize = damagePopupFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.2f, 1f, 0.2f, 1f); // Green color
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        CanvasGroup canvasGroup = popupObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        StartCoroutine(AnimateDamagePopup(popupObject, rectTransform, canvasGroup, damagePopupDuration));
    }

    /// <summary>
    /// Resets pulse effect to normal color
    /// </summary>
    public void ResetPulseEffect()
    {
        if (healthFillImage != null)
        {
            healthFillImage.color = originalHealthBarColor;
        }
    }
}
