using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TopHudPanelStyle : MonoBehaviour
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightedColor = new Color(1f, 0.95f, 0.78f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.86f, 0.82f, 0.72f, 1f);
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
    [SerializeField] private float fadeDuration = 0.08f;

    [Header("Estilo del menu principal")]
    [Tooltip("Convierte los marcadores de puntaje y nivel en pildoras con el estilo del menu principal.")]
    [SerializeField] private bool applyMainMenuStyle = true;
    [SerializeField] private MenuButtonColor scoreColor = MenuButtonColor.Blue;
    [SerializeField] private MenuButtonColor levelColor = MenuButtonColor.Purple;

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            StyleButton(buttons[i]);

        if (applyMainMenuStyle)
        {
            StyleBadge("ScoreImagen", "ScoreText", scoreColor);
            StyleBadge("LevelImage", "LevelText", levelColor);
        }
    }

    /// <summary>
    /// Da a un marcador del HUD (puntaje o nivel) la misma pildora de color y el mismo
    /// texto blanco con contorno que los botones del menu principal.
    /// </summary>
    private void StyleBadge(string imageName, string textName, MenuButtonColor color)
    {
        Transform imageTransform = FindChild(transform, imageName);
        Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
        if (image == null)
            return;

        MenuStyle.StylePill(image, color);
        image.raycastTarget = false;

        Transform textTransform = FindChild(transform, textName);
        TMP_Text text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
        if (text == null)
            return;

        // El texto pasa a ser hijo de la pildora para quedar siempre centrado sobre ella.
        RectTransform textRect = text.rectTransform;
        if (textRect.parent != image.rectTransform)
            textRect.SetParent(image.rectTransform, false);

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);
        MenuStyle.StyleWhiteLabel(text, MenuStyle.GetEdgeColor(color), Mathf.Clamp(image.rectTransform.rect.height * 0.42f, 22f, 40f));
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChild(root.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void StyleButton(Button button)
    {
        if (button == null)
            return;

        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = selectedColor;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = fadeDuration;
        button.colors = colors;

        if (button.targetGraphic != null)
            button.targetGraphic.color = button.interactable ? normalColor : disabledColor;

        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();
    }
}
