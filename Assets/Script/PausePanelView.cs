using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PausePanelView : MonoBehaviour
{
    [SerializeField] private string contentRootName = "PauseBox";
    [SerializeField] private float overlayFadeDuration = 0.18f;

    [Header("Estilo del menu principal")]
    [Tooltip("Reemplaza la imagen de madera por el panel crema y botones reales con el estilo del menu principal.")]
    [SerializeField] private bool applyMainMenuStyle = true;
    [SerializeField] private string pauseTitle = "Pausa";
    [SerializeField] private Vector2 boxSize = new Vector2(540f, 560f);
    [SerializeField] private Vector2 buttonSize = new Vector2(380f, 96f);

    private const string TitleObjectName = "PauseTitle";
    private bool menuStyleApplied;
    private TMP_Text titleText;

    private UIPanelTransition panelTransition;
    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        ApplyMenuStyle();
        ResolveReferences();
    }

    public void InitializeHidden()
    {
        ApplyMenuStyle();
        ResolveReferences();

        if (panelTransition != null)
        {
            panelTransition.CaptureShownState();
            panelTransition.SetHiddenInstant();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    public void Show()
    {
        ApplyMenuStyle();
        ResolveReferences();
        gameObject.SetActive(true);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(AnimateOverlayAlpha(1f, null));

        if (panelTransition != null)
        {
            panelTransition.PlayShow();
            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void Hide(Action onComplete = null)
    {
        ResolveReferences();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (panelTransition != null)
        {
            panelTransition.PlayHide(() =>
            {
                fadeRoutine = StartCoroutine(AnimateOverlayAlpha(0f, () =>
                {
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                }));
            });
            return;
        }

        fadeRoutine = StartCoroutine(AnimateOverlayAlpha(0f, () =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }));
    }

    /// <summary>Cambia el titulo del menu (por ejemplo al terminar la partida).</summary>
    public void SetTitle(string title)
    {
        ApplyMenuStyle();
        if (titleText != null)
            MenuStyle.StyleTitle(titleText, title);
    }

    /// <summary>
    /// Convierte la caja de pausa al estilo del menu principal: panel crema, titulo multicolor
    /// y botones pildora (Continuar, Reiniciar, Salir al menu). Solo se aplica una vez.
    /// </summary>
    public void ApplyMenuStyle()
    {
        if (!applyMainMenuStyle || menuStyleApplied)
            return;

        RectTransform box = transform.Find(contentRootName) as RectTransform;
        if (box == null)
            return;

        menuStyleApplied = true;

        MenuStyle.PlaceCentered(box, box.anchoredPosition, boxSize);
        MenuStyle.StylePanel(box.GetComponent<Image>());

        float top = boxSize.y * 0.5f;
        titleText = MenuStyle.GetOrCreateText(box, TitleObjectName);
        MenuStyle.PlaceCentered(titleText.rectTransform, new Vector2(0f, top - 80f), new Vector2(boxSize.x - 60f, 80f));
        MenuStyle.StyleTitle(titleText, pauseTitle);

        float firstButtonY = top - 195f;
        float step = buttonSize.y + 20f;
        StylePauseButton(box, "ResumeButton", "Continuar", MenuButtonColor.Green, MenuIcon.Play, firstButtonY);
        StylePauseButton(box, "ResumenButton", "Continuar", MenuButtonColor.Green, MenuIcon.Play, firstButtonY);
        StylePauseButton(box, "RestartButton", "Reiniciar", MenuButtonColor.Yellow, MenuIcon.None, firstButtonY - step);
        StylePauseButton(box, "MenuButton", "Salir al menú", MenuButtonColor.Red, MenuIcon.Exit, firstButtonY - step * 2f);
    }

    private void StylePauseButton(Transform box, string objectName, string label, MenuButtonColor color, MenuIcon icon, float y)
    {
        Transform buttonTransform = box.Find(objectName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button == null)
            return;

        MenuStyle.PlaceCentered(buttonTransform as RectTransform, new Vector2(0f, y), buttonSize);
        MenuStyle.StyleButton(button, label, color, icon);
    }

    private void ResolveReferences()
    {
        if (panelTransition == null)
        {
            Transform contentRoot = transform.Find(contentRootName);
            if (contentRoot != null)
            {
                panelTransition = contentRoot.GetComponent<UIPanelTransition>();
                if (panelTransition == null)
                    panelTransition = contentRoot.gameObject.AddComponent<UIPanelTransition>();
            }

            if (panelTransition == null)
                panelTransition = GetComponent<UIPanelTransition>();
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private IEnumerator AnimateOverlayAlpha(float targetAlpha, Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, overlayFadeDuration);

        canvasGroup.interactable = targetAlpha > 0.01f;
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0.01f;
        canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        fadeRoutine = null;
        onComplete?.Invoke();
    }
}
