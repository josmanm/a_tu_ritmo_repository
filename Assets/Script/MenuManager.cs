using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
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

    private Vector2 logoBasePosition;
    private Quaternion logoBaseRotation;
    private Vector3 playBaseScale = Vector3.one;
    private Vector3 settingsBaseScale = Vector3.one;
    private Vector3 exitBaseScale = Vector3.one;

    private void Awake()
    {
        ResolveReferences();
        CacheBaseTransforms();
        ShowMainOptions();

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
        SceneManager.LoadScene(sceneName);
    }

    public void ShowMainOptions()
    {
        SetPanelState(mainOptionsPanel, true);
        SetPanelState(gamesPanel, false);
        SetPanelState(settingsPanel, false);
    }

    public void ShowGames()
    {
        SetPanelState(mainOptionsPanel, false);
        SetPanelState(gamesPanel, true);
        SetPanelState(settingsPanel, false);
    }

    public void ShowSettings()
    {
        SetPanelState(mainOptionsPanel, false);
        SetPanelState(gamesPanel, false);
        SetPanelState(settingsPanel, true);
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

    private void CacheBaseTransforms()
    {
        if (logoRect != null)
        {
            logoBasePosition = logoRect.anchoredPosition;
            logoBaseRotation = logoRect.localRotation;
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

        logoRect.anchoredPosition = logoBasePosition + new Vector2(0f, floatOffset);
        logoRect.localRotation = logoBaseRotation * Quaternion.Euler(0f, 0f, wobbleOffset);
    }

    private void AnimateButton(RectTransform buttonRect, Vector3 baseScale, float time, float phase)
    {
        if (buttonRect == null)
            return;

        float pulse = Mathf.Sin((time + phase) * buttonPulseSpeed) * buttonPulseScaleAmount;
        buttonRect.localScale = baseScale * (1f + pulse);
    }

    private static RectTransform FindRectTransformInScene(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<RectTransform>() : null;
    }
}
