using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ProfileMenuController : MonoBehaviour
{
    public static ProfileMenuController Instance { get; private set; }
    private static bool autoShownThisAppSession;

    [Header("Defaults")]
    [SerializeField] private string defaultAvatar = "conejo_azul";

    [Header("Scene References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newPerfilButton;
    [SerializeField] private Button updateButton;

    [Header("Cards")]
    [SerializeField] private GameObject cardProfilePrefab;
    [SerializeField] private RectTransform cardsContainer;
    [SerializeField] private Sprite[] avatarSprites;

    private readonly List<PlayerProfileData> loadedProfiles = new List<PlayerProfileData>();
    private readonly List<Button> profileCardButtons = new List<Button>();
    private readonly List<Button> avatarButtons = new List<Button>();

    private static readonly Color DarkText = new Color(0.094f, 0.188f, 0.31f, 1f);

    private ScrollRect profilesScrollRect;
    [Header("Create Profile UI")]
    [SerializeField] private GameObject createProfilePanel;
    [SerializeField] private TMP_InputField createNameInput;
    [SerializeField] private TMP_Text createAvatarLabel;
    [SerializeField] private Transform avatarContainer;
    [SerializeField] private Button cancelCreateButton;
    [SerializeField] private Button confirmCreateButton;
    private int selectedAvatarIndex;
    private bool waitingForFirebase;
    [Header("Avatar Selector")]
    [SerializeField] private GameObject cardAvatarPrefab;
    [SerializeField] private float avatarCardSpacing = 18f;
    private const float CardsViewportWidth = 820f;
    private const float CardsViewportHeight = 260f;
  /*   private const float CardWidth = 176f;
    private const float CardHeight = 214f; */
    private const float CardSpacing = 28f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "MainScene")
        {
            Destroy(this);
            return;
        }

        ResolveReferences();
        StyleStatusAndRefreshButton();
        EnsureCardsContainer();
        ConfigureCreateProfileUI();
        ConfigureButtons();

        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.ActiveProfileChanged += HandleActiveProfileChanged;

        RefreshActiveProfileDisplay();

        if (!autoShownThisAppSession)
        {
            autoShownThisAppSession = true;
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.ActiveProfileChanged -= HandleActiveProfileChanged;

        if (FirebaseManager.Instance != null)
            FirebaseManager.Instance.Ready -= HandleFirebaseReady;
    }

    public void Show()
    {
        ResolveReferences();
        EnsureCardsContainer();
        ConfigureCreateProfileUI();

        if (panelRoot == null)
            return;

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshActiveProfileDisplay();
        RefreshContinueButton();
        NarrationPlayer.Say("perfil_bienvenida");
        LoadProfilesOrWaitForFirebase();
    }

    public void Hide()
    {
        if (createProfilePanel != null)
            createProfilePanel.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (statusText == null)
            statusText = FindTextByName(panelRoot != null ? panelRoot.transform : null, "Status");

        if (continueButton == null)
            continueButton = FindButtonByName(panelRoot != null ? panelRoot.transform : null, "ContinueButton");

        if (newPerfilButton == null)
            newPerfilButton = FindButtonByName(panelRoot != null ? panelRoot.transform : null, "NewPerfilButton");

        if (updateButton == null)
            updateButton = FindButtonByName(panelRoot != null ? panelRoot.transform : null, "Update");
    }

    
    private void StyleStatusAndRefreshButton()
    {

        if (statusText == null && panelRoot != null)
        {
            Transform surface = FindChild(panelRoot.transform, "StatusSurface");

            if (surface != null)
                statusText = FindTextByName(surface, "Status");
        }

        if (statusText != null)
        {
            statusText.color = DarkText;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.textWrappingMode = TextWrappingModes.Normal;
            statusText.raycastTarget = false;
        }

        if (updateButton != null)
        {
            Image refreshImage = updateButton.GetComponent<Image>();

            if (refreshImage != null)
                refreshImage.preserveAspect = true;
        }
    }

    private void ConfigureButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (newPerfilButton != null)
        {
            newPerfilButton.onClick.RemoveAllListeners();
            newPerfilButton.onClick.AddListener(OnNewProfileClicked);
        }

        if (updateButton != null)
        {
            updateButton.onClick.RemoveAllListeners();
            updateButton.onClick.AddListener(LoadProfilesOrWaitForFirebase);
        }
    }

    private void EnsureCardsContainer()
    {
        if (panelRoot == null || cardsContainer != null)
            return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        GameObject viewportObject = new GameObject("ProfilesViewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(panelRect, false);
        viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = new Vector2(0f, -18f);
        viewportRect.sizeDelta = new Vector2(CardsViewportWidth, CardsViewportHeight);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        profilesScrollRect = viewportObject.GetComponent<ScrollRect>();
        profilesScrollRect.horizontal = true;
        profilesScrollRect.vertical = false;
        profilesScrollRect.movementType = ScrollRect.MovementType.Clamped;
        profilesScrollRect.scrollSensitivity = 18f;

        GameObject contentObject = new GameObject("ProfilesContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        cardsContainer = contentObject.GetComponent<RectTransform>();
        cardsContainer.SetParent(viewportRect, false);
        cardsContainer.anchorMin = new Vector2(0f, 0.5f);
        cardsContainer.anchorMax = new Vector2(0f, 0.5f);
        cardsContainer.pivot = new Vector2(0f, 0.5f);
        cardsContainer.anchoredPosition = Vector2.zero;
        cardsContainer.sizeDelta = new Vector2(CardsViewportWidth, CardsViewportHeight);

        HorizontalLayoutGroup layout = contentObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = CardSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        profilesScrollRect.viewport = viewportRect;
        profilesScrollRect.content = cardsContainer;
    }

    private void ConfigureCreateProfileUI()
    {
        if (createProfilePanel == null)
        {
            Debug.LogError(
                "Asigna CreatePerfilPanel en ProfileMenuController."
            );
            return;
        }

        if (createNameInput == null ||
            createAvatarLabel == null ||
            avatarContainer == null ||
            cancelCreateButton == null ||
            confirmCreateButton == null)
        {
            Debug.LogError(
                "Faltan referencias en la interfaz de creación de perfil."
            );
            return;
        }

        createProfilePanel.SetActive(false);

        cancelCreateButton.onClick.RemoveAllListeners();
        cancelCreateButton.onClick.AddListener(() =>
        {
            createProfilePanel.SetActive(false);
        });

        confirmCreateButton.onClick.RemoveAllListeners();
        confirmCreateButton.onClick.AddListener(ConfirmCreateProfile);

        CreateAvatarSelectorButtons();

        createNameInput.onValueChanged.RemoveListener(
            OnCreateProfileNameChanged
        );

        createNameInput.onValueChanged.AddListener(
            OnCreateProfileNameChanged
        );

        UpdateCreateProfileButton();
    }

    private void OnCreateProfileNameChanged(string value)
    {
        UpdateCreateProfileButton();
    }
    private void CreateAvatarSelectorButtons()
    {
        avatarButtons.Clear();

        // Eliminar las tarjetas anteriores.
        for (int i = avatarContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(avatarContainer.GetChild(i).gameObject);
        }

        // Crear únicamente los tres avatares.
        int count = Mathf.Min(3, avatarSprites.Length);

        for (int i = 0; i < count; i++)
        {
            int index = i;

            GameObject card = Instantiate(
                cardAvatarPrefab,
                avatarContainer,
                false
            );

            Transform face = card.transform.Find("AvatarImage");
            Image faceImage = face.GetComponent<Image>();

            faceImage.sprite = avatarSprites[i];
            faceImage.preserveAspect = true;

            Button button = card.GetComponent<Button>();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => SelectAvatarIndex(index)
            );

            avatarButtons.Add(button);
        }

        selectedAvatarIndex = -1;
        UpdateCreateProfileButton();
    }

    private void UpdateCreateProfileButton()
    {
        if (confirmCreateButton == null)
            return;

        bool hasName = createNameInput != null &&
                    !string.IsNullOrWhiteSpace(createNameInput.text);

        bool hasAvatar = selectedAvatarIndex >= 0 &&
                        selectedAvatarIndex < avatarSprites.Length;

        confirmCreateButton.interactable = hasName && hasAvatar;
    }


    
    private void SelectAvatarIndex(int avatarIndex)
    {
        selectedAvatarIndex = avatarIndex;

        if (createAvatarLabel != null)
            createAvatarLabel.text = GetAvatarDisplayName(avatarIndex);

        UpdateCreateProfileButton();
    }


    private void LoadProfilesOrWaitForFirebase()
    {
        FirebaseManager firebaseManager = FirebaseManager.Instance;
        waitingForFirebase = firebaseManager == null || !firebaseManager.IsReady;
        RefreshButtonState(true);
        LoadProfiles();

        if (waitingForFirebase && firebaseManager != null)
        {
            firebaseManager.Ready -= HandleFirebaseReady;
            firebaseManager.Ready += HandleFirebaseReady;
        }
    }

    private void LoadProfiles()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        ClearProfileCards();
        profileCardButtons.Clear();

        PlayerProfileManager.Instance.LoadProfiles(profiles =>
        {
            ClearProfileCards();
            profileCardButtons.Clear();
            loadedProfiles.Clear();
            loadedProfiles.AddRange(profiles);

            for (int i = 0; i < loadedProfiles.Count; i++)
                CreateProfileCard(loadedProfiles[i], i);

            if (waitingForFirebase)
                ShowStatusMessage("Sin conexion. Selecciona o crea un perfil en este dispositivo.");
            else
                RefreshActiveProfileDisplay();
            RefreshContinueButton();
        }, error =>
        {
            ShowStatusMessage(error);
            NarrationPlayer.Say("perfil_sin_conexion");
            RefreshContinueButton();
        });
    }

    private void CreateProfileCard(PlayerProfileData profile, int index)
    {
        GameObject card = CreateCardInstance();
        if (card == null)
            return;

        card.name = profile.playerId;

        TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);

        Image avatarImage = GetOrCreateAvatarImage(
            card.transform,
            new Vector2(120f, 120f),
            new Vector2(0f, 28f)
        );

        if (avatarImage != null)
        {
            avatarImage.sprite = GetAvatarSprite(profile.avatar);
        }

        if (label != null)
        {
            label.text = GetTruncatedProfileName(profile.name);
        }

        Button button = card.GetComponent<Button>();

        if (button == null)
            button = card.AddComponent<Button>();

        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectProfile(profile));

        profileCardButtons.Add(button);
    }

    private GameObject CreateCardInstance()
    {
        if (cardsContainer == null)
            return null;

        if (cardProfilePrefab != null)
            return Instantiate(cardProfilePrefab, cardsContainer, false);

       return null;
    }

    private void SelectProfile(PlayerProfileData profile)
    {
        if (PlayerProfileManager.Instance != null)
            PlayerProfileManager.Instance.SetActiveProfile(profile);

        RefreshActiveProfileDisplay();
        RepaintCards();
    }

    private void OnContinueClicked()
    {
        if (PlayerProfileManager.Instance == null || !PlayerProfileManager.Instance.HasActiveProfile)
        {
            ShowStatusMessage("Selecciona un perfil para continuar.");
            return;
        }

        if (SessionManager.Instance == null)
        {
            ShowStatusMessage("No se puede iniciar la sesion.");
            return;
        }

        SessionManager.Instance.StartSessionForPlayer(PlayerProfileManager.Instance.ActiveProfile, _ => Hide(), ShowStatusMessage);
    }

    
    
    private void OnNewProfileClicked()
    {
        if (createProfilePanel == null)
            return;

        createNameInput.text = string.Empty;

        selectedAvatarIndex = -1;

        if (createAvatarLabel != null)
            createAvatarLabel.text = "Seleccionar un avatar";

        UpdateCreateProfileButton();

        createProfilePanel.SetActive(true);
        createProfilePanel.transform.SetAsLastSibling();

        NarrationPlayer.Say("perfil_crear");
    }

   
    private void ConfirmCreateProfile()
    {
        if (selectedAvatarIndex < 0 ||
            selectedAvatarIndex >= avatarSprites.Length ||
            createNameInput == null ||
            string.IsNullOrWhiteSpace(createNameInput.text))
        {
            UpdateCreateProfileButton();
            return;
        }
        if (PlayerProfileManager.Instance == null)
            return;

        string playerName = createNameInput != null
            ? createNameInput.text
            : string.Empty;

        string avatarName = GetAvatarOptionName(selectedAvatarIndex);

        PlayerProfileManager.Instance.CreateProfile(
            playerName,
            avatarName,
            profile =>
            {
                if (createProfilePanel != null)
                    createProfilePanel.SetActive(false);

                NarrationPlayer.Say("perfil_listo");
                RefreshActiveProfileDisplay();
                LoadProfilesOrWaitForFirebase();
            },
            ShowStatusMessage
        );
    }
    private void RefreshActiveProfileDisplay()
    {
        if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile)
            ShowStatusMessage("Perfil activo: " + PlayerProfileManager.Instance.ActiveProfile.name);
        else if (!waitingForFirebase)
            ShowStatusMessage("Selecciona o crea un perfil.");

        RefreshContinueButton();
        RepaintCards();
    }

    private void RepaintCards()
    {
        if (cardsContainer == null)
            return;

        for (int i = 0; i < cardsContainer.childCount && i < loadedProfiles.Count; i++)
        {
            Transform child = cardsContainer.GetChild(i);
            Image background = child.GetComponent<Image>();
            TMP_Text label = child.GetComponentInChildren<TMP_Text>(true);
            ApplyCardVisualState(background, label, loadedProfiles[i], i);
        }
    }

    private void ApplyCardVisualState(Image background, TMP_Text label, PlayerProfileData profile, int index)
    {
        if (background == null || profile == null)
            return;

        bool isActive = PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile && PlayerProfileManager.Instance.ActiveProfile.playerId == profile.playerId;
        Color accent = GetAccentColor(index);
        background.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.94f);

        if (label != null)
        {
            label.color = DarkText;
            label.text = GetTruncatedProfileName(profile.name);
        }
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.interactable = PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActiveProfile;
    }

    private void RefreshButtonState(bool ready)
    {
        if (newPerfilButton != null)
            newPerfilButton.interactable = ready;

        if (updateButton != null)
            updateButton.interactable = ready;

        RefreshContinueButton();
    }

    private void HandleActiveProfileChanged(PlayerProfileData profile)
    {
        RefreshActiveProfileDisplay();
    }

    private void HandleFirebaseReady()
    {
        if (FirebaseManager.Instance != null)
            FirebaseManager.Instance.Ready -= HandleFirebaseReady;

        if (panelRoot != null && panelRoot.activeSelf)
            LoadProfilesOrWaitForFirebase();
    }

    private void ClearProfileCards()
    {
        if (cardsContainer == null)
            return;

        for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            Destroy(cardsContainer.GetChild(i).gameObject);
    }

    private void ShowStatusMessage(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    private static string GetTruncatedProfileName(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            return profileName;

        profileName = profileName.Trim();

        profileName = char.ToUpper(profileName[0]) +
                    profileName.Substring(1).ToLower();

        if (profileName.Length > 9)
            return profileName.Substring(0, 9).TrimEnd() + "...";

        return profileName;
    }
    private Sprite GetAvatarSprite(string avatarName)
    {
        string[] avatarOptions = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.GetAvatarOptions() : null;
        if (avatarOptions == null || avatarOptions.Length == 0)
            return GetAvatarSpriteByIndex(0);

        for (int i = 0; i < avatarOptions.Length; i++)
        {
            if (avatarOptions[i] == avatarName)
                return GetAvatarSpriteByIndex(i);
        }

        return GetAvatarSpriteByIndex(0);
    }

    private Sprite GetAvatarSpriteByIndex(int index)
    {
        if (avatarSprites == null || avatarSprites.Length == 0)
            return null;

        return avatarSprites[Mathf.Clamp(index, 0, avatarSprites.Length - 1)];
    }

    private string GetAvatarOptionName(int index)
    {
        string[] avatarOptions = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.GetAvatarOptions() : null;
        if (avatarOptions == null || avatarOptions.Length == 0)
            return defaultAvatar;

        return avatarOptions[Mathf.Clamp(index, 0, avatarOptions.Length - 1)];
    }

    private string GetAvatarDisplayName(int index)
    {
        return GetAvatarOptionName(index).Replace('_', ' ');
    }

    private static string GetAvatarShortName(int index)
    {
        switch (index)
        {
            case 0: return "A1";
            case 1: return "A2";
            default: return "A3";
        }
    }

    private static Color GetFallbackAvatarColor(int index)
    {
        switch (index % 3)
        {
            case 0: return new Color(1f, 0.76f, 0.83f, 1f);
            case 1: return new Color(0.76f, 0.92f, 0.9f, 1f);
            default: return new Color(1f, 0.88f, 0.54f, 1f);
        }
    }

    private static Color GetAccentColor(int index)
    {
        switch (index % 4)
        {
            case 0: return new Color(1f, 0.45f, 0.67f, 1f);
            case 1: return new Color(0.28f, 0.82f, 0.79f, 1f);
            case 2: return new Color(1f, 0.86f, 0.28f, 1f);
            default: return new Color(0.68f, 0.54f, 1f, 1f);
        }
    }

    private static Button FindButtonByName(Transform root, string objectName)
    {
        Transform child = FindChild(root, objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static TMP_Text FindTextByName(Transform root, string objectName)
    {
        Transform child = FindChild(root, objectName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChild(root.GetChild(i), objectName);
            if (match != null)
                return match;
        }

        return null;
    }

    private static Image GetOrCreateAvatarImage(Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        Transform child = FindChild(parent, "AvatarImage");
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image != null)
            return image;

        GameObject obj = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        image = obj.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    
    private static TMP_Text CreateLabel(
        string value,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        return text;
}

}
