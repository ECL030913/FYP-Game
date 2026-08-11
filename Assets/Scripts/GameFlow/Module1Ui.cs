using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Small runtime UI for the prototype stage: run selection, current node, and
/// the three free Shop upgrades. It uses Unity's built-in UI so no art assets
/// are required at this stage.
/// </summary>
public class Module1Ui : MonoBehaviour
{
    private Font font;
    private Text roundText;
    private Text messageText;
    private Text progressText;
    private GameObject shopPanel;
    private GameObject levelUpPanel;
    private GameObject pausePanel;
    private GameObject deathPanel;
    private GameObject victoryPanel;
    private GameObject healthBarContainer;
    private Slider healthBarSlider;
    private Text healthValueText;
    private HealthUIDisplay healthUIDisplay;

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

    public void ShowShop(IReadOnlyList<ShopUpgradeType> upgrades)
    {
        if (shopPanel == null)
        {
            shopPanel = CreatePanel("Shop", new Vector2(500f, 360f));
        }

        RemoveChildren(shopPanel.transform);
        shopPanel.SetActive(true);
        CreateText(shopPanel.transform, "Shop - Choose One Free Upgrade", 26, TextAnchor.MiddleCenter, 48f);
        CreateText(shopPanel.transform, "No enemies spawn in this room.", 16, TextAnchor.MiddleCenter, 32f);

        StageManager stageManager = FindAnyObjectByType<StageManager>();
        foreach (ShopUpgradeType upgrade in upgrades)
        {
            ShopUpgradeType capturedUpgrade = upgrade;
            string label = stageManager != null ? stageManager.GetUpgradeLabel(upgrade) : upgrade.ToString();
            CreateButton(shopPanel.transform, label, () => stageManager?.ApplyLevelUpgrade(capturedUpgrade));
        }
    }

    public void ShowWeaponShop(IReadOnlyList<WeaponType> weaponTypes)
    {
        if (shopPanel == null)
        {
            shopPanel = CreatePanel("Weapon Shop", new Vector2(680f, 620f));
        }

        RemoveChildren(shopPanel.transform);
        shopPanel.SetActive(true);

        RunData data = RunManager.EnsureInstance().Data;
        StageManager stageManager = FindAnyObjectByType<StageManager>();
        CreateText(shopPanel.transform, $"Weapon Shop - Coins: {data.coins}", 28, TextAnchor.MiddleCenter, 50f);
        CreateText(shopPanel.transform, "Choose one weapon or leave without buying.", 17, TextAnchor.MiddleCenter, 34f);

        foreach (WeaponType weaponType in weaponTypes)
        {
            WeaponType capturedType = weaponType;
            WeaponDefinition definition = WeaponCatalog.Get(weaponType);
            bool equipped = data.equippedWeapon == weaponType;
            bool affordable = data.coins >= definition.Price;
            string state = equipped ? " [EQUIPPED]" : affordable ? string.Empty : " [NOT ENOUGH COINS]";
            string label = $"{definition.DisplayName} - {definition.Price} coins{state}\n{WeaponCatalog.GetStatsText(weaponType)}\n{definition.Description}";
            Button button = CreateButton(
                shopPanel.transform,
                label,
                () => stageManager?.PurchaseWeapon(capturedType),
                84f);
            AddButtonIcon(button, WeaponCatalog.GetIcon(weaponType));
            button.interactable = !equipped && affordable;
        }

        CreateButton(shopPanel.transform, "Leave Shop (No Purchase)", () => stageManager?.LeaveShop(), 52f);
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

    public void HideShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void HideAllPanels()
    {
        HideShop();
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

    private static void AddButtonIcon(Button button, Sprite sprite)
    {
        if (button == null || sprite == null)
        {
            return;
        }

        Transform labelTransform = button.transform.Find("Label");
        if (labelTransform != null)
        {
            RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
            labelRect.offsetMin = new Vector2(78f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
        }

        GameObject iconObject = new GameObject("Weapon Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(button.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(40f, 0f);
        iconRect.sizeDelta = new Vector2(66f, 66f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
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
        SceneManager.LoadScene("Menu");
    }

    private static void RemoveChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
