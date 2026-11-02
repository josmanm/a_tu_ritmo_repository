using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Colores disponibles para los botones con el estilo del menu principal.</summary>
public enum MenuButtonColor
{
    Green,
    Yellow,
    Red,
    Blue,
    Purple,
}

/// <summary>Icono opcional que se muestra a la izquierda del texto del boton.</summary>
public enum MenuIcon
{
    None,
    Play,
    Settings,
    Exit,
}

/// <summary>
/// Aplica el estilo del menu principal (botones pildora de color con borde oscuro,
/// texto blanco grueso con contorno, panel crema y titulos multicolor) a cualquier
/// boton, panel o texto. Todos los menus del juego deben pasar por aqui para verse igual.
/// </summary>
public static class MenuStyle
{
    private const string ThemeResourcePath = "MenuTheme";
    private const string LabelName = "Label";
    private const string IconName = "MenuIcon";

    /// <summary>Texto oscuro usado en los paneles crema (mismo tono del menu de perfiles).</summary>
    public static readonly Color DarkText = new Color(0.094f, 0.188f, 0.31f, 1f);

    /// <summary>Paleta de las letras de colores de los titulos del menu principal.</summary>
    private static readonly Color[] TitlePalette =
    {
        new Color(1f, 0.45f, 0.72f),
        new Color(1f, 0.74f, 0.32f),
        new Color(0.46f, 0.86f, 0.99f),
        new Color(0.56f, 0.94f, 0.62f),
    };

    private static MenuTheme theme;
    private static bool themeLoaded;

    /// <summary>Tema cargado desde Resources/MenuTheme (puede ser null si no existe).</summary>
    public static MenuTheme Theme
    {
        get
        {
            if (!themeLoaded)
            {
                theme = Resources.Load<MenuTheme>(ThemeResourcePath);
                themeLoaded = true;
            }

            return theme;
        }
    }

    /// <summary>Color principal del boton (muestreado de los botones del menu principal).</summary>
    public static Color GetBodyColor(MenuButtonColor color)
    {
        switch (color)
        {
            case MenuButtonColor.Green: return new Color(0.33f, 0.78f, 0.03f, 1f);
            case MenuButtonColor.Yellow: return new Color(0.99f, 0.69f, 0.02f, 1f);
            case MenuButtonColor.Red: return new Color(0.93f, 0.09f, 0.07f, 1f);
            case MenuButtonColor.Blue: return new Color(0.02f, 0.49f, 0.95f, 1f);
            default: return new Color(0.62f, 0.1f, 0.88f, 1f);
        }
    }

    /// <summary>Color oscuro del borde y del contorno del texto de cada boton.</summary>
    public static Color GetEdgeColor(MenuButtonColor color)
    {
        switch (color)
        {
            case MenuButtonColor.Green: return new Color(0.04f, 0.26f, 0f, 1f);
            case MenuButtonColor.Yellow: return new Color(0.5f, 0.2f, 0f, 1f);
            case MenuButtonColor.Red: return new Color(0.35f, 0f, 0f, 1f);
            case MenuButtonColor.Blue: return new Color(0f, 0.18f, 0.43f, 1f);
            default: return new Color(0.18f, 0.07f, 0.28f, 1f);
        }
    }

    /// <summary>
    /// Convierte un boton existente al estilo del menu principal: pildora de color,
    /// borde oscuro, texto blanco en negrita, icono opcional y animacion de pulso.
    /// </summary>
    public static void StyleButton(Button button, string text, MenuButtonColor color, MenuIcon icon = MenuIcon.None)
    {
        if (button == null)
            return;

        RectTransform rect = button.transform as RectTransform;
        float height = rect != null && rect.rect.height > 1f ? rect.rect.height : rect != null ? Mathf.Max(rect.sizeDelta.y, 60f) : 80f;
        Color edge = GetEdgeColor(color);

        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.gameObject.AddComponent<Image>();

        StylePill(image, color, height);
        image.raycastTarget = true;
        button.targetGraphic = image;
        ConfigureButtonColors(button);

        Image iconImage = SetIcon(button.transform, icon, height);
        float leftPadding = iconImage != null ? height * 0.95f : 20f;

        TMP_Text label = GetOrCreateLabel(button.transform);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = new Vector2(leftPadding, 6f);
        labelRect.offsetMax = new Vector2(-20f, -6f);

        label.text = text;
        StyleWhiteLabel(label, edge, Mathf.Clamp(height * 0.42f, 22f, 46f));

        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();
    }

    /// <summary>
    /// Pildora de color con borde oscuro, sin comportamiento de boton.
    /// Sirve para marcadores (puntaje, nivel) y fondos decorativos.
    /// </summary>
    public static void StylePill(Image image, MenuButtonColor color, float height = 0f)
    {
        if (image == null)
            return;

        if (height <= 1f)
            height = image.rectTransform.rect.height > 1f ? image.rectTransform.rect.height : Mathf.Max(image.rectTransform.sizeDelta.y, 60f);

        ApplyButtonSprite(image, height);
        image.color = GetBodyColor(color);

        Outline outline = image.GetComponent<Outline>();
        if (outline == null)
            outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = GetEdgeColor(color);
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = true;
    }

    /// <summary>Texto blanco en negrita con contorno oscuro, igual que las letras de los botones.</summary>
    public static void StyleWhiteLabel(TMP_Text label, Color outlineColor, float maxSize)
    {
        if (label == null)
            return;

        label.enableVertexGradient = false;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Min(18f, maxSize);
        label.fontSizeMax = maxSize;
        label.fontSize = maxSize;
        label.characterSpacing = 2f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.outlineColor = outlineColor;
        label.outlineWidth = 0.22f;
    }

    /// <summary>Panel crema redondeado (el mismo fondo del menu de perfiles).</summary>
    public static void StylePanel(Image image, float cornerSize = 56f)
    {
        if (image == null)
            return;

        MenuTheme currentTheme = Theme;
        Sprite sprite = currentTheme != null ? currentTheme.panelSprite : null;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            if (sprite.border.sqrMagnitude > 0f)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, sprite.border.x / Mathf.Max(1f, cornerSize));
            }
            else
            {
                image.type = Image.Type.Simple;
            }
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 0.95f, 0.8f, 1f);
        }
    }

    /// <summary>Titulo con letras de colores y contorno oscuro, como "Configuracion" en el menu principal.</summary>
    public static void StyleTitle(TMP_Text title, string text, float maxSize = 54f)
    {
        if (title == null)
            return;

        title.richText = true;
        title.text = FormatTwoLetterColorGroups(text);
        title.enableVertexGradient = false;
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = Mathf.Min(26f, maxSize);
        title.fontSizeMax = maxSize;
        title.fontSize = maxSize;
        title.characterSpacing = 3f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.raycastTarget = false;
        title.outlineColor = DarkText;
        title.outlineWidth = 0.28f;
    }

    /// <summary>Colorea el texto en grupos de dos letras con la paleta del menu principal.</summary>
    public static string FormatTwoLetterColorGroups(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder builder = new StringBuilder(text.Length * 20);
        int paletteIndex = 0;

        for (int i = 0; i < text.Length; i += 2)
        {
            int length = Mathf.Min(2, text.Length - i);
            string group = text.Substring(i, length);
            if (string.IsNullOrWhiteSpace(group))
            {
                builder.Append(group);
                continue;
            }

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGB(TitlePalette[paletteIndex % TitlePalette.Length]));
            builder.Append('>');
            builder.Append(group);
            builder.Append("</color>");
            paletteIndex++;
        }

        return builder.ToString();
    }

    /// <summary>Crea (o reutiliza) un texto hijo con el nombre indicado.</summary>
    public static TMP_Text GetOrCreateText(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            TMP_Text existingText = existing.GetComponent<TMP_Text>();
            if (existingText != null)
                return existingText;
        }

        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return textObject.GetComponent<TMP_Text>();
    }

    /// <summary>Coloca un RectTransform centrado con la posicion y el tamano indicados.</summary>
    public static void PlaceCentered(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void ApplyButtonSprite(Image image, float height)
    {
        MenuTheme currentTheme = Theme;
        Sprite sprite = currentTheme != null ? currentTheme.buttonSprite : null;
        if (sprite == null)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = false;
        if (sprite.border.sqrMagnitude > 0f)
        {
            // Mantiene las puntas redondas sin importar el alto del boton.
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, sprite.rect.height / Mathf.Max(1f, height));
        }
        else
        {
            image.type = Image.Type.Simple;
        }
    }

    private static void ConfigureButtonColors(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static Image SetIcon(Transform buttonTransform, MenuIcon icon, float height)
    {
        Transform existing = buttonTransform.Find(IconName);
        Sprite sprite = GetIconSprite(icon);

        if (sprite == null)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return null;
        }

        Image iconImage;
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            iconImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject iconObject = new GameObject(IconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(buttonTransform, false);
            iconImage = iconObject.GetComponent<Image>();
        }

        RectTransform rect = iconImage.rectTransform;
        float iconHeight = height * 0.5f;
        float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(height * 0.55f, 0f);
        rect.sizeDelta = new Vector2(iconHeight * aspect, iconHeight);

        iconImage.sprite = sprite;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
        return iconImage;
    }

    private static Sprite GetIconSprite(MenuIcon icon)
    {
        MenuTheme currentTheme = Theme;
        if (currentTheme == null)
            return null;

        switch (icon)
        {
            case MenuIcon.Play: return currentTheme.playIcon;
            case MenuIcon.Settings: return currentTheme.settingsIcon;
            case MenuIcon.Exit: return currentTheme.exitIcon;
            default: return null;
        }
    }

    private static TMP_Text GetOrCreateLabel(Transform buttonTransform)
    {
        TMP_Text[] labels = buttonTransform.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].transform.parent == buttonTransform)
            {
                labels[i].gameObject.SetActive(true);
                return labels[i];
            }
        }

        GameObject labelObject = new GameObject(LabelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonTransform, false);
        return labelObject.GetComponent<TMP_Text>();
    }
}
