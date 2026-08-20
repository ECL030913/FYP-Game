using UnityEngine;

/// <summary>
/// Displays the saved run nickname above the player in every gameplay scene.
/// </summary>
public class PlayerNameplate : MonoBehaviour
{
    private TextMesh mainLabel;
    private TextMesh shadowLabel;

    private void Start()
    {
        CreateLabels();
        Refresh();
    }

    public void Refresh()
    {
        CreateLabels();
        string nickname = RunManager.Instance != null
            ? RunManager.Instance.Data.playerNickname
            : "Player";
        nickname = RunManager.NormalizeNickname(nickname);
        mainLabel.text = nickname;
        shadowLabel.text = nickname;
    }

    private void CreateLabels()
    {
        if (mainLabel != null && shadowLabel != null)
        {
            return;
        }

        Font font = PixelUiTheme.DisplayFont;
        shadowLabel = CreateLabel(
            "Nickname Shadow",
            new Vector3(0.025f, 1.085f, 0f),
            new Color(0f, 0f, 0f, 0.9f),
            29,
            font);
        mainLabel = CreateLabel(
            "Nickname",
            new Vector3(0f, 1.11f, 0f),
            PixelUiTheme.TextPrimary,
            30,
            font);
    }

    private TextMesh CreateLabel(
        string objectName,
        Vector3 localPosition,
        Color colour,
        int sortingOrder,
        Font font)
    {
        GameObject labelObject = new GameObject(objectName);
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = localPosition;
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.LowerCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = 0.035f;
        label.fontSize = 38;
        label.color = colour;
        label.richText = false;

        if (font != null)
        {
            label.font = font;
        }

        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            if (font != null)
            {
                renderer.sharedMaterial = font.material;
            }
        }

        return label;
    }
}
