using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small runtime UI for the prototype stage: run selection, current node, and
/// the three free Shop upgrades. It uses Unity's built-in UI so no art assets
/// are required at this stage.
/// </summary>
public class Module1Ui : MonoBehaviour
{
    private Font font;
    private Text stageText;
    private Text messageText;
    private GameObject runMenuPanel;
    private GameObject shopPanel;
    private GameObject deathPanel;

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

    public void UpdateStageHud(int stageIndex, StageType stageType)
    {
        EnsureHud();
        if (stageText != null)
        {
            stageText.text = $"Stage {stageIndex}  |  {stageType}";
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

    public void ShowRunMenu(bool showContinue)
    {
        if (runMenuPanel == null)
        {
            runMenuPanel = CreatePanel("Run Menu", new Vector2(460f, 260f));
        }

        RemoveChildren(runMenuPanel.transform);
        runMenuPanel.SetActive(true);
        CreateText(runMenuPanel.transform, "Arena Survival", 32, TextAnchor.MiddleCenter, 52f);
        CreateText(
            runMenuPanel.transform,
            showContinue ? "Continue your saved run or start a new run." : "Start a new procedural run.",
            18,
            TextAnchor.MiddleCenter,
            42f);

        if (showContinue)
        {
            CreateButton(runMenuPanel.transform, "Continue", () => RunManager.EnsureInstance().ContinueRun());
        }

        CreateButton(runMenuPanel.transform, "New Game", () => RunManager.EnsureInstance().BeginNewRun());
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
            CreateButton(shopPanel.transform, label, () => stageManager?.PurchaseUpgrade(capturedUpgrade));
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

    public void HideShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public void HideAllPanels()
    {
        if (runMenuPanel != null)
        {
            runMenuPanel.SetActive(false);
        }

        HideShop();

        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    private void CreateHud()
    {
        if (stageText != null || messageText != null)
        {
            return;
        }

        stageText = CreateFloatingText("Stage HUD", new Vector2(18f, -18f), new Vector2(0f, 1f), TextAnchor.UpperLeft, 24);
        stageText.text = "Stage 1  |  Combat";
        messageText = CreateFloatingText("Stage Message", new Vector2(0f, -18f), new Vector2(0.5f, 1f), TextAnchor.UpperCenter, 22);
    }

    private void EnsureHud()
    {
        if (stageText == null || messageText == null)
        {
            CreateHud();
        }
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

    private void CreateButton(Transform parent, string label, Action onClick)
    {
        GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.48f, 0.8f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());
        buttonObject.GetComponent<LayoutElement>().preferredHeight = 46f;

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
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void RemoveChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
