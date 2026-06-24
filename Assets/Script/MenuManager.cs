using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    private enum MenuPanelState
    {
        MainOptions,
        Games,
        Settings,
    }

    [Header("Animated UI")]
    [Tooltip("Assign the main menu logo RectTransform here. If left empty, the script will try to find an object named 'Logo'.")]
    [SerializeField] private RectTransform logoRect;
    [Tooltip("Assign the Play button RectTransform here. If left empty, the script will try to find an object named 'Play'.")]
    [SerializeField] private RectTransform playButtonRect;
    [Tooltip("Assign the Settings button RectTransform here. If left empty, the script will try to find an object named 'Settings'.")]
    [SerializeField] private RectTransform settingsButtonRect;
    [Tooltip("Assign the Exit button RectTransform here. If left empty, the script will try to find an object named 'Exit'.")]
    [SerializeField] private RectTransform exitButtonRect;

    [Header("Idle Animation")]
    [SerializeField] private bool enableIdleAnimation = true;
    [SerializeField] private float logoFloatAmount = 8f;
    [SerializeField] private float logoFloatSpeed = 0.8f;
    [SerializeField] private float logoWobbleAngle = 2f;
    [SerializeField] private float logoWobbleSpeed = 0.7f;
    [SerializeField] private float buttonPulseScaleAmount = 0.03f;
    [SerializeField] private float buttonPulseSpeed = 0.9f;
    [SerializeField] private float buttonPhaseOffset = 0.45f;

    [Header("Panels")]
    [SerializeField] private GameObject mainOptionsPanel;
    [SerializeField] private GameObject gamesPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Optional")]
    [SerializeField] private Button backButton;

    [Header("Panel Transition Juice")]
    [SerializeField] private float panelAccentDuration = 0.22f;
    [SerializeField] private float logoAccentScale = 1.08f;
    [SerializeField] private float buttonAccentScale = 1.1f;
    [SerializeField] private float logoShiftAmount = 34f;

    private Vector2 logoBasePosition;
    private Quaternion logoBaseRotation;
    private Vector3 logoBaseScale = Vector3.one;
    private Vector3 playBaseScale = Vector3.one;
    private Vector3 settingsBaseScale = Vector3.one;
    private Vector3 exitBaseScale = Vector3.one;
    private UIPanelTransition mainOptionsTransition;
    private UIPanelTransition gamesTransition;
    private UIPanelTransition settingsTransition;
    private Coroutine panelSwitchRoutine;
    private Coroutine menuAccentRoutine;
    private Vector2 logoTransitionOffset = Vector2.zero;
    private float logoTransitionRotation;
    private float logoTransitionScale = 1f;
    private float playTransitionScale = 1f;
    private float settingsTransitionScale = 1f;
    private float exitTransitionScale = 1f;

    private void Awake()
    {
        ResolveReferences();
        ResolvePanelTransitions();
        CacheBaseTransforms();
        InitializePanels();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowMainOptions);
        }
    }

    private void Update()
    {
        if (!enableIdleAnimation)
            return;

        float time = Time.unscaledTime;
        AnimateLogo(time);
        AnimateButton(playButtonRect, playBaseScale, time, 0f);
        AnimateButton(settingsButtonRect, settingsBaseScale, time, buttonPhaseOffset);
        AnimateButton(exitButtonRect, exitBaseScale, time, buttonPhaseOffset * 2f);
    }

    public void LoadScene(string sceneName)
    {
        SceneTransitionController.LoadScene(sceneName);
    }

    public void ShowMainOptions()
    {
        ShowPanel(mainOptionsPanel, mainOptionsTransition, gamesPanel, gamesTransition, settingsPanel, settingsTransition);
        StartMenuAccent(MenuPanelState.MainOptions);
        SetBackButtonVisible(false);
    }

    public void ShowGames()
    {
        ShowPanel(gamesPanel, gamesTransition, mainOptionsPanel, mainOptionsTransition, settingsPanel, settingsTransition);
        StartMenuAccent(MenuPanelState.Games);
        SetBackButtonVisible(true);
    }

    public void ShowSettings()
    {
        ShowPanel(settingsPanel, settingsTransition, mainOptionsPanel, mainOptionsTransition, gamesPanel, gamesTransition);
        StartMenuAccent(MenuPanelState.Settings);
        SetBackButtonVisible(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Salir (en el editor no se cierra, solo en el .exe)");
    }

    private void ResolveReferences()
    {
        if (logoRect == null)
            logoRect = FindRectTransformInScene("Logo");

        if (playButtonRect == null)
            playButtonRect = FindRectTransformInScene("Play");

        if (settingsButtonRect == null)
            settingsButtonRect = FindRectTransformInScene("Settings");

        if (exitButtonRect == null)
            exitButtonRect = FindRectTransformInScene("Exit");

        if (mainOptionsPanel == null)
            mainOptionsPanel = GameObject.Find("MainOptionsPanel");

        if (gamesPanel == null)
            gamesPanel = GameObject.Find("GamesPanel");

        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingsPanel");

        if (backButton == null)
        {
            GameObject backButtonObject = GameObject.Find("BackButton");
            if (backButtonObject != null)
                backButton = backButtonObject.GetComponent<Button>();
        }
    }

    private void SetPanelState(GameObject panel, bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
    }

    private void InitializePanels()
    {
        SetPanelInstant(mainOptionsPanel, mainOptionsTransition, true);
        SetPanelInstant(gamesPanel, gamesTransition, false);
        SetPanelInstant(settingsPanel, settingsTransition, false);
        SetBackButtonVisible(false);
    }

    private void ResolvePanelTransitions()
    {
        mainOptionsTransition = mainOptionsPanel != null ? mainOptionsPanel.GetComponent<UIPanelTransition>() : null;
        gamesTransition = gamesPanel != null ? gamesPanel.GetComponent<UIPanelTransition>() : null;
        settingsTransition = settingsPanel != null ? settingsPanel.GetComponent<UIPanelTransition>() : null;
    }

    private void ShowPanel(GameObject targetPanel, UIPanelTransition targetTransition, GameObject panelToHideA, UIPanelTransition hideTransitionA, GameObject panelToHideB, UIPanelTransition hideTransitionB)
    {
        if (panelSwitchRoutine != null)
            StopCoroutine(panelSwitchRoutine);

        panelSwitchRoutine = StartCoroutine(SwitchPanelsRoutine(targetPanel, targetTransition, panelToHideA, hideTransitionA, panelToHideB, hideTransitionB));
    }

    private IEnumerator SwitchPanelsRoutine(GameObject targetPanel, UIPanelTransition targetTransition, GameObject panelToHideA, UIPanelTransition hideTransitionA, GameObject panelToHideB, UIPanelTransition hideTransitionB)
    {
        HidePanel(panelToHideA, hideTransitionA);
        HidePanel(panelToHideB, hideTransitionB);

        yield return null;

        ShowPanelInternal(targetPanel, targetTransition);
        panelSwitchRoutine = null;
    }

    private void ShowPanelInternal(GameObject panel, UIPanelTransition transition)
    {
        if (panel == null)
            return;

        if (transition != null)
        {
            panel.SetActive(true);
            transition.PlayShow();
        }
        else
        {
            SetPanelState(panel, true);
        }
    }

    private void HidePanel(GameObject panel, UIPanelTransition transition)
    {
        if (panel == null)
            return;

        if (transition != null)
        {
            transition.PlayHide(() =>
            {
                if (panel != null)
                    panel.SetActive(false);
            });
        }
        else
        {
            SetPanelState(panel, false);
        }
    }

    private void SetPanelInstant(GameObject panel, UIPanelTransition transition, bool visible)
    {
        if (panel == null)
            return;

        panel.SetActive(visible);
        if (transition != null)
        {
            if (visible)
                transition.SetShownInstant();
            else
                transition.SetHiddenInstant();
        }
    }

    private void CacheBaseTransforms()
    {
        if (logoRect != null)
        {
            logoBasePosition = logoRect.anchoredPosition;
            logoBaseRotation = logoRect.localRotation;
            logoBaseScale = logoRect.localScale;
        }

        if (playButtonRect != null)
            playBaseScale = playButtonRect.localScale;

        if (settingsButtonRect != null)
            settingsBaseScale = settingsButtonRect.localScale;

        if (exitButtonRect != null)
            exitBaseScale = exitButtonRect.localScale;
    }

    private void AnimateLogo(float time)
    {
        if (logoRect == null)
            return;

        float floatOffset = Mathf.Sin(time * logoFloatSpeed) * logoFloatAmount;
        float wobbleOffset = Mathf.Sin(time * logoWobbleSpeed) * logoWobbleAngle;

        logoRect.anchoredPosition = logoBasePosition + logoTransitionOffset + new Vector2(0f, floatOffset);
        logoRect.localRotation = logoBaseRotation * Quaternion.Euler(0f, 0f, wobbleOffset + logoTransitionRotation);
        logoRect.localScale = logoBaseScale * logoTransitionScale;
    }

    private void AnimateButton(RectTransform buttonRect, Vector3 baseScale, float time, float phase)
    {
        if (buttonRect == null)
            return;

        float pulse = Mathf.Sin((time + phase) * buttonPulseSpeed) * buttonPulseScaleAmount;
        float accentScale = 1f;
        if (buttonRect == playButtonRect)
            accentScale = playTransitionScale;
        else if (buttonRect == settingsButtonRect)
            accentScale = settingsTransitionScale;
        else if (buttonRect == exitButtonRect)
            accentScale = exitTransitionScale;

        buttonRect.localScale = baseScale * (1f + pulse) * accentScale;
    }

    private void StartMenuAccent(MenuPanelState panelState)
    {
        if (menuAccentRoutine != null)
            StopCoroutine(menuAccentRoutine);

        menuAccentRoutine = StartCoroutine(AnimateMenuAccent(panelState));
    }

    private IEnumerator AnimateMenuAccent(MenuPanelState panelState)
    {
        Vector2 targetOffset = panelState switch
        {
            MenuPanelState.Games => new Vector2(-logoShiftAmount, 0f),
            MenuPanelState.Settings => new Vector2(0f, -logoShiftAmount * 0.45f),
            _ => Vector2.zero,
        };

        float targetRotation = panelState switch
        {
            MenuPanelState.Games => -3.5f,
            MenuPanelState.Settings => 2.5f,
            _ => 0f,
        };

        float targetLogoScale = panelState == MenuPanelState.MainOptions ? logoAccentScale : 1.02f;
        float targetPlayScale = panelState == MenuPanelState.MainOptions ? buttonAccentScale : 1f;
        float targetSettingsScale = panelState == MenuPanelState.Settings ? buttonAccentScale : 1f;
        float targetExitScale = panelState == MenuPanelState.MainOptions ? 1.04f : 1f;

        yield return StartCoroutine(AnimateMenuAccentPhase(targetOffset, targetRotation, targetLogoScale, targetPlayScale, targetSettingsScale, targetExitScale));

        yield return StartCoroutine(AnimateMenuAccentPhase(targetOffset * 0.2f, targetRotation * 0.2f, 1f, 1f, 1f, 1f));
        menuAccentRoutine = null;
    }

    private IEnumerator AnimateMenuAccentPhase(Vector2 targetOffset, float targetRotation, float targetLogoScale, float targetPlayScale, float targetSettingsScale, float targetExitScale)
    {
        Vector2 startOffset = logoTransitionOffset;
        float startRotation = logoTransitionRotation;
        float startLogoScale = logoTransitionScale;
        float startPlayScale = playTransitionScale;
        float startSettingsScale = settingsTransitionScale;
        float startExitScale = exitTransitionScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, panelAccentDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            logoTransitionOffset = Vector2.Lerp(startOffset, targetOffset, k);
            logoTransitionRotation = Mathf.Lerp(startRotation, targetRotation, k);
            logoTransitionScale = Mathf.Lerp(startLogoScale, targetLogoScale, k);
            playTransitionScale = Mathf.Lerp(startPlayScale, targetPlayScale, k);
            settingsTransitionScale = Mathf.Lerp(startSettingsScale, targetSettingsScale, k);
            exitTransitionScale = Mathf.Lerp(startExitScale, targetExitScale, k);
            yield return null;
        }

        logoTransitionOffset = targetOffset;
        logoTransitionRotation = targetRotation;
        logoTransitionScale = targetLogoScale;
        playTransitionScale = targetPlayScale;
        settingsTransitionScale = targetSettingsScale;
        exitTransitionScale = targetExitScale;
    }

    private static RectTransform FindRectTransformInScene(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<RectTransform>() : null;
    }

    private void SetBackButtonVisible(bool visible)
    {
        if (backButton != null)
            backButton.gameObject.SetActive(visible);
    }
}
