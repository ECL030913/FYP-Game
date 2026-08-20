using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built pixel UI for configuring a new run and reading the reusable
/// user guide. Keeping it under the existing Menu canvas avoids scene-specific
/// duplicate logic while still using the project's visual theme.
/// </summary>
public class MainMenuOverlayUi : MonoBehaviour
{
    private readonly Dictionary<GameDifficulty, Button> difficultyButtons =
        new Dictionary<GameDifficulty, Button>();

    private Action<string, GameDifficulty> startRequested;
    private Action setupCancelled;
    private Action guideClosed;
    private GameObject setupOverlay;
    private GameObject guideOverlay;
    private TMP_InputField nicknameInput;
    private TMP_Text difficultyDetails;
    private TMP_Text validationText;
    private TMP_Text guideTitle;
    private TMP_Text guideBody;
    private TMP_Text guideCounter;
    private Button guideBackButton;
    private Button guideNextButton;
    private GameDifficulty selectedDifficulty = GameDifficulty.Normal;
    private IReadOnlyList<GuidePage> guidePages;
    private int guidePageIndex;
    private bool returnToSetupAfterGuide;

    public static MainMenuOverlayUi Create(
        Canvas canvas,
        Action<string, GameDifficulty> onStartRequested,
        Action onSetupCancelled)
    {
        if (canvas == null)
        {
            return null;
        }

        GameObject host = new GameObject("Main Menu Overlays", typeof(RectTransform));
        host.transform.SetParent(canvas.transform, false);
        RectTransform hostRect = host.GetComponent<RectTransform>();
        Stretch(hostRect);

        MainMenuOverlayUi ui = host.AddComponent<MainMenuOverlayUi>();
        ui.startRequested = onStartRequested;
        ui.setupCancelled = onSetupCancelled;
        ui.BuildSetupOverlay();
        ui.BuildGuideOverlay();
        ui.HideAll();
        return ui;
    }

    public void ShowNewRunSetup(bool hasSavedRun)
    {
        guideOverlay.SetActive(false);
        setupOverlay.SetActive(true);
        selectedDifficulty = GameDifficulty.Normal;
        nicknameInput.text = "Player";
        validationText.text = hasSavedRun
            ? "Starting a new run will replace the current save."
            : "Choose a nickname and difficulty before starting.";
        validationText.color = hasSavedRun ? PixelUiTheme.Gold : PixelUiTheme.TextMuted;
        RefreshDifficultyPresentation();
        nicknameInput.Select();
        nicknameInput.ActivateInputField();
    }

    public void ShowUserGuide(Action onClosed)
    {
        guideClosed = onClosed;
        returnToSetupAfterGuide = setupOverlay.activeSelf;
        setupOverlay.SetActive(false);
        guideOverlay.SetActive(true);
        guidePages = GameGuidanceCatalog.GetUserGuidePages();
        guidePageIndex = 0;
        RefreshGuidePage();
    }

    public void HideAll()
    {
        setupOverlay?.SetActive(false);
        guideOverlay?.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (guideOverlay != null && guideOverlay.activeSelf)
            {
                CloseGuide();
            }
            else if (setupOverlay != null && setupOverlay.activeSelf)
            {
                CancelSetup();
            }
        }
    }

    private void BuildSetupOverlay()
    {
        setupOverlay = CreateOverlay("New Run Setup Overlay");
        GameObject panel = CreatePanel(setupOverlay.transform, "New Run Setup", new Vector2(900f, 720f));
        ConfigureVerticalLayout(panel, new RectOffset(34, 34, 28, 28), 10f);

        CreateText(panel.transform, "NEW RUN SETUP", 32f, 50f, PixelUiTheme.Cyan, true);
        CreateText(
            panel.transform,
            "Enter a nickname and select the experience that matches your skill level.",
            18f,
            42f,
            PixelUiTheme.TextMuted);
        CreateText(panel.transform, "NICKNAME", 18f, 30f, PixelUiTheme.Gold, true, TextAlignmentOptions.Left);
        nicknameInput = CreateNicknameInput(panel.transform);
        CreateText(panel.transform, "DIFFICULTY", 18f, 30f, PixelUiTheme.Gold, true, TextAlignmentOptions.Left);

        GameObject difficultyRow = new GameObject(
            "Difficulty Choices",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        difficultyRow.transform.SetParent(panel.transform, false);
        difficultyRow.GetComponent<LayoutElement>().preferredHeight = 112f;
        HorizontalLayoutGroup rowLayout = difficultyRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;

        foreach (GameDifficulty difficulty in DifficultyCatalog.DisplayOrder)
        {
            DifficultyDefinition definition = DifficultyCatalog.Get(difficulty);
            Button button = CreateButton(
                difficultyRow.transform,
                $"{definition.DisplayName}\n{definition.PlayerLabel}",
                () => SelectDifficulty(difficulty),
                104f,
                18f);
            difficultyButtons[difficulty] = button;
        }

        difficultyDetails = CreateText(
            panel.transform,
            string.Empty,
            17f,
            118f,
            PixelUiTheme.TextPrimary,
            false,
            TextAlignmentOptions.Center);
        validationText = CreateText(
            panel.transform,
            string.Empty,
            16f,
            34f,
            PixelUiTheme.TextMuted);

        GameObject actionRow = new GameObject(
            "Actions",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        actionRow.transform.SetParent(panel.transform, false);
        actionRow.GetComponent<LayoutElement>().preferredHeight = 58f;
        HorizontalLayoutGroup actionLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 12f;
        actionLayout.childForceExpandWidth = true;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;

        CreateButton(actionRow.transform, "HOW TO PLAY", () => ShowUserGuide(null), 54f, 16f);
        CreateButton(actionRow.transform, "BACK", CancelSetup, 54f, 16f);
        CreateButton(actionRow.transform, "START RUN", ConfirmSetup, 54f, 16f);
    }

    private void BuildGuideOverlay()
    {
        guideOverlay = CreateOverlay("User Guide Overlay");
        GameObject panel = CreatePanel(guideOverlay.transform, "User Guide", new Vector2(820f, 650f));
        ConfigureVerticalLayout(panel, new RectOffset(40, 40, 32, 32), 14f);

        guideTitle = CreateText(panel.transform, "USER GUIDE", 32f, 54f, PixelUiTheme.Cyan, true);
        guideBody = CreateText(
            panel.transform,
            string.Empty,
            20f,
            390f,
            PixelUiTheme.TextPrimary,
            false,
            TextAlignmentOptions.TopLeft);
        guideBody.textWrappingMode = TextWrappingModes.Normal;
        guideCounter = CreateText(panel.transform, "1 / 4", 16f, 30f, PixelUiTheme.TextMuted);

        GameObject navigation = new GameObject(
            "Navigation",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        navigation.transform.SetParent(panel.transform, false);
        navigation.GetComponent<LayoutElement>().preferredHeight = 58f;
        HorizontalLayoutGroup layout = navigation.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        guideBackButton = CreateButton(navigation.transform, "BACK", PreviousGuidePage, 54f, 17f);
        CreateButton(navigation.transform, "CLOSE", CloseGuide, 54f, 17f);
        guideNextButton = CreateButton(navigation.transform, "NEXT", NextGuidePage, 54f, 17f);
    }

    private void ConfirmSetup()
    {
        string rawNickname = nicknameInput.text?.Trim();
        if (string.IsNullOrWhiteSpace(rawNickname))
        {
            validationText.text = "Nickname cannot be empty.";
            validationText.color = new Color(1f, 0.3f, 0.25f);
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
            return;
        }

        string nickname = RunManager.NormalizeNickname(rawNickname);
        setupOverlay.SetActive(false);
        startRequested?.Invoke(nickname, selectedDifficulty);
    }

    private void CancelSetup()
    {
        setupOverlay.SetActive(false);
        setupCancelled?.Invoke();
    }

    private void SelectDifficulty(GameDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        RefreshDifficultyPresentation();
    }

    private void RefreshDifficultyPresentation()
    {
        DifficultyDefinition selected = DifficultyCatalog.Get(selectedDifficulty);
        difficultyDetails.text = $"{selected.Description}\n{selected.GetMultiplierSummary()}";

        foreach (KeyValuePair<GameDifficulty, Button> pair in difficultyButtons)
        {
            PixelUiTheme.StyleButton(
                pair.Value,
                pair.Key == selectedDifficulty ? PixelUiTheme.Gold : PixelUiTheme.Cyan);
        }
    }

    private void PreviousGuidePage()
    {
        if (guidePages == null || guidePages.Count == 0)
        {
            return;
        }

        guidePageIndex = Mathf.Max(0, guidePageIndex - 1);
        RefreshGuidePage();
    }

    private void NextGuidePage()
    {
        if (guidePages == null || guidePages.Count == 0)
        {
            return;
        }

        if (guidePageIndex >= guidePages.Count - 1)
        {
            CloseGuide();
            return;
        }

        guidePageIndex++;
        RefreshGuidePage();
    }

    private void RefreshGuidePage()
    {
        if (guidePages == null || guidePages.Count == 0)
        {
            return;
        }

        GuidePage page = guidePages[guidePageIndex];
        guideTitle.text = page.Title;
        guideBody.text = page.Body;
        guideCounter.text = $"{guidePageIndex + 1} / {guidePages.Count}";
        if (guideBackButton != null)
        {
            guideBackButton.interactable = guidePageIndex > 0;
        }

        TMP_Text nextLabel = guideNextButton != null
            ? guideNextButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (nextLabel != null)
        {
            nextLabel.text = guidePageIndex >= guidePages.Count - 1 ? "FINISH" : "NEXT";
        }
    }

    private void CloseGuide()
    {
        guideOverlay.SetActive(false);
        if (returnToSetupAfterGuide)
        {
            setupOverlay.SetActive(true);
            returnToSetupAfterGuide = false;
            return;
        }

        Action callback = guideClosed;
        guideClosed = null;
        callback?.Invoke();
    }

    private GameObject CreateOverlay(string name)
    {
        GameObject overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        Image image = overlay.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);
        image.raycastTarget = true;
        return overlay;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        PixelUiTheme.StylePanel(panel.GetComponent<Image>());
        return panel;
    }

    private static void ConfigureVerticalLayout(GameObject panel, RectOffset padding, float spacing)
    {
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string value,
        float fontSize,
        float height,
        Color colour,
        bool display = false,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        PixelUiTheme.StyleText(text, fontSize, colour, display);
        textObject.GetComponent<LayoutElement>().preferredHeight = height;
        return text;
    }

    private static TMP_InputField CreateNicknameInput(Transform parent)
    {
        GameObject fieldObject = new GameObject(
            "Nickname Input",
            typeof(RectTransform),
            typeof(Image),
            typeof(TMP_InputField),
            typeof(LayoutElement));
        fieldObject.transform.SetParent(parent, false);
        fieldObject.GetComponent<LayoutElement>().preferredHeight = 58f;
        PixelUiTheme.StylePanel(fieldObject.GetComponent<Image>(), true);

        GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform));
        viewportObject.transform.SetParent(fieldObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(18f, 6f);
        viewport.offsetMax = new Vector2(-18f, -6f);

        TMP_Text placeholder = CreateInputText(viewportObject.transform, "Enter 1-12 characters", PixelUiTheme.TextMuted);
        TMP_Text valueText = CreateInputText(viewportObject.transform, string.Empty, PixelUiTheme.TextPrimary);

        TMP_InputField input = fieldObject.GetComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = valueText;
        input.placeholder = placeholder;
        input.characterLimit = 12;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Custom;
        input.caretColor = PixelUiTheme.Cyan;
        input.selectionColor = new Color(PixelUiTheme.Cyan.r, PixelUiTheme.Cyan.g, PixelUiTheme.Cyan.b, 0.45f);
        input.onValidateInput += ValidateNicknameCharacter;
        return input;
    }

    private static TMP_Text CreateInputText(Transform parent, string value, Color colour)
    {
        GameObject textObject = new GameObject("Input Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        PixelUiTheme.StyleText(text, 20f, colour);
        return text;
    }

    private static char ValidateNicknameCharacter(string text, int index, char addedChar)
    {
        bool asciiLetterOrDigit = addedChar <= 127 && char.IsLetterOrDigit(addedChar);
        return asciiLetterOrDigit || addedChar == ' ' || addedChar == '_'
            ? addedChar
            : '\0';
    }

    private static Button CreateButton(
        Transform parent,
        string label,
        Action onClick,
        float height,
        float fontSize)
    {
        GameObject buttonObject = new GameObject(
            label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<LayoutElement>().preferredHeight = height;
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => onClick?.Invoke());

        TMP_Text text = CreateText(
            buttonObject.transform,
            label,
            fontSize,
            height,
            PixelUiTheme.TextPrimary,
            true);
        RectTransform textRect = text.rectTransform;
        Stretch(textRect);
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        text.GetComponent<LayoutElement>().ignoreLayout = true;
        PixelUiTheme.StyleButton(button, PixelUiTheme.Cyan);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
