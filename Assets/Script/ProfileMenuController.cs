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
    private TMP_InputField createNameInput;
    private TMP_Text createAvatarLabel;
    private GameObject createPopupRoot;
    private int selectedAvatarIndex;
    private bool waitingForFirebase;

    private const float CardsViewportWidth = 820f;
    private const float CardsViewportHeight = 260f;
    private const float CardWidth = 176f;
    private const float CardHeight = 214f;
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
        StyleProfileMenu();
        StyleStatusAndRefreshButton();
        EnsureCardsContainer();
        EnsureCreatePopup();
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
        EnsureCreatePopup();

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
        if (createPopupRoot != null)
            createPopupRoot.SetActive(false);

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
        if (statusText != null)
        {
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchoredPosition = new Vector2(-132f, 190f);
            statusRect.sizeDelta = new Vector2(540f, 76f);
            statusText.color = DarkText;
            statusText.fontSize = 28f;
            statusText.enableAutoSizing = true;
            statusText.fontSizeMin = 26f;
            statusText.fontSizeMax = 28f;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.textWrappingMode = TextWrappingModes.Normal;
            statusText.raycastTarget = false;

            Transform surface = panelRoot.transform.Find("StatusSurface");
            if (surface == null)
            {
                GameObject surfaceObject = new GameObject("StatusSurface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
                surfaceRect.SetParent(panelRoot.transform, false);
                surfaceRect.anchorMin = surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
                surfaceRect.pivot = new Vector2(0.5f, 0.5f);
                surfaceRect.anchoredPosition = statusRect.anchoredPosition;
                surfaceRect.sizeDelta = statusRect.sizeDelta;
                Image surfaceImage = surfaceObject.GetComponent<Image>();
                surfaceImage.color = new Color(1f, 0.941f, 0.816f, 1f);
                surfaceImage.raycastTarget = false;
                surfaceRect.SetSiblingIndex(statusRect.GetSiblingIndex());
            }
        }

        if (updateButton != null)
        {
            MenuStyle.PlaceCentered(updateButton.GetComponent<RectTransform>(), new Vector2(318f, 190f), new Vector2(200f, 76f));
            MenuStyle.StyleButton(updateButton, "Actualizar", MenuButtonColor.Yellow);
        }
    }

    /// <summary>
    /// Aplica el estilo del menu principal al menu de escoger perfil:
    /// panel crema, titulo multicolor y botones pildora de color.
    /// </summary>
    private void StyleProfileMenu()
    {
        if (panelRoot == null)
            return;

        MenuStyle.StylePanel(panelRoot.GetComponent<Image>(), 64f);

        TMP_Text title = FindTextByName(panelRoot.transform, "perfilText");
        if (title != null)
            MenuStyle.StyleTitle(title, "¿Quién va a jugar?", 56f);

        if (continueButton != null)
        {
            MenuStyle.PlaceCentered(continueButton.GetComponent<RectTransform>(), new Vector2(175f, -210f), new Vector2(320f, 96f));
            MenuStyle.StyleButton(continueButton, "Continuar", MenuButtonColor.Green, MenuIcon.Play);
        }

        if (newPerfilButton != null)
        {
            MenuStyle.PlaceCentered(newPerfilButton.GetComponent<RectTransform>(), new Vector2(-175f, -210f), new Vector2(320f, 96f));
            MenuStyle.StyleButton(newPerfilButton, "Crear perfil", MenuButtonColor.Purple);
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

    private void EnsureCreatePopup()
    {
        if (panelRoot == null || createPopupRoot != null)
            return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        // La escena guarda una version antigua del popup; se reemplaza por la nueva con el estilo del menu principal.
        Transform stalePopup = panelRect.Find("CreateProfilePopup");
        if (stalePopup != null)
            Destroy(stalePopup.gameObject);

        // Fondo oscuro que cubre el menu de perfiles mientras se crea uno nuevo.
        createPopupRoot = new GameObject("CreateProfilePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform blockerRect = createPopupRoot.GetComponent<RectTransform>();
        blockerRect.SetParent(panelRect, false);
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        createPopupRoot.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.16f, 0.45f);

        GameObject box = new GameObject("CreateProfileBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.SetParent(blockerRect, false);
        MenuStyle.PlaceCentered(boxRect, Vector2.zero, new Vector2(640f, 520f));
        MenuStyle.StylePanel(box.GetComponent<Image>(), 56f);

        TMP_Text title = MenuStyle.GetOrCreateText(boxRect, "Title");
        MenuStyle.PlaceCentered(title.rectTransform, new Vector2(0f, 190f), new Vector2(520f, 76f));
        MenuStyle.StyleTitle(title, "Crear perfil", 52f);

        createNameInput = CreateInputField(boxRect, new Vector2(0f, 100f), new Vector2(460f, 80f), "Nombre del jugador");

        createAvatarLabel = CreateLabel("Avatar 1", boxRect, new Vector2(0f, 30f), new Vector2(360f, 40f), 30f, FontStyles.Bold, DarkText, TextAlignmentOptions.Center);

        CreateAvatarSelectorButtons(boxRect);

        Button cancelButton = CreateActionButton(boxRect, "Cancelar", new Vector2(-140f, -188f), new Vector2(250f, 88f), MenuButtonColor.Red, MenuIcon.None);
        cancelButton.onClick.AddListener(() => createPopupRoot.SetActive(false));

        Button confirmButton = CreateActionButton(boxRect, "Guardar", new Vector2(140f, -188f), new Vector2(250f, 88f), MenuButtonColor.Green, MenuIcon.Play);
        confirmButton.onClick.AddListener(ConfirmCreateProfile);

        createPopupRoot.SetActive(false);
    }

    private void CreateAvatarSelectorButtons(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            int avatarIndex = i;
            Button avatarButton = CreateIconButton(parent, new Vector2(-140f + i * 140f, -62f), new Vector2(112f, 112f));
            avatarButtons.Add(avatarButton);
            Image icon = GetOrCreateAvatarImage(avatarButton.transform, new Vector2(86f, 86f), Vector2.zero);
            icon.sprite = GetAvatarSpriteByIndex(avatarIndex);
            icon.color = icon.sprite != null ? Color.white : GetFallbackAvatarColor(avatarIndex);
            icon.raycastTarget = false;

            TMP_Text fallback = CreateLabel(GetAvatarShortName(avatarIndex), avatarButton.transform, Vector2.zero, new Vector2(70f, 32f), 26f, FontStyles.Bold, DarkText, TextAlignmentOptions.Center);
            fallback.raycastTarget = false;
            fallback.gameObject.SetActive(icon.sprite == null);

            avatarButton.onClick.AddListener(() => SelectAvatarIndex(avatarIndex));
        }

        SelectAvatarIndex(0);
    }

    private void SelectAvatarIndex(int avatarIndex)
    {
        selectedAvatarIndex = Mathf.Clamp(avatarIndex, 0, 2);
        if (createAvatarLabel != null)
            createAvatarLabel.text = GetAvatarDisplayName(selectedAvatarIndex);

        for (int i = 0; i < avatarButtons.Count; i++)
        {
            Outline outline = avatarButtons[i].GetComponent<Outline>();
            if (outline == null)
                outline = avatarButtons[i].gameObject.AddComponent<Outline>();
            bool selected = i == selectedAvatarIndex;
            outline.effectColor = selected ? MenuStyle.GetBodyColor(MenuButtonColor.Green) : new Color(0.094f, 0.188f, 0.31f, 0.2f);
            outline.effectDistance = selected ? new Vector2(6f, -6f) : new Vector2(2f, -2f);
        }
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
        RectTransform rect = card.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image background = card.GetComponent<Image>();
        TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);
        Image avatarImage = GetOrCreateAvatarImage(card.transform, new Vector2(86f, 86f), new Vector2(0f, 28f));

        if (avatarImage != null)
        {
            avatarImage.sprite = GetAvatarSprite(profile.avatar);
            avatarImage.color = avatarImage.sprite != null ? Color.white : GetFallbackAvatarColor(index);
        }

        if (label != null)
        {
            label.text = GetTruncatedProfileName(profile.name);
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 30f;
            label.fontSize = 30f;
            label.color = DarkText;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        Button button = card.GetComponent<Button>();
        if (button == null)
            button = card.AddComponent<Button>();

        if (button.GetComponent<UIButtonPulse>() == null)
            button.gameObject.AddComponent<UIButtonPulse>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectProfile(profile));
        profileCardButtons.Add(button);

        ApplyCardVisualState(background, label, profile, index);
    }

    private GameObject CreateCardInstance()
    {
        if (cardsContainer == null)
            return null;

        if (cardProfilePrefab != null)
            return Instantiate(cardProfilePrefab, cardsContainer, false);

        GameObject fallback = new GameObject("ProfileCard", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = fallback.GetComponent<RectTransform>();
        rect.SetParent(cardsContainer, false);
        rect.sizeDelta = new Vector2(CardWidth, CardHeight);
        fallback.GetComponent<Image>().color = Color.white;
        CreateLabel(string.Empty, fallback.transform, new Vector2(0f, -66f), new Vector2(140f, 34f), 26f, FontStyles.Bold, new Color(0.13f, 0.12f, 0.36f, 1f), TextAlignmentOptions.Center);
        return fallback;
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
        EnsureCreatePopup();
        if (createPopupRoot == null)
            return;

        if (createNameInput != null)
            createNameInput.text = string.Empty;

        SelectAvatarIndex(0);
        createPopupRoot.SetActive(true);
        NarrationPlayer.Say("perfil_crear");
    }

    private void ConfirmCreateProfile()
    {
        if (PlayerProfileManager.Instance == null)
            return;

        string playerName = createNameInput != null ? createNameInput.text : string.Empty;
        string avatarName = GetAvatarOptionName(selectedAvatarIndex);

        PlayerProfileManager.Instance.CreateProfile(playerName, avatarName, profile =>
        {
            if (createPopupRoot != null)
                createPopupRoot.SetActive(false);

            NarrationPlayer.Say("perfil_listo");
            RefreshActiveProfileDisplay();
            LoadProfilesOrWaitForFirebase();
        }, ShowStatusMessage);
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

        Outline outline = background.GetComponent<Outline>();
        if (outline == null)
            outline = background.gameObject.AddComponent<Outline>();
        outline.effectColor = isActive ? accent : new Color(accent.r, accent.g, accent.b, 0.75f);
        outline.effectDistance = isActive ? new Vector2(8f, -8f) : new Vector2(5f, -5f);

        Shadow shadow = background.GetComponent<Shadow>();
        if (shadow == null)
            shadow = background.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.45f, 0.36f, 0.24f, 0.16f);
        shadow.effectDistance = new Vector2(0f, -10f);

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
        if (string.IsNullOrWhiteSpace(profileName) || profileName.Length <= 9)
            return profileName;

        return profileName.Substring(0, 9).TrimEnd() + "...";
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

    private static TMP_Text CreateLabel(string value, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(fontSize, 16f);
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static TMP_InputField CreateInputField(Transform parent, Vector2 anchoredPosition, Vector2 size, string placeholder)
    {
        GameObject root = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image background = root.GetComponent<Image>();
        MenuStyle.StylePill(background, MenuButtonColor.Blue, size.y);
        background.color = Color.white;
        TMP_InputField input = root.GetComponent<TMP_InputField>();

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(rect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(32f, 12f);
        viewportRect.offsetMax = new Vector2(-32f, -12f);

        TMP_Text text = CreateLabel(string.Empty, viewportRect, Vector2.zero, Vector2.zero, 30f, FontStyles.Bold, DarkText, TextAlignmentOptions.Left);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        TMP_Text placeholderText = CreateLabel(placeholder, viewportRect, Vector2.zero, Vector2.zero, 28f, FontStyles.Italic, new Color(0.45f, 0.5f, 0.6f, 1f), TextAlignmentOptions.Left);
        placeholderText.rectTransform.anchorMin = Vector2.zero;
        placeholderText.rectTransform.anchorMax = Vector2.one;
        placeholderText.rectTransform.offsetMin = Vector2.zero;
        placeholderText.rectTransform.offsetMax = Vector2.zero;

        input.textViewport = viewportRect;
        input.textComponent = text as TextMeshProUGUI;
        input.placeholder = placeholderText;
        return input;
    }

    private static Button CreateActionButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 size, MenuButtonColor color, MenuIcon icon)
    {
        GameObject root = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        MenuStyle.PlaceCentered(rect, anchoredPosition, size);

        Button button = root.GetComponent<Button>();
        MenuStyle.StyleButton(button, label, color, icon);
        return button;
    }

    private static Button CreateIconButton(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject root = new GameObject("AvatarButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        MenuStyle.StylePanel(root.GetComponent<Image>(), 24f);
        Button button = root.GetComponent<Button>();
        if (root.GetComponent<UIButtonPulse>() == null)
            root.AddComponent<UIButtonPulse>();
        return button;
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
}
