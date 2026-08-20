using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD and modal UI shared by every gameplay scene.
/// </summary>
public class Module1Ui : MonoBehaviour
{
    private Font font;
    private Text roundText;
    private Text messageText;
    private Text progressText;
    private GameObject shopItemDetailsPanel;
    private GameObject shopPurchasePanel;
    private GameObject levelUpPanel;
    private GameObject pausePanel;
    private GameObject runStatsPanel;
    private GameObject deathPanel;
    private GameObject victoryPanel;
    private GameObject roundHudPanel;
    private GameObject messageHudPanel;
    private GameObject progressHudPanel;
    private Image experienceFillImage;
    private HealthUIDisplay healthUIDisplay;
    private Action pendingShopPurchase;
    private Action pendingShopCancel;
    private int shopPurchaseInputArmFrame;
    private bool levelUpVisible;
    private bool pauseVisible;

    public static Module1Ui EnsureForScene()
    {
        Module1Ui existing = UnityEngine.Object.FindAnyObjectByType<Module1Ui>();
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Module 1 Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        return canvas.gameObject.AddComponent<Module1Ui>();
    }

    private void Awake()
    {
        font = PixelUiTheme.BodyFont;
        PixelUiTheme.ConfigureCanvas(GetComponent<Canvas>());
        CreateHud();
    }

    private void Start()
    {
        // HealthUIDisplay creates its value text during Awake. Styling it in
        // Start guarantees that text exists regardless of script order.
        StyleExistingHealthBar();
    }

    private void Update()
    {
        if (shopPurchasePanel == null
            || !shopPurchasePanel.activeSelf
            || Time.frameCount < shopPurchaseInputArmFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmPendingShopPurchase();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPendingShopPurchase();
        }
    }

    public void UpdateStageHud(int stageIndex, int maximumStageCount, StageType stageType)
    {
        EnsureHud();
        if (roundText != null)
        {
            roundText.gameObject.SetActive(true);
            roundText.text = $"STAGE {stageIndex} / {maximumStageCount}   •   {stageType.ToString().ToUpperInvariant()}";
            roundText.color = PixelUiTheme.GetStageAccent(stageType);
        }
    }

    public void ShowStageMessage(string message)
    {
        EnsureHud();
        if (messageText != null)
        {
            messageText.text = message;
            if (messageHudPanel != null)
            {
                messageHudPanel.SetActive(!string.IsNullOrWhiteSpace(message));
            }
        }
    }

    public void ShowShopItemDetails(
        string title,
        string details,
        string action,
        Color actionColour)
    {
        if (levelUpVisible
            || pauseVisible
            || (shopPurchasePanel != null && shopPurchasePanel.activeSelf))
        {
            return;
        }

        if (shopItemDetailsPanel == null)
        {
            shopItemDetailsPanel = CreatePanel("Shop Item Details", new Vector2(660f, 164f));
            RectTransform rect = shopItemDetailsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 18f);
            ConfigurePanelLayout(shopItemDetailsPanel, new RectOffset(22, 22, 12, 12), 4f);
        }

        RemoveChildren(shopItemDetailsPanel.transform);
        shopItemDetailsPanel.SetActive(true);
        CreateColoredText(
            shopItemDetailsPanel.transform,
            title,
            20,
            TextAnchor.MiddleCenter,
            28f,
            new Color(0.85f, 0.96f, 1f));
        CreateColoredText(
            shopItemDetailsPanel.transform,
            details,
            15,
            TextAnchor.MiddleCenter,
            64f,
            Color.white);
        CreateColoredText(
            shopItemDetailsPanel.transform,
            action,
            16,
            TextAnchor.MiddleCenter,
            28f,
            actionColour);
    }

    public void HideShopItemDetails()
    {
        if (shopItemDetailsPanel != null)
        {
            shopItemDetailsPanel.SetActive(false);
        }
    }

    public void ShowShopPurchaseConfirmation(
        string title,
        string details,
        Action confirm,
        Action cancel)
    {
        HideShopItemDetails();
        if (shopPurchasePanel == null)
        {
            shopPurchasePanel = CreatePanel("Shop Purchase Confirmation", new Vector2(660f, 360f));
        }

        pendingShopPurchase = confirm;
        pendingShopCancel = cancel;
        shopPurchaseInputArmFrame = Time.frameCount + 1;
        RemoveChildren(shopPurchasePanel.transform);
        shopPurchasePanel.SetActive(true);
        CreateColoredText(
            shopPurchasePanel.transform,
            title,
            24,
            TextAnchor.MiddleCenter,
            52f,
            new Color(0.85f, 0.96f, 1f));
        CreateColoredText(
            shopPurchasePanel.transform,
            details,
            17,
            TextAnchor.MiddleCenter,
            84f,
            Color.white);
        CreateColoredText(
            shopPurchasePanel.transform,
            "Press E again to confirm, or Esc to cancel",
            16,
            TextAnchor.MiddleCenter,
            38f,
            new Color(1f, 0.86f, 0.28f));
        CreateButton(shopPurchasePanel.transform, "Confirm Purchase", ConfirmPendingShopPurchase, 48f);
        CreateButton(shopPurchasePanel.transform, "Cancel", CancelPendingShopPurchase, 48f);
    }

    public void HideShopPurchaseConfirmation()
    {
        if (shopPurchasePanel != null)
        {
            shopPurchasePanel.SetActive(false);
        }

        pendingShopPurchase = null;
        pendingShopCancel = null;
    }

    public void ShowLevelUp(IReadOnlyList<ShopUpgradeType> upgrades)
    {
        HideShopItemDetails();
        if (levelUpPanel == null)
        {
            levelUpPanel = CreatePanel("Level Up", new Vector2(720f, 520f));
            RectTransform rect = levelUpPanel.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-190f, 0f);
        }

        RemoveChildren(levelUpPanel.transform);
        levelUpPanel.SetActive(true);
        levelUpVisible = true;
        CreateColoredText(
            levelUpPanel.transform,
            "LEVEL UP",
            30,
            TextAnchor.MiddleCenter,
            46f,
            PixelUiTheme.Gold);
        CreateColoredText(
            levelUpPanel.transform,
            "Choose one permanent upgrade. Combat is paused.",
            17,
            TextAnchor.MiddleCenter,
            38f,
            PixelUiTheme.TextMuted);
        CreateColoredText(
            levelUpPanel.transform,
            "CHOOSE AN UPGRADE",
            18,
            TextAnchor.MiddleCenter,
            34f,
            PixelUiTheme.Cyan);

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        PlayerProgression progression = FindAnyObjectByType<PlayerProgression>();
        foreach (ShopUpgradeType upgrade in upgrades)
        {
            ShopUpgradeType capturedUpgrade = upgrade;
            string label = stageManager != null ? stageManager.GetUpgradeLabel(upgrade) : upgrade.ToString();
            CreateButton(
                levelUpPanel.transform,
                label,
                () => progression?.SelectUpgrade(capturedUpgrade),
                68f);
        }
        ShowRunStatsPanel();
    }

    public void HideLevelUp()
    {
        levelUpVisible = false;
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        UpdateRunStatsPanelVisibility();
        FindAnyObjectByType<ShopSceneController>()?.RefreshAll();
    }

    public void ShowPauseMenu()
    {
        HideShopItemDetails();
        if (pausePanel == null)
        {
            pausePanel = CreatePanel("Pause Menu", new Vector2(520f, 420f));
            RectTransform rect = pausePanel.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-190f, 0f);
        }

        RemoveChildren(pausePanel.transform);
        pausePanel.SetActive(true);
        pauseVisible = true;
        CreateColoredText(
            pausePanel.transform,
            "GAME PAUSED",
            30,
            TextAnchor.MiddleCenter,
            54f,
            PixelUiTheme.Gold);
        CreateButton(pausePanel.transform, "Resume", () => GamePauseManager.Instance?.ResumeFromPauseMenu(), 64f);
        CreateButton(pausePanel.transform, "Main Menu", ReturnToMainMenu, 64f);
        CreateButton(pausePanel.transform, "Quit Game", QuitGame, 64f);
        ShowRunStatsPanel();
    }

    public void HidePauseMenu()
    {
        pauseVisible = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        UpdateRunStatsPanelVisibility();
        FindAnyObjectByType<ShopSceneController>()?.RefreshAll();
    }

    public void ShowDeathMenu()
    {
        levelUpVisible = false;
        pauseVisible = false;
        UpdateRunStatsPanelVisibility();
        if (deathPanel == null)
        {
            deathPanel = CreatePanel("Death Menu", new Vector2(560f, 350f));
        }

        RemoveChildren(deathPanel.transform);
        deathPanel.SetActive(true);
        CreateColoredText(
            deathPanel.transform,
            "YOU DIED",
            38,
            TextAnchor.MiddleCenter,
            62f,
            new Color(1f, 0.28f, 0.22f));
        CreateColoredText(
            deathPanel.transform,
            "Retry begins a new run from Stage 1.",
            18,
            TextAnchor.MiddleCenter,
            48f,
            PixelUiTheme.TextMuted);
        CreateButton(deathPanel.transform, "Retry", () => FindAnyObjectByType<StageManager>()?.RetryFromDeath(), 58f);
        CreateButton(deathPanel.transform, "Quit Game", QuitGame, 58f);
    }

    public void ShowVictoryMenu()
    {
        levelUpVisible = false;
        pauseVisible = false;
        UpdateRunStatsPanelVisibility();
        if (victoryPanel == null)
        {
            victoryPanel = CreatePanel("Victory Menu", new Vector2(600f, 370f));
        }

        RemoveChildren(victoryPanel.transform);
        victoryPanel.SetActive(true);
        CreateColoredText(
            victoryPanel.transform,
            "RUN COMPLETE",
            38,
            TextAnchor.MiddleCenter,
            62f,
            PixelUiTheme.Gold);
        CreateColoredText(
            victoryPanel.transform,
            "The final boss is defeated. All 10 stages are clear.",
            18,
            TextAnchor.MiddleCenter,
            54f,
            PixelUiTheme.TextPrimary);
        CreateButton(victoryPanel.transform, "Return to Main Menu", ReturnToMainMenu, 58f);
        CreateButton(victoryPanel.transform, "Quit Game", QuitGame, 58f);
    }

    public void HideAllPanels()
    {
        levelUpVisible = false;
        pauseVisible = false;
        HideShopItemDetails();
        HideShopPurchaseConfirmation();
        HideLevelUp();
        HidePauseMenu();
        HideShopItemDetails();
        UpdateRunStatsPanelVisibility();

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void UpdateProgressHud(int level, int experience, int requiredExperience, int coins, string weaponName)
    {
        EnsureHud();
        if (progressText != null)
        {
            progressText.text = $"LEVEL {level}     COINS {coins}\nXP {experience} / {requiredExperience}     {weaponName.ToUpperInvariant()}";
        }

        if (experienceFillImage != null)
        {
            float progress = requiredExperience > 0
                ? Mathf.Clamp01((float)experience / requiredExperience)
                : 0f;
            experienceFillImage.rectTransform.anchorMax = new Vector2(progress, 1f);
        }
    }

    private void CreateHud()
    {
        if (roundText != null || messageText != null)
        {
            return;
        }

        roundText = CreateHudPlate(
            "Stage HUD",
            new Vector2(0f, -22f),
            new Vector2(0.5f, 1f),
            new Vector2(560f, 54f),
            TextAnchor.MiddleCenter,
            19,
            out roundHudPanel);
        roundText.text = "STAGE 1 / 10   •   COMBAT";
        roundText.color = PixelUiTheme.Cyan;

        messageText = CreateHudPlate(
            "Stage Message",
            new Vector2(0f, -84f),
            new Vector2(0.5f, 1f),
            new Vector2(720f, 46f),
            TextAnchor.MiddleCenter,
            16,
            out messageHudPanel);
        messageHudPanel.SetActive(false);

        progressText = CreateHudPlate(
            "Progress HUD",
            new Vector2(24f, -80f),
            new Vector2(0f, 1f),
            new Vector2(430f, 84f),
            TextAnchor.MiddleLeft,
            17,
            out progressHudPanel);
        progressText.rectTransform.offsetMin = new Vector2(20f, 22f);
        progressText.rectTransform.offsetMax = new Vector2(-20f, -13f);
        progressText.text = "LEVEL 1     COINS 0\nXP 0 / 30     RUNE KNIFE";

        GameObject experienceTrack = new GameObject(
            "Experience Track",
            typeof(RectTransform),
            typeof(Image));
        experienceTrack.transform.SetParent(progressHudPanel.transform, false);
        RectTransform trackRect = experienceTrack.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(1f, 0f);
        trackRect.pivot = new Vector2(0.5f, 0f);
        trackRect.offsetMin = new Vector2(20f, 10f);
        trackRect.offsetMax = new Vector2(-20f, 18f);
        experienceTrack.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.055f, 0.95f);

        GameObject experienceFill = new GameObject(
            "Experience Fill",
            typeof(RectTransform),
            typeof(Image));
        experienceFill.transform.SetParent(experienceTrack.transform, false);
        RectTransform fillRect = experienceFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        experienceFillImage = experienceFill.GetComponent<Image>();
        experienceFillImage.color = PixelUiTheme.Experience;
    }

    private void EnsureHud()
    {
        if (roundText == null || messageText == null)
        {
            CreateHud();
        }
    }

    private void StyleExistingHealthBar()
    {
        healthUIDisplay = FindAnyObjectByType<HealthUIDisplay>();
        if (healthUIDisplay == null)
        {
            return;
        }

        RectTransform healthRect = healthUIDisplay.GetComponent<RectTransform>();
        if (healthRect == null)
        {
            return;
        }

        healthRect.anchorMin = new Vector2(0f, 1f);
        healthRect.anchorMax = new Vector2(0f, 1f);
        healthRect.pivot = new Vector2(0f, 1f);
        healthRect.anchoredPosition = new Vector2(24f, -24f);
        healthRect.sizeDelta = new Vector2(370f, 44f);

        Image frame = healthUIDisplay.GetComponent<Image>();
        if (frame == null)
        {
            frame = healthUIDisplay.gameObject.AddComponent<Image>();
        }

        frame.raycastTarget = false;
        PixelUiTheme.StylePanel(frame, true);

        Image background = healthRect.Find("Background")?.GetComponent<Image>();
        if (background != null)
        {
            background.sprite = null;
            background.color = new Color(0.11f, 0.015f, 0.025f, 0.98f);
            background.rectTransform.offsetMin = new Vector2(9f, 8f);
            background.rectTransform.offsetMax = new Vector2(-9f, -8f);
        }

        Image fill = healthUIDisplay.healthFillImage;
        if (fill != null)
        {
            fill.sprite = null;
            fill.rectTransform.offsetMin = new Vector2(9f, 8f);
            fill.rectTransform.offsetMax = new Vector2(-9f, -8f);
        }

        healthUIDisplay.ApplyHudTheme(PixelUiTheme.Health, PixelUiTheme.DisplayFont, 16);
    }

    private Text CreateHudPlate(
        string name,
        Vector2 anchoredPosition,
        Vector2 anchor,
        Vector2 size,
        TextAnchor alignment,
        int fontSize,
        out GameObject panelObject)
    {
        panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = anchor;
        panelRect.anchorMax = anchor;
        panelRect.pivot = anchor;
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.raycastTarget = false;
        PixelUiTheme.StylePanel(panelImage, true);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        PixelUiTheme.StyleText(text, fontSize, PixelUiTheme.TextPrimary, true);
        return text;
    }

    private GameObject CreatePanel(string name, Vector2 size)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelObject.transform.SetParent(transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        PixelUiTheme.StylePanel(image);

        ConfigurePanelLayout(panelObject, new RectOffset(32, 32, 24, 24), 10f);
        return panelObject;
    }

    private static void ConfigurePanelLayout(
        GameObject panelObject,
        RectOffset padding,
        float spacing)
    {
        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void ShowRunStatsPanel()
    {
        if (runStatsPanel == null)
        {
            runStatsPanel = CreatePanel("Run Stats", new Vector2(360f, 620f));
            RectTransform rect = runStatsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f, 0f);
            ConfigurePanelLayout(
                runStatsPanel,
                new RectOffset(24, 24, 22, 22),
                5f);
        }

        RemoveChildren(runStatsPanel.transform);
        runStatsPanel.SetActive(true);

        CreateColoredText(
            runStatsPanel.transform,
            "RUN STATS",
            20,
            TextAnchor.MiddleCenter,
            38f,
            PixelUiTheme.Cyan);

        RunData data = RunManager.Instance != null
            ? RunManager.Instance.Data
            : null;
        if (data == null)
        {
            CreateColoredText(
                runStatsPanel.transform,
                "No active run",
                17,
                TextAnchor.MiddleCenter,
                32f,
                PixelUiTheme.TextMuted);
            return;
        }

        CreateStatRow(runStatsPanel.transform, "LEVEL", data.playerLevel.ToString(), PixelUiTheme.TextPrimary);
        CreateStatRow(
            runStatsPanel.transform,
            "STAGE",
            $"{data.currentStageIndex} / {StageManager.MaxStageCount}",
            PixelUiTheme.GetStageAccent(data.currentStageType));
        CreateStatRow(
            runStatsPanel.transform,
            "EXPERIENCE",
            $"{data.currentExperience} / {data.experienceToNextLevel}",
            PixelUiTheme.Experience);
        CreateStatRow(runStatsPanel.transform, "COINS", data.coins.ToString(), PixelUiTheme.Gold);

        CreateColoredText(
            runStatsPanel.transform,
            "PERMANENT UPGRADES",
            16,
            TextAnchor.MiddleLeft,
            30f,
            PixelUiTheme.TextMuted);

        int maximumHealthBonus = Mathf.RoundToInt(Mathf.Max(0f, data.maxHealthBonus));
        int damageBonus = Mathf.RoundToInt(
            Mathf.Max(0f, data.weaponDamageMultiplier - 1f) * 100f);
        int attackSpeedBonus = Mathf.RoundToInt(
            Mathf.Max(0f, 1f / Mathf.Max(0.01f, data.cooldownMultiplier) - 1f) * 100f);
        int rangeBonus = Mathf.RoundToInt(
            Mathf.Max(0f, data.attackRangeMultiplier - 1f) * 100f);

        CreateStatRow(runStatsPanel.transform, "MAX HEALTH", $"+{maximumHealthBonus}", PixelUiTheme.Health);
        CreateStatRow(runStatsPanel.transform, "WEAPON DAMAGE", $"+{damageBonus}%", PixelUiTheme.Gold);
        CreateStatRow(runStatsPanel.transform, "ATTACK SPEED", $"+{attackSpeedBonus}%", PixelUiTheme.Cyan);
        CreateStatRow(runStatsPanel.transform, "MOVE SPEED", $"+{data.moveSpeedBonus:0.00}", new Color(0.35f, 1f, 0.62f));
        CreateStatRow(runStatsPanel.transform, "ATTACK RANGE", $"+{rangeBonus}%", new Color(0.72f, 0.48f, 1f));
    }

    private void UpdateRunStatsPanelVisibility()
    {
        if (runStatsPanel != null)
        {
            runStatsPanel.SetActive(levelUpVisible || pauseVisible);
        }
    }

    private void CreateStatRow(
        Transform parent,
        string label,
        string value,
        Color valueColour)
    {
        GameObject row = new GameObject(
            $"{label} Row",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.GetComponent<LayoutElement>().preferredHeight = 34f;

        Text labelText = CreateInlineText(row.transform, label, TextAnchor.MiddleLeft);
        LayoutElement labelElement = labelText.GetComponent<LayoutElement>();
        labelElement.flexibleWidth = 1f;

        Text valueText = CreateInlineText(row.transform, value, TextAnchor.MiddleRight);
        LayoutElement valueElement = valueText.GetComponent<LayoutElement>();
        valueElement.preferredWidth = 135f;
        PixelUiTheme.StyleText(valueText, 14, valueColour, true);
    }

    private Text CreateInlineText(Transform parent, string value, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(Text),
            typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        PixelUiTheme.StyleText(text, 15, PixelUiTheme.TextPrimary);
        return text;
    }

    private Text CreateColoredText(
        Transform parent,
        string value,
        int fontSize,
        TextAnchor alignment,
        float preferredHeight,
        Color colour)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = colour;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        textObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
        PixelUiTheme.StyleText(text, fontSize, colour, fontSize >= 20);
        return text;
    }

    private void ConfirmPendingShopPurchase()
    {
        Action confirm = pendingShopPurchase;
        HideShopPurchaseConfirmation();
        confirm?.Invoke();
    }

    private void CancelPendingShopPurchase()
    {
        Action cancel = pendingShopCancel;
        GamePauseManager.Instance?.SuppressEscapeForCurrentFrame();
        HideShopPurchaseConfirmation();
        cancel?.Invoke();
    }

    private Button CreateButton(Transform parent, string label, Action onClick, float preferredHeight = 46f)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(() => onClick?.Invoke());
        buttonObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(16f, 5f);
        labelRect.offsetMax = new Vector2(-16f, -5f);

        Text text = labelObject.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        PixelUiTheme.StyleText(text, 19, PixelUiTheme.TextPrimary, true);
        PixelUiTheme.StyleButton(button, PixelUiTheme.Cyan);
        return button;
    }

    private static void QuitGame()
    {
        GamePauseManager.Instance?.ResumeAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void ReturnToMainMenu()
    {
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        RunManager.Instance?.SavePlayerState(player);
        RunManager.Instance?.SaveRun();
        GamePauseManager.Instance?.ResumeAll();
        StageSceneRouter.LoadMenuAsync();
    }

    private static void RemoveChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
