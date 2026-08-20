using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared pixel-art visual language for menu and runtime UI. The frame is a
/// deliberately small, code-generated nine-slice: only its narrow border is
/// decorative, so resizing a panel can never stretch a gem into its centre.
/// </summary>
public static class PixelUiTheme
{
    private const string DisplayFontResource = "Fonts/Silkscreen-Regular";
    private const string BodyFontResource = "Fonts/PixelifySans";
    private const int FrameSize = 32;
    private const int FrameBorder = 7;

    public static readonly Color TextPrimary = new Color(0.94f, 0.97f, 1f, 1f);
    public static readonly Color TextMuted = new Color(0.67f, 0.76f, 0.86f, 1f);
    public static readonly Color Cyan = new Color(0.18f, 0.86f, 1f, 1f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.24f, 1f);
    public static readonly Color Health = new Color(0.88f, 0.13f, 0.18f, 1f);
    public static readonly Color Experience = new Color(0.25f, 0.68f, 1f, 1f);

    private static Sprite panelSprite;
    private static Font displayFont;
    private static Font bodyFont;
    private static TMP_FontAsset displayTmpFont;
    private static TMP_FontAsset bodyTmpFont;

    public static Font DisplayFont => LoadFont(ref displayFont, DisplayFontResource);
    public static Font BodyFont => LoadFont(ref bodyFont, BodyFontResource);

    public static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas.pixelPerfect = true;
        }
    }

    public static void StylePanel(Image image, bool innerPanel = false)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetPanelSprite();
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = innerPanel
            ? new Color(0.76f, 0.84f, 0.96f, 0.98f)
            : Color.white;
    }

    public static void StyleButton(Button button, Color accent)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetPanelSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = new Color(0.18f, 0.25f, 0.38f, 1f);

            Outline border = image.GetComponent<Outline>();
            if (border == null)
            {
                border = image.gameObject.AddComponent<Outline>();
            }

            border.effectColor = new Color(accent.r, accent.g, accent.b, 0.72f);
            border.effectDistance = new Vector2(2f, -2f);
            border.useGraphicAlpha = true;
        }

        ColorBlock colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colours.pressedColor = new Color(0.62f, 0.72f, 0.88f, 1f);
        colours.selectedColor = colours.highlightedColor;
        colours.disabledColor = new Color(0.42f, 0.45f, 0.52f, 0.75f);
        colours.colorMultiplier = 1f;
        colours.fadeDuration = 0.06f;
        button.colors = colours;

        Text legacyLabel = button.GetComponentInChildren<Text>(true);
        if (legacyLabel != null)
        {
            StyleText(legacyLabel, legacyLabel.fontSize, TextPrimary, true);
        }

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpLabel != null)
        {
            StyleText(tmpLabel, tmpLabel.fontSize, TextPrimary, true);
        }
    }

    public static void StyleText(Text text, int fontSize, Color colour, bool display = false)
    {
        if (text == null)
        {
            return;
        }

        text.font = display ? DisplayFont : BodyFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.color = colour;
        text.raycastTarget = false;

        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.005f, 0.008f, 0.018f, 0.96f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    public static void StyleText(TMP_Text text, float fontSize, Color colour, bool display = false)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset themedFont = GetTmpFont(display);
        if (themedFont != null)
        {
            text.font = themedFont;
        }

        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Normal;
        text.color = colour;
        text.raycastTarget = false;
        text.outlineColor = new Color32(1, 2, 5, 245);
        text.outlineWidth = 0.14f;
    }

    public static void ApplyMainMenu(
        Button newGame,
        Button continueGame,
        Button quit,
        TMP_Text saveSlot)
    {
        if (newGame == null)
        {
            return;
        }

        Canvas canvas = newGame.GetComponentInParent<Canvas>();
        ConfigureCanvas(canvas);

        Transform menuContainer = newGame.transform.parent;
        if (menuContainer != null)
        {
            Image panel = menuContainer.GetComponent<Image>();
            if (panel == null)
            {
                panel = menuContainer.gameObject.AddComponent<Image>();
            }

            panel.raycastTarget = false;
            StylePanel(panel);

            RectTransform menuRect = menuContainer as RectTransform;
            if (menuRect != null)
            {
                menuRect.sizeDelta = new Vector2(480f, 590f);
            }

            VerticalLayoutGroup layout = menuContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(54, 54, 44, 44);
                layout.spacing = 14f;
            }
        }

        StyleMenuButton(newGame);
        StyleMenuButton(continueGame);
        StyleMenuButton(quit);

        if (saveSlot != null)
        {
            StyleText(saveSlot, 18f, Gold);
        }

        if (canvas == null)
        {
            return;
        }

        TMP_Text[] labels = canvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            switch (label.gameObject.name)
            {
                case "TitleText":
                    StyleText(label, 54f, TextPrimary, true);
                    label.characterSpacing = 1f;
                    break;
                case "SubtitleText":
                    StyleText(label, 17f, Cyan, true);
                    label.characterSpacing = 4f;
                    break;
                case "VersionText":
                    StyleText(label, 14f, TextMuted);
                    break;
            }
        }

        Transform backgroundTransform = canvas.transform.Find("Background");
        Image background = backgroundTransform != null
            ? backgroundTransform.GetComponent<Image>()
            : null;
        if (background != null)
        {
            background.sprite = null;
            background.color = new Color(0.015f, 0.023f, 0.055f, 1f);
        }
    }

    public static Color GetStageAccent(StageType stageType)
    {
        return stageType switch
        {
            StageType.Combat => Cyan,
            StageType.Elite => new Color(1f, 0.25f, 0.2f, 1f),
            StageType.Shop => new Color(0.25f, 1f, 0.62f, 1f),
            StageType.Boss => new Color(0.78f, 0.34f, 1f, 1f),
            StageType.End => Gold,
            _ => Cyan
        };
    }

    private static void StyleMenuButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        StyleButton(button, Cyan);
        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredHeight = 58f;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            StyleText(label, 20f, TextPrimary, true);
            label.characterSpacing = 0.5f;
        }
    }

    private static Font LoadFont(ref Font cachedFont, string resourcePath)
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.Load<Font>(resourcePath);
        if (cachedFont == null)
        {
            Debug.LogWarning($"Pixel UI font is missing at Resources/{resourcePath}.");
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return cachedFont;
    }

    private static TMP_FontAsset GetTmpFont(bool display)
    {
        TMP_FontAsset cached = display ? displayTmpFont : bodyTmpFont;
        if (cached != null)
        {
            return cached;
        }

        Font source = display ? DisplayFont : BodyFont;
        if (source == null)
        {
            return null;
        }

        cached = TMP_FontAsset.CreateFontAsset(source);
        if (cached != null)
        {
            cached.name = display ? "Silkscreen Runtime TMP" : "Pixelify Sans Runtime TMP";
            cached.hideFlags = HideFlags.HideAndDontSave;
        }

        if (display)
        {
            displayTmpFont = cached;
        }
        else
        {
            bodyTmpFont = cached;
        }

        return cached;
    }

    private static Sprite GetPanelSprite()
    {
        if (panelSprite != null)
        {
            return panelSprite;
        }

        Texture2D texture = new Texture2D(FrameSize, FrameSize, TextureFormat.RGBA32, false);
        texture.name = "Pixel UI Frame Texture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32 outer = new Color32(4, 7, 15, 255);
        Color32 shadow = new Color32(16, 27, 46, 250);
        Color32 stone = new Color32(44, 65, 92, 250);
        Color32 highlight = new Color32(70, 116, 150, 255);
        Color32 cyanLine = new Color32(24, 181, 226, 255);
        Color32 centre = new Color32(10, 20, 36, 244);

        for (int y = 0; y < FrameSize; y++)
        {
            for (int x = 0; x < FrameSize; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, FrameSize - 1 - x), Mathf.Min(y, FrameSize - 1 - y));
                Color32 colour;
                if (edge <= 1)
                {
                    colour = outer;
                }
                else if (edge == 2)
                {
                    colour = highlight;
                }
                else if (edge == 3)
                {
                    colour = stone;
                }
                else if (edge == 4)
                {
                    colour = cyanLine;
                }
                else if (edge < FrameBorder)
                {
                    colour = shadow;
                }
                else
                {
                    colour = centre;
                }

                bool cornerRivet = (x == 3 || x == FrameSize - 4)
                    && (y == 3 || y == FrameSize - 4);
                texture.SetPixel(x, y, cornerRivet ? new Color32(155, 225, 255, 255) : colour);
            }
        }

        texture.Apply(false, true);
        panelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, FrameSize, FrameSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(FrameBorder, FrameBorder, FrameBorder, FrameBorder));
        panelSprite.name = "Pixel UI Frame (Runtime)";
        panelSprite.hideFlags = HideFlags.HideAndDontSave;
        return panelSprite;
    }
}
