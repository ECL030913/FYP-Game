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
    private GameObject deathPanel;
    private GameObject victoryPanel;
    private GameObject healthBarContainer;
    private Slider healthBarSlider;
    private Text healthValueText;
    private HealthUIDisplay healthUIDisplay;
    private Action pendingShopPurchase;
    private Action pendingShopCancel;
    private int shopPurchaseInputArmFrame;

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
        // Unity 6 removed Arial.ttf from the built-in font set.
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        CreateHud();
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
            roundText.text = $"Stage {stageIndex} / {maximumStageCount} | {stageType}";
        }
    }

    public void ShowStageMessage(string message)
    {
        EnsureHud();
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void ShowShopItemDetails(
        string title,
        string details,
        string action,
        Color actionColour)
    {
        if (shopItemDetailsPanel == null)
        {
            shopItemDetailsPanel = CreatePanel("Shop Item Details", new Vector2(760f, 210f));
            RectTransform rect = shopItemDetailsPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 28f);

            Image background = shopItemDetailsPanel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.045f, 0.09f, 0.96f);
            Outline outline = shopItemDetailsPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.75f, 1f, 0.95f);
            outline.effectDistance = new Vector2(3f, -3f);
        }

        RemoveChildren(shopItemDetailsPanel.transform);
        shopItemDetailsPanel.SetActive(true);
        CreateColoredText(
            shopItemDetailsPanel.transform,
            title,
            27,
            TextAnchor.MiddleCenter,
            42f,
            new Color(0.85f, 0.96f, 1f));
        CreateColoredText(
            shopItemDetailsPanel.transform,
            details,
            18,
            TextAnchor.MiddleCenter,
            78f,
            Color.white);
        CreateColoredText(
            shopItemDetailsPanel.transform,
            action,
            21,
            TextAnchor.MiddleCenter,
            42f,
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
        if (shopPurchasePanel == null)
        {
            shopPurchasePanel = CreatePanel("Shop Purchase Confirmation", new Vector2(590f, 330f));
            Image background = shopPurchasePanel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.04f, 0.075f, 0.98f);
            Outline outline = shopPurchasePanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.8f, 1f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
        }

        pendingShopPurchase = confirm;
        pendingShopCancel = cancel;
        shopPurchaseInputArmFrame = Time.frameCount + 1;
        RemoveChildren(shopPurchasePanel.transform);
        shopPurchasePanel.SetActive(true);
        CreateColoredText(
            shopPurchasePanel.transform,
            title,
            29,
            TextAnchor.MiddleCenter,
            52f,
            new Color(0.85f, 0.96f, 1f));
        CreateColoredText(
            shopPurchasePanel.transform,
            details,
            18,
            TextAnchor.MiddleCenter,
            84f,
            Color.white);
        CreateColoredText(
            shopPurchasePanel.transform,
            "Press E again to confirm, or Esc to cancel",
            18,
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
        if (levelUpPanel == null)
        {
            levelUpPanel = CreatePanel("Level Up", new Vector2(520f, 390f));
        }

        RemoveChildren(levelUpPanel.transform);
        levelUpPanel.SetActive(true);
        CreateText(levelUpPanel.transform, "Level Up!", 30, TextAnchor.MiddleCenter, 52f);
        CreateText(levelUpPanel.transform, "Choose one upgrade. The game is paused.", 17, TextAnchor.MiddleCenter, 38f);

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        PlayerProgression progression = FindAnyObjectByType<PlayerProgression>();
        foreach (ShopUpgradeType upgrade in upgrades)
        {
            ShopUpgradeType capturedUpgrade = upgrade;
            string label = stageManager != null ? stageManager.GetUpgradeLabel(upgrade) : upgrade.ToString();
            CreateButton(levelUpPanel.transform, label, () => progression?.SelectUpgrade(capturedUpgrade), 58f);
        }
    }

    public void HideLevelUp()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel == null)
        {
            pausePanel = CreatePanel("Pause Menu", new Vector2(460f, 330f));
        }

        RemoveChildren(pausePanel.transform);
        pausePanel.SetActive(true);
        CreateText(pausePanel.transform, "Paused", 32, TextAnchor.MiddleCenter, 56f);
        CreateButton(pausePanel.transform, "Resume", () => GamePauseManager.Instance?.ResumeFromPauseMenu());
        CreateButton(pausePanel.transform, "Main Menu", ReturnToMainMenu);
        CreateButton(pausePanel.transform, "Quit Game", QuitGame);
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void ShowDeathMenu()
    {
        if (deathPanel == null)
        {
            deathPanel = CreatePanel("Death Menu", new Vector2(460f, 260f));
        }

        RemoveChildren(deathPanel.transform);
        deathPanel.SetActive(true);
        CreateText(deathPanel.transform, "You Died", 32, TextAnchor.MiddleCenter, 52f);
        CreateText(deathPanel.transform, "Retry starts a new run from Stage 1.", 18, TextAnchor.MiddleCenter, 42f);
        CreateButton(deathPanel.transform, "Retry", () => FindAnyObjectByType<StageManager>()?.RetryFromDeath());
        CreateButton(deathPanel.transform, "Quit Game", QuitGame);
    }

    public void ShowVictoryMenu()
    {
        if (victoryPanel == null)
        {
            victoryPanel = CreatePanel("Victory Menu", new Vector2(480f, 280f));
        }

        RemoveChildren(victoryPanel.transform);
        victoryPanel.SetActive(true);
        CreateText(victoryPanel.transform, "Run Complete", 32, TextAnchor.MiddleCenter, 52f);
        CreateText(victoryPanel.transform, "You defeated the final boss and cleared all 10 stages.", 18, TextAnchor.MiddleCenter, 52f);
        CreateButton(victoryPanel.transform, "Return to Main Menu", ReturnToMainMenu);
        CreateButton(victoryPanel.transform, "Quit Game", QuitGame);
    }

    public void HideAllPanels()
    {
        HideShopItemDetails();
        HideShopPurchaseConfirmation();
        HideLevelUp();
        HidePauseMenu();

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
            progressText.text = $"Level {level} | XP {experience} / {requiredExperience} | Coins {coins}\nWeapon: {weaponName}";
        }
    }

    private void CreateHud()
    {
        if (roundText != null || messageText != null)
        {
            return;
        }

        roundText = CreateFloatingText("Round HUD", new Vector2(0f, -18f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter, 24);
        roundText.text = "Round 1 | Combat";

        messageText = CreateFloatingText("Stage Message", new Vector2(0f, -52f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter, 20);

        progressText = CreateFloatingText("Progress HUD", new Vector2(20f, -92f), new Vector2(0f, 1f), TextAnchor.UpperLeft, 17);
        progressText.text = "Level 1 | XP 0 / 30 | Coins 0\nWeapon: Rune Knife";
    }

    private void CreateHealthBarUI()
    {
        if (healthBarContainer != null)
        {
            return;
        }

        // Create container for health bar
        healthBarContainer = new GameObject("Health Bar Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
        healthBarContainer.transform.SetParent(transform, false);

        RectTransform containerRect = healthBarContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(20f, -20f);
        containerRect.sizeDelta = new Vector2(280f, 80f);

        VerticalLayoutGroup layout = healthBarContainer.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        // Create health value text (100 / 150 HP)
        GameObject healthTextObj = new GameObject("Health Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        healthTextObj.transform.SetParent(healthBarContainer.transform, false);

        healthValueText = healthTextObj.GetComponent<Text>();
        healthValueText.font = font;
        healthValueText.text = "100 / 100";
        healthValueText.fontSize = 18;
        healthValueText.fontStyle = FontStyle.Bold;
        healthValueText.alignment = TextAnchor.MiddleLeft;
        healthValueText.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Red color for health
        healthValueText.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform textRect = healthTextObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(280f, 24f);
        healthTextObj.GetComponent<LayoutElement>().preferredHeight = 24f;

        // Create health bar slider
        GameObject sliderObj = new GameObject("Health Bar Slider", typeof(RectTransform), typeof(Slider), typeof(HealthUIDisplay), typeof(LayoutElement));
        sliderObj.transform.SetParent(healthBarContainer.transform, false);

        healthBarSlider = sliderObj.GetComponent<Slider>();
        healthBarSlider.minValue = 0f;
        healthBarSlider.maxValue = 1f;
        healthBarSlider.value = 1f;
        healthBarSlider.interactable = false;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(280f, 24f);
        sliderObj.GetComponent<LayoutElement>().preferredHeight = 24f;

        // Setup slider appearance
        Image sliderBg = sliderObj.AddComponent<Image>();
        sliderBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // Create fill area
        GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform), typeof(Image));
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        // Create fill
        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = new Color(0.2f, 1f, 0.2f, 1f); // Green health bar
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        healthBarSlider.fillRect = fillRect;

        // Setup HealthUIDisplay
        healthUIDisplay = sliderObj.GetComponent<HealthUIDisplay>();
        healthUIDisplay.healthFillImage = fillImage;
        healthUIDisplay.healthValueText = healthValueText;
    }

    private void EnsureHud()
    {
        if (roundText == null || messageText == null)
        {
            CreateHud();
        }
    }

    public Slider GetHealthBar()
    {
        EnsureHud();
        return healthBarSlider;
    }

    public HealthUIDisplay GetHealthUIDisplay()
    {
        EnsureHud();
        return healthUIDisplay;
    }

    private Text CreateFloatingText(string name, Vector2 anchoredPosition, Vector2 anchor, TextAnchor alignment, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(520f, 48f);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private GameObject CreatePanel(string name, Vector2 size)
    {
        GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(transform, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.06f, 0.08f, 0.13f, 0.92f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return panelObject;
    }

    private void CreateText(Transform parent, string value, int fontSize, TextAnchor alignment, float preferredHeight)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        textObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;
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

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.48f, 0.8f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());
        buttonObject.GetComponent<LayoutElement>().preferredHeight = preferredHeight;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text text = labelObject.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 19;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
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
