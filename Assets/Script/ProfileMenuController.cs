using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileMenuController : MonoBehaviour
{
    private class AvatarVisual
    {
        public GameObject root;
        public Image image;
        public TMP_Text fallbackText;
    }

    public static ProfileMenuController Instance { get; private set; }

    [SerializeField] private string defaultAvatar = "conejo_azul";

    private Canvas rootCanvas;
    private GameObject overlayRoot;
    private CanvasGroup overlayCanvasGroup;
    private TMP_InputField nameInput;
    private TMP_Dropdown avatarDropdown;
    private TMP_Text selectedProfileLabel;
    private TMP_Text helperText;
    private ScrollRect profilesScrollRect;
    private RectTransform profilesViewport;
    private RectTransform profilesContent;
    private Button continueButton;
    private Button createButton;
    private Button refreshButton;
    private AvatarVisual selectedProfileAvatar;
    private readonly List<PlayerProfileData> loadedProfiles = new List<PlayerProfileData>();
    private bool waitingForFirebase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (rootCanvas == null)
            return;

        EnsureUi();
        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.ActiveProfileChanged += HandleActiveProfileChanged;

        RefreshActiveProfileDisplay();
        Show();
    }

    private void OnDestroy()
    {
        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.ActiveProfileChanged -= HandleActiveProfileChanged;
    }

    public void Show()
    {
        EnsureUi();
        if (overlayRoot == null)
            return;

        overlayRoot.SetActive(true);
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;
        }

        RefreshActiveProfileDisplay();
        RefreshContinueButton();
        LoadProfilesOrWaitForFirebase();
    }

    public void Hide()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void EnsureUi()
    {
        if (overlayRoot != null)
            return;

        overlayRoot = new GameObject("ProfileOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(rootCanvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        Image overlayImage = overlayRoot.GetComponent<Image>();
        overlayImage.color = new Color(0.07f, 0.09f, 0.14f, 0.82f);
        overlayImage.raycastTarget = true;
        overlayCanvasGroup.alpha = 1f;
        overlayCanvasGroup.interactable = true;
        overlayCanvasGroup.blocksRaycasts = true;

        GameObject panel = CreateUiObject("ProfilePanel", overlayRoot.transform, new Vector2(720f, 760f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.98f, 0.98f, 1f, 0.98f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        TMP_Text title = CreateText("Selecciona tu perfil", panel.transform, 42f, FontStyles.Bold);
        title.rectTransform.anchoredPosition = new Vector2(0f, 320f);
        title.rectTransform.sizeDelta = new Vector2(620f, 70f);
        title.color = new Color(0.15f, 0.18f, 0.28f, 1f);

        helperText = CreateText("Elige un perfil para comenzar o crea uno nuevo", panel.transform, 24f, FontStyles.Normal);
        helperText.rectTransform.anchoredPosition = new Vector2(0f, 280f);
        helperText.rectTransform.sizeDelta = new Vector2(620f, 50f);
        helperText.color = new Color(0.37f, 0.42f, 0.56f, 1f);

        selectedProfileAvatar = CreateAvatarVisual(panel.transform, new Vector2(-240f, 220f), 96f);
        selectedProfileLabel = CreateText("Selecciona o crea un perfil", panel.transform, 28f, FontStyles.Bold);
        selectedProfileLabel.rectTransform.anchoredPosition = new Vector2(60f, 220f);
        selectedProfileLabel.rectTransform.sizeDelta = new Vector2(470f, 60f);
        selectedProfileLabel.alignment = TextAlignmentOptions.Left;
        selectedProfileLabel.color = new Color(0.2f, 0.45f, 0.78f, 1f);

        CreateProfilesScrollArea(panel.transform);

        nameInput = CreateInputField(panel.transform, new Vector2(0f, -132f), new Vector2(520f, 72f), "Nombre del jugador");
        avatarDropdown = CreateDropdown(panel.transform, new Vector2(0f, -218f), new Vector2(520f, 64f));
        avatarDropdown.ClearOptions();
        avatarDropdown.AddOptions(new List<string>(PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.GetAvatarOptions() : new[] { defaultAvatar }));

        createButton = CreateButton(panel.transform, "Crear", new Vector2(-130f, -312f), new Vector2(210f, 68f), OnCreateProfileClicked, new Color(0.28f, 0.72f, 0.42f, 0.98f));
        refreshButton = CreateButton(panel.transform, "Actualizar", new Vector2(130f, -312f), new Vector2(210f, 68f), LoadProfilesOrWaitForFirebase, new Color(0.29f, 0.57f, 0.9f, 0.98f));
        continueButton = CreateButton(panel.transform, "Continuar", new Vector2(0f, -402f), new Vector2(300f, 76f), OnContinueClicked, new Color(0.96f, 0.71f, 0.21f, 0.98f));
    }

    private void CreateProfilesScrollArea(Transform parent)
    {
        GameObject scrollArea = CreateUiObject("ProfilesScrollArea", parent, new Vector2(620f, 240f));
        RectTransform scrollRectTransform = scrollArea.GetComponent<RectTransform>();
        scrollRectTransform.anchoredPosition = new Vector2(0f, 68f);
        Image scrollImage = scrollArea.AddComponent<Image>();
        scrollImage.color = new Color(0.92f, 0.95f, 1f, 0.95f);
        profilesScrollRect = scrollArea.AddComponent<ScrollRect>();
        profilesScrollRect.horizontal = false;
        profilesScrollRect.movementType = ScrollRect.MovementType.Clamped;
        profilesScrollRect.scrollSensitivity = 24f;

        GameObject viewport = CreateUiObject("Viewport", scrollArea.transform, new Vector2(600f, 220f));
        profilesViewport = viewport.GetComponent<RectTransform>();
        profilesViewport.anchorMin = Vector2.zero;
        profilesViewport.anchorMax = Vector2.one;
        profilesViewport.offsetMin = new Vector2(12f, 12f);
        profilesViewport.offsetMax = new Vector2(-12f, -12f);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        RectMask2D rectMask = viewport.AddComponent<RectMask2D>();
        rectMask.padding = Vector4.zero;

        GameObject content = CreateUiObject("Content", viewport.transform, new Vector2(560f, 220f));
        profilesContent = content.GetComponent<RectTransform>();
        profilesContent.anchorMin = new Vector2(0f, 1f);
        profilesContent.anchorMax = new Vector2(1f, 1f);
        profilesContent.pivot = new Vector2(0.5f, 1f);
        profilesContent.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        profilesScrollRect.viewport = profilesViewport;
        profilesScrollRect.content = profilesContent;
    }

    private void LoadProfilesOrWaitForFirebase()
    {
        FirebaseManager firebaseManager = FirebaseManager.Instance;
        if (firebaseManager == null || !firebaseManager.IsReady)
        {
            waitingForFirebase = true;
            CreateInfoLabel("Conectando perfiles...");
            RefreshButtonState(false);

            if (firebaseManager != null)
            {
                firebaseManager.Ready -= HandleFirebaseReady;
                firebaseManager.Ready += HandleFirebaseReady;
            }

            return;
        }

        RefreshButtonState(true);
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        waitingForFirebase = false;
        ClearProfilesUi();
        PlayerProfileManager.Instance.LoadProfiles(profiles =>
        {
            loadedProfiles.Clear();
            loadedProfiles.AddRange(profiles);
            if (profiles.Count == 0)
            {
                CreateInfoLabel("No hay perfiles creados todavia");
                RebuildProfilesLayout();
                return;
            }

            for (int i = 0; i < profiles.Count; i++)
                CreateProfileEntry(profiles[i]);

            RebuildProfilesLayout();
        }, error =>
        {
            CreateInfoLabel(error);
        });
    }

    private void OnCreateProfileClicked()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        string avatar = avatarDropdown != null && avatarDropdown.options.Count > 0
            ? avatarDropdown.options[avatarDropdown.value].text
            : defaultAvatar;

        PlayerProfileManager.Instance.CreateProfile(nameInput != null ? nameInput.text : string.Empty, avatar, profile =>
        {
            StartSession(profile);
            RefreshActiveProfileDisplay();
            Hide();
        }, CreateInfoLabel);
    }

    private void OnContinueClicked()
    {
        if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile)
        {
            StartSession(PlayerProfileManager.Instance.ActiveProfile);
            Hide();
            return;
        }

        CreateInfoLabel("Selecciona o crea un perfil.");
    }

    private void CreateProfileEntry(PlayerProfileData profile)
    {
        GameObject row = CreateUiObject(profile.playerId, profilesContent, new Vector2(520f, 72f));
        Image rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(1f, 1f, 1f, 0.92f);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 72f;
        rowLayout.minHeight = 72f;
        rowLayout.preferredWidth = 520f;
        Button button = row.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (PlayerProfileManager.Instance != null)
                PlayerProfileManager.Instance.SetActiveProfile(profile);
            RefreshActiveProfileDisplay();
            StartSession(profile);
            Hide();
        });
        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();

        AvatarVisual avatar = CreateAvatarVisual(row.transform, new Vector2(-210f, 0f), 58f);
        RefreshAvatarVisual(avatar, profile.name, profile.avatar);

        TMP_Text text = CreateText(profile.name + "  •  " + profile.avatar, row.transform, 26f, FontStyles.Bold);
        text.rectTransform.anchoredPosition = new Vector2(42f, 0f);
        text.rectTransform.sizeDelta = new Vector2(380f, 52f);
        text.alignment = TextAlignmentOptions.Left;
        if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile && PlayerProfileManager.Instance.ActiveProfile.playerId == profile.playerId)
            text.color = new Color(0.18f, 0.56f, 0.91f, 1f);
    }

    private void StartSession(PlayerProfileData profile)
    {
        if (SessionManager.Instance == null || profile == null)
            return;

        SessionManager.Instance.StartSessionForPlayer(profile, null, CreateInfoLabel);
    }

    private void RefreshActiveProfileDisplay()
    {
        if (selectedProfileLabel == null)
            return;

        if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile)
        {
            selectedProfileLabel.text = "Perfil activo: " + PlayerProfileManager.Instance.ActiveProfile.name;
            RefreshAvatarVisual(selectedProfileAvatar, PlayerProfileManager.Instance.ActiveProfile.name, PlayerProfileManager.Instance.ActiveProfile.avatar);
        }
        else
        {
            selectedProfileLabel.text = "Selecciona o crea un perfil";
            RefreshAvatarVisual(selectedProfileAvatar, string.Empty, string.Empty);
        }

        RefreshContinueButton();
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        bool canContinue = PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile && !waitingForFirebase;
        continueButton.interactable = canContinue;
        Image image = continueButton.GetComponent<Image>();
        if (image != null)
            image.color = canContinue ? new Color(0.96f, 0.71f, 0.21f, 0.98f) : new Color(0.78f, 0.78f, 0.78f, 0.85f);
    }

    private void RefreshButtonState(bool firebaseReady)
    {
        if (createButton != null)
            createButton.interactable = firebaseReady;

        if (refreshButton != null)
            refreshButton.interactable = firebaseReady;

        RefreshContinueButton();
    }

    private void HandleActiveProfileChanged(PlayerProfileData profile)
    {
        RefreshActiveProfileDisplay();
        LoadProfilesOrWaitForFirebase();
    }

    private void HandleFirebaseReady()
    {
        if (FirebaseManager.Instance != null)
            FirebaseManager.Instance.Ready -= HandleFirebaseReady;

        if (overlayRoot != null && overlayRoot.activeSelf)
            LoadProfilesOrWaitForFirebase();
    }

    private void ClearProfilesUi()
    {
        if (profilesContent == null)
            return;

        for (int i = profilesContent.childCount - 1; i >= 0; i--)
            Destroy(profilesContent.GetChild(i).gameObject);
    }

    private void CreateInfoLabel(string message)
    {
        if (profilesContent == null)
            return;

        ClearProfilesUi();
        TMP_Text text = CreateText(message, profilesContent, 24f, FontStyles.Bold);
        text.rectTransform.sizeDelta = new Vector2(520f, 56f);
        text.color = waitingForFirebase ? new Color(0.2f, 0.45f, 0.78f, 1f) : new Color(0.7f, 0.2f, 0.2f, 1f);
        LayoutElement rowLayout = text.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 56f;
        rowLayout.minHeight = 56f;
        RebuildProfilesLayout();
    }

    private void RefreshAvatarVisual(AvatarVisual avatarVisual, string playerName, string avatarName)
    {
        if (avatarVisual == null)
            return;

        avatarVisual.image.sprite = null;
        avatarVisual.image.color = new Color(0.25f, 0.58f, 0.88f, 0.95f);
        avatarVisual.fallbackText.text = string.IsNullOrWhiteSpace(playerName) ? "?" : playerName.Substring(0, 1).ToUpperInvariant();
        avatarVisual.fallbackText.gameObject.SetActive(true);
    }

    private static GameObject CreateUiObject(string name, Transform parent, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return obj;
    }

    private void RebuildProfilesLayout()
    {
        if (profilesContent == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(profilesContent);
        if (profilesScrollRect != null)
            profilesScrollRect.verticalNormalizedPosition = 1f;
    }

    private static TMP_Text CreateText(string value, Transform parent, float fontSize, FontStyles style)
    {
        GameObject obj = CreateUiObject("Text", parent, new Vector2(400f, 60f));
        TMP_Text text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 20f;
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static TMP_InputField CreateInputField(Transform parent, Vector2 position, Vector2 size, string placeholderText)
    {
        GameObject root = CreateUiObject("NameInput", parent, size);
        root.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = root.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);
        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject viewportObject = CreateUiObject("Viewport", root.transform, size);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(20f, 10f);
        viewportRect.offsetMax = new Vector2(-20f, -10f);
        RectMask2D mask = viewportObject.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;

        TMP_Text text = CreateText(string.Empty, viewportObject.transform, 28f, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.Left;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        input.textViewport = viewportRect;
        input.textComponent = text as TextMeshProUGUI;

        TMP_Text placeholder = CreateText(placeholderText, viewportObject.transform, 28f, FontStyles.Italic);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.color = new Color(0.5f, 0.5f, 0.6f, 0.85f);
        placeholder.rectTransform.anchorMin = Vector2.zero;
        placeholder.rectTransform.anchorMax = Vector2.one;
        placeholder.rectTransform.offsetMin = Vector2.zero;
        placeholder.rectTransform.offsetMax = Vector2.zero;
        placeholder.textWrappingMode = TextWrappingModes.NoWrap;
        input.placeholder = placeholder;
        return input;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = CreateUiObject("AvatarDropdown", parent, size);
        root.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = root.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.95f);
        TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
        TMP_Text label = CreateText(string.Empty, root.transform, 28f, FontStyles.Bold);
        dropdown.captionText = label as TextMeshProUGUI;
        return dropdown;
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, Color color)
    {
        GameObject root = CreateUiObject(label + "Button", parent, size);
        root.GetComponent<RectTransform>().anchoredPosition = position;
        Image image = root.AddComponent<Image>();
        image.color = color;
        Button button = root.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();
        TMP_Text text = CreateText(label, root.transform, 30f, FontStyles.Bold);
        text.color = Color.white;
        return button;
    }

    private static AvatarVisual CreateAvatarVisual(Transform parent, Vector2 anchoredPosition, float size)
    {
        AvatarVisual visual = new AvatarVisual();
        visual.root = CreateUiObject("Avatar", parent, new Vector2(size, size));
        visual.root.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
        visual.image = visual.root.AddComponent<Image>();
        visual.image.color = new Color(0.25f, 0.58f, 0.88f, 0.95f);
        visual.image.raycastTarget = false;
        visual.fallbackText = CreateText("?", visual.root.transform, size * 0.45f, FontStyles.Bold);
        visual.fallbackText.color = Color.white;
        visual.fallbackText.rectTransform.anchoredPosition = Vector2.zero;
        visual.fallbackText.rectTransform.sizeDelta = new Vector2(size - 10f, size - 10f);
        return visual;
    }
}