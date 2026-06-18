using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class SimonGameManager : MonoBehaviour
{
    [System.Serializable]
    private class BoardReferences
    {
        public RectTransform boardRoot;
        public Image baseBoardImage;
        public RectTransform zonesRoot;
    }

    [System.Serializable]
    public class SimonColorData
    {
        public string name;
        public string label;
        public AudioClip clip;
    }

    [Header("Configuracion del tablero")]
    [SerializeField] private RectTransform board4Root;
    [SerializeField] private Image board4Image;
    [SerializeField] private RectTransform board4ZonesRoot;
    [SerializeField] private RectTransform board5Root;
    [SerializeField] private Image board5Image;
    [SerializeField] private RectTransform board5ZonesRoot;
    [SerializeField] private RectTransform board7Root;
    [SerializeField] private Image board7Image;
    [SerializeField] private RectTransform board7ZonesRoot;

    [Header("Sprites base del tablero")]
    [SerializeField] private Sprite boardSprite4;
    [SerializeField] private Sprite boardSprite5;
    [SerializeField] private Sprite boardSprite7;

    [Header("Sprites iluminados del tablero")]
    [SerializeField] private Sprite[] board4LitSprites = new Sprite[4];
    [SerializeField] private Sprite[] board5LitSprites = new Sprite[5];
    [SerializeField] private Sprite[] board7LitSprites = new Sprite[7];

    [Header("Editor Preview")]
    [SerializeField] [Range(4, 7)] private int editorPreviewPieceCount = 4;

    [Header("Colores disponibles")]
    [SerializeField] private List<SimonColorData> colorPool = new List<SimonColorData>();

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Color scoreColor = new Color32(0xFF, 0xD9, 0x05, 0xFF);
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button pauseMenuButton;

    // Panel de estadisticas eliminado - UI mas limpia

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip roundTransitionClip;

    [Header("Efectos visuales")]
    [SerializeField] private SimonCelebrationEffect celebrationEffect;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private Color bgSuccessColor = new Color(0.1f, 0.3f, 0.1f, 0.3f);
    [SerializeField] private Color bgErrorColor = new Color(0.3f, 0.1f, 0.1f, 0.3f);

    [Header("Dificultad suave")]
    [SerializeField] private float pauseBetweenRounds = 0.8f;
    [SerializeField] private float playerResponseBeatWindow = 3f;
    [SerializeField] private float minimumResponseTime = 1.5f;

    [Header("Tempo")]
    [SerializeField] private float initialBPM = 60f;
    [SerializeField] private float maximumBPM = 90f;
    [SerializeField] private float bpmIncrease = 5f;
    [SerializeField] private int roundsPerSpeedIncrease = 2;
    [SerializeField] [Range(0.2f, 0.9f)] private float soundDurationRatio = 0.6f;
    [SerializeField] private SimonMetronomeController metronomeController;

    [Header("Colores de estado")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color successColor = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);

    [Header("Animacion de estado")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.15f;

    [Header("Barra de tiempo")]
    [SerializeField] private Image timeBarFill;
    [SerializeField] private Color timeOkColor = Color.white;
    [SerializeField] private Color timeWarnColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color timeLowColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float warnThreshold = 0.35f;
    [SerializeField] private float lowThreshold = 0.15f;
    [SerializeField] private GameObject timeBarRoot;

    [Header("Vidas")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    private enum GameState { Idle, ShowingSequence, PlayerTurn, GameOver }
    private GameState currentState = GameState.Idle;

    private const string RECORD_KEY = "SIMON_RECORD";

    private int currentLevel = 0;

    private readonly List<int> sequence = new List<int>();
    private int playerIndex = 0;

    private float currentInputTime;
    private float timeLimit;
    private float currentBPM;

    private Coroutine statusAnim;
    private Coroutine showSequenceRoutine;
    private Coroutine loseLifeRoutine;

    private int lives;
    private int score;

    private readonly List<SimonZoneUI> activeZones = new List<SimonZoneUI>();
    private readonly List<Transform> discoveredZoneButtons = new List<Transform>();
    private Image activeBoardImage;
    private RectTransform activeZonesRoot;
    private int currentColorCount = 4;

    private float lastTapTime = -1f;
    private float tapCooldown = 0.2f;
    private bool isPaused;

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ResolveBoardReferences();
        ApplyEditorPreview();
    }

    private void Start()
    {
        ResolveBoardReferences();
        ResolvePauseReferences();
        ValidateReferences();
        BuildBoard(4);
        SetInput(false);
        ConfigureButtons();
        currentBPM = initialBPM;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        startButton.interactable = true;
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
        }
        score = 0;
        RefreshScoreUI();
        RefreshRecordUI();

        SetStatus("Presiona JUGAR para comenzar", normalColor, animate: false);
        SetInfo("");
    }

    private void Update()
    {
        if (isPaused)
            return;

        if (currentState != GameState.PlayerTurn) return;

        currentInputTime -= Time.deltaTime;
        float remaining = Mathf.Max(0, currentInputTime);
        SetInfo($"Tiempo: {remaining:0.0}s");

        if (currentInputTime <= 0f)
        {
            StartLoseLife("Tiempo agotado");
            return;
        }

        UpdateTimeBar();
    }

    private void ValidateReferences()
    {
        if (board4ZonesRoot == null) Debug.LogError("SimonGameManager: board4ZonesRoot es null.");
        if (board5ZonesRoot == null) Debug.LogError("SimonGameManager: board5ZonesRoot es null.");
        if (board7ZonesRoot == null) Debug.LogError("SimonGameManager: board7ZonesRoot es null.");
        if (colorPool == null || colorPool.Count == 0) Debug.LogError("SimonGameManager: colorPool esta vacio.");
    }

    private int GetColorCountForLevel(int level)
    {
        if (level < 4) return 4;
        if (level < 7) return 5;
        return 7;
    }

    private void BuildBoard(int pieceCount)
    {
        if (colorPool == null || colorPool.Count < pieceCount)
        {
            Debug.LogError($"SimonGameManager: faltan {pieceCount - (colorPool?.Count ?? 0)} colores en colorPool.");
            return;
        }

        ResolveBoardReferences();
        BoardReferences board = GetBoardReferences(pieceCount);
        if (board == null || board.zonesRoot == null)
            return;

        SetActiveBoard(pieceCount);

        activeZones.Clear();

        Sprite boardSpriteToUse = pieceCount switch
        {
            4 => boardSprite4,
            5 => boardSprite5,
            7 => boardSprite7,
            _ => boardSprite4
        };

        if (board.baseBoardImage != null)
        {
            board.baseBoardImage.sprite = boardSpriteToUse;
            board.baseBoardImage.preserveAspect = true;
            board.baseBoardImage.enabled = boardSpriteToUse != null;
        }

        for (int i = 0; i < 7; i++)
        {
            bool active = i < pieceCount;
            if (!active)
                continue;

            SimonZoneUI zone = GetOrCreateZone(i);
            if (zone == null)
                continue;

            zone.gameObject.SetActive(true);

            zone.Setup(i, colorPool[i].label, OnPlayerPress);

            zone.SetOff();

            activeZones.Add(zone);
        }

        currentColorCount = pieceCount;
    }

    public void StartGame()
    {
        StopAllCoroutines();
        if (metronomeController != null)
            metronomeController.StopMetronome();

        sequence.Clear();
        playerIndex = 0;
        score = 0;
        currentLevel = 1;
        currentBPM = initialBPM;
        RefreshScoreUI();

        BuildBoard(4);
        AddStep();
        UpdateBoardForLevel();
        UpdateTempoForRound();
        ResetPlayerTimeLimit();

        SetStatus($"Mira la secuencia y repitela tocando los colores", warningColor);
        SetInfo($"Nivel {currentLevel}");

        if (replayButton != null) replayButton.gameObject.SetActive(true);

        showSequenceRoutine = StartCoroutine(ShowSequence());
        lives = maxLives;
        UpdateLivesUI();
    }

    public void ReplaySequence()
    {
        if (isPaused)
            return;

        if (currentState != GameState.PlayerTurn && currentState != GameState.Idle) return;

        StopAllCoroutines();
        if (metronomeController != null)
            metronomeController.StopMetronome();

        SetStatus("Escucha de nuevo", warningColor);
        showSequenceRoutine = StartCoroutine(ShowSequence());
    }

    private void AddStep()
    {
        sequence.Add(Random.Range(0, activeZones.Count));
    }

    private IEnumerator ShowSequence()
    {
        currentState = GameState.ShowingSequence;
        ShowAllOff();

        if (timeBarRoot != null) timeBarRoot.SetActive(true);
        SetInput(false);
        startButton.interactable = false;
        if (replayButton != null) replayButton.interactable = false;
        SetInfo($"Nivel {currentLevel}");

        yield return new WaitForSeconds(0.4f);

        float beatDuration = GetBeatDuration();
        float activeDuration = beatDuration * soundDurationRatio;
        float restDuration = Mathf.Max(0f, beatDuration - activeDuration);

        if (metronomeController != null)
            metronomeController.StartMetronome(currentBPM);

        for (int i = 0; i < sequence.Count; i++)
        {
            int step = sequence[i];

            ShowOn(step);
            PlayColorSound(step);

            yield return new WaitForSeconds(activeDuration);

            ShowAllOff();
            if (restDuration > 0f)
                yield return new WaitForSeconds(restDuration);
        }

        if (metronomeController != null)
            metronomeController.StopMetronome();

        currentState = GameState.PlayerTurn;
        playerIndex = 0;

        ResetPlayerTimeLimit();

        SetStatus("Repite la secuencia tocando los colores", normalColor, animate: false);
        SetInfo($"Nivel {currentLevel}");
        SetInput(true);
        startButton.interactable = true;
        if (replayButton != null) replayButton.interactable = true;

        if (timeBarFill != null)
            timeBarFill.transform.parent.gameObject.SetActive(true);
    }

    private void OnPlayerPress(int idx)
    {
        if (isPaused)
            return;

        if (currentState != GameState.PlayerTurn) return;

        float now = Time.time;
        if (now - lastTapTime < tapCooldown) return;
        lastTapTime = now;

        StartCoroutine(FlashBoardSelection(idx));

        if (idx != sequence[playerIndex])
        {
            TriggerHapticError();
            StartLoseLife("Fallaste");
            return;
        }

        PlayColorSound(idx);
        TriggerHapticSuccess();

        Vector3 piecePos = activeZones[idx].transform.position;
        celebrationEffect.PlayPerfectEffect(piecePos);

        playerIndex++;
        ResetPlayerTimeLimit();

        if (playerIndex >= sequence.Count)
        {
            score += 10;
            RefreshScoreUI();

            currentState = GameState.Idle;
            SetInput(false);
            StartCoroutine(NextRound());
        }
    }

    private IEnumerator NextRound()
    {
        if (roundTransitionClip != null && sfxSource != null)
            sfxSource.PlayOneShot(roundTransitionClip);

        currentLevel++;
        SetStatus($"Excelente! Nivel {currentLevel}", successColor);
        SetInfo($"Siguiente: Nivel {currentLevel}");

        celebrationEffect.PlayLevelUpEffect();

        yield return new WaitForSeconds(pauseBetweenRounds);

        AddStep();
        UpdateBoardForLevel();
        UpdateTempoForRound();
        ResetPlayerTimeLimit();

        SetStatus("Mira la secuencia y repitela tocando los colores", warningColor, animate: false);
        yield return StartCoroutine(ShowSequence());
    }

    private void UpdateTempoForRound()
    {
        int roundIndex = Mathf.Max(0, currentLevel - 1);
        int increaseSteps = Mathf.Max(0, roundIndex / Mathf.Max(1, roundsPerSpeedIncrease));
        currentBPM = Mathf.Min(maximumBPM, initialBPM + increaseSteps * bpmIncrease);

        if (metronomeController != null)
            metronomeController.SetBpm(currentBPM);
    }

    private void ResetPlayerTimeLimit()
    {
        float beatDuration = GetBeatDuration();
        currentInputTime = Mathf.Max(minimumResponseTime, beatDuration * Mathf.Max(0.5f, playerResponseBeatWindow));
        timeLimit = currentInputTime;

        if (timeBarFill != null)
        {
            timeBarFill.fillAmount = 1f;
            timeBarFill.color = timeOkColor;
        }
    }

    private void UpdateTimeBar()
    {
        if (timeBarFill == null || timeLimit <= 0f) return;

        float pct = Mathf.Clamp01(currentInputTime / timeLimit);
        timeBarFill.fillAmount = pct;

        if (pct <= lowThreshold) timeBarFill.color = timeLowColor;
        else if (pct <= warnThreshold) timeBarFill.color = timeWarnColor;
        else timeBarFill.color = timeOkColor;
    }

    private void GameOver(string reason)
    {
        StopAllCoroutines();

        currentState = GameState.GameOver;
        SetInput(false);
        startButton.interactable = true;
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (metronomeController != null)
            metronomeController.StopMetronome();

        if (gameOverClip != null && sfxSource != null)
            sfxSource.PlayOneShot(gameOverClip);

        TriggerHapticError();
        celebrationEffect.PlayErrorEffect(Vector3.zero);

        int nivelAlcanzado = currentLevel;
        SaveRecordIfNeeded(nivelAlcanzado);
        RefreshRecordUI();

        SetStatus($"Fin del juego! Nivel alcanzado: {nivelAlcanzado}", errorColor);
        SetInfo("Presiona JUGAR para intentarlo de nuevo");

        sequence.Clear();
        playerIndex = 0;
        currentLevel = 0;
    }

    private void SaveRecordIfNeeded(int currentLevel)
    {
        int record = PlayerPrefs.GetInt(RECORD_KEY, 0);

        if (currentLevel > record)
        {
            PlayerPrefs.SetInt(RECORD_KEY, currentLevel);
            PlayerPrefs.Save();
        }
    }

    private void RefreshRecordUI()
    {
        int record = PlayerPrefs.GetInt(RECORD_KEY, 0);
        if (recordText != null) recordText.text = $"Record: {record}";
    }

    private void SetInput(bool value)
    {
        for (int i = 0; i < activeZones.Count; i++)
            activeZones[i].SetInteractable(value);
    }

    private void SetStatus(string msg, Color color, bool animate = true)
    {
        if (statusText == null) return;

        statusText.text = msg;
        statusText.color = color;

        if (backgroundOverlay != null)
        {
            if (color == successColor)
                backgroundOverlay.color = bgSuccessColor;
            else if (color == errorColor)
                backgroundOverlay.color = bgErrorColor;
            else
                backgroundOverlay.color = Color.clear;
        }

        if (!animate) return;

        if (statusAnim != null) StopCoroutine(statusAnim);
        statusAnim = StartCoroutine(StatusPop());
    }

    private void SetInfo(string msg)
    {
        if (infoText == null) return;
        infoText.text = msg;
    }

    private IEnumerator StatusPop()
    {
        RectTransform rt = statusText.rectTransform;
        Vector3 original = rt.localScale;

        rt.localScale = original * 0.9f;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = t / popDuration;
            rt.localScale = Vector3.Lerp(original * 0.9f, original * popScale, k);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = t / popDuration;
            rt.localScale = Vector3.Lerp(original * popScale, original, k);
            yield return null;
        }

        rt.localScale = original;
    }

    private void ShowOn(int idx)
    {
        if (idx < 0 || idx >= activeZones.Count) return;

        Sprite litSprite = GetLitBoardSprite(currentColorCount, idx);
        if (activeBoardImage != null && litSprite != null)
            activeBoardImage.sprite = litSprite;
    }

    private void ShowAllOff()
    {
        for (int i = 0; i < activeZones.Count; i++)
            activeZones[i].SetOff();

        if (activeBoardImage != null)
            activeBoardImage.sprite = GetBoardSpriteForCount(currentColorCount);
    }

    private void PlayColorSound(int idx)
    {
        if (sfxSource == null || idx < 0 || idx >= colorPool.Count) return;

        AudioClip clipToPlay = colorPool[idx].clip;
        if (clipToPlay != null) sfxSource.PlayOneShot(clipToPlay);
    }

    private void TriggerHapticSuccess()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private void TriggerHapticError()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    private void StartLoseLife(string reason)
    {
        if (loseLifeRoutine != null) return;
        loseLifeRoutine = StartCoroutine(LoseLifeRoutine(reason));
    }

    private IEnumerator LoseLifeRoutine(string reason)
    {
        currentState = GameState.Idle;

        if (showSequenceRoutine != null)
        {
            StopCoroutine(showSequenceRoutine);
            showSequenceRoutine = null;
        }

        SetInput(false);

        lives--;
        UpdateLivesUI();

        celebrationEffect.PlayErrorEffect(Vector3.zero);

        if (lives <= 0)
        {
            GameOver(reason);
            loseLifeRoutine = null;
            yield break;
        }

        if (sfxSource != null) sfxSource.Stop();
        if (failClip != null && sfxSource != null) sfxSource.PlayOneShot(failClip);

        SetStatus($"{reason}! Te quedan {lives} vidas", errorColor);
        SetInfo($"Nivel {currentLevel}");
        ShowAllOff();

        playerIndex = 0;

        yield return new WaitForSeconds(1f);

        showSequenceRoutine = StartCoroutine(ShowSequence());
        loseLifeRoutine = null;
    }

    private void UpdateLivesUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            lifeIcons[i].sprite = (i < lives) ? heartFull : heartEmpty;
        }
    }

    private void RefreshScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            scoreText.color = scoreColor;
        }
    }

    private void UpdateBoardForLevel()
    {
        int level = Mathf.Max(1, sequence.Count);
        int neededCount = GetColorCountForLevel(level);

        if (neededCount != currentColorCount)
            BuildBoard(neededCount);
    }

    private void ResolveBoardReferences()
    {
        ResolveBoardSet(ref board4Root, ref board4Image, ref board4ZonesRoot, "Board4Root");
        ResolveBoardSet(ref board5Root, ref board5Image, ref board5ZonesRoot, "Board5Root");
        ResolveBoardSet(ref board7Root, ref board7Image, ref board7ZonesRoot, "Board7Root");
    }

    private void ApplyEditorPreview()
    {
        int visibleCount = editorPreviewPieceCount <= 4 ? 4 : editorPreviewPieceCount <= 5 ? 5 : 7;
        SetActiveBoard(visibleCount);

        if (activeBoardImage != null)
        {
            Sprite previewSprite = GetBoardSpriteForCount(visibleCount);
            activeBoardImage.sprite = previewSprite;
            activeBoardImage.enabled = previewSprite != null;
            activeBoardImage.preserveAspect = true;
        }

        if (activeZonesRoot == null)
            return;

        if (discoveredZoneButtons.Count == 0)
            RefreshDiscoveredZones();

        for (int i = 0; i < discoveredZoneButtons.Count; i++)
            discoveredZoneButtons[i].gameObject.SetActive(i < visibleCount);
    }

    private Sprite GetBoardSpriteForCount(int pieceCount)
    {
        return pieceCount switch
        {
            4 => boardSprite4,
            5 => boardSprite5,
            7 => boardSprite7,
            _ => boardSprite4
        };
    }

    private Sprite GetLitBoardSprite(int pieceCount, int index)
    {
        Sprite[] litSprites = pieceCount switch
        {
            4 => board4LitSprites,
            5 => board5LitSprites,
            7 => board7LitSprites,
            _ => null
        };

        if (litSprites == null || index < 0 || index >= litSprites.Length)
            return null;

        return litSprites[index];
    }

    private SimonZoneUI GetOrCreateZone(int index)
    {
        if (activeZonesRoot == null)
            return null;

        if (discoveredZoneButtons.Count == 0)
            RefreshDiscoveredZones();

        Transform zoneTransform = activeZonesRoot.Find("ZoneButton_" + index);
        if (zoneTransform == null && index >= 0 && index < discoveredZoneButtons.Count)
            zoneTransform = discoveredZoneButtons[index];

        if (zoneTransform == null)
        {
            Debug.LogWarning("SimonGameManager: falta ZoneButton_" + index);
            return null;
        }

        SimonZoneUI zone = zoneTransform.GetComponent<SimonZoneUI>();
        if (zone == null)
            zone = zoneTransform.gameObject.AddComponent<SimonZoneUI>();

        return zone;
    }

    private void RefreshDiscoveredZones()
    {
        discoveredZoneButtons.Clear();
        if (activeZonesRoot == null)
            return;

        List<Transform> namedZones = new List<Transform>();
        List<Transform> unnamedZones = new List<Transform>();

        for (int i = 0; i < activeZonesRoot.childCount; i++)
        {
            Transform child = activeZonesRoot.GetChild(i);
            if (child.GetComponent<Button>() == null)
                continue;

            if (child.name.StartsWith("ZoneButton_"))
                namedZones.Add(child);
            else
                unnamedZones.Add(child);
        }

        namedZones = namedZones.OrderBy(t => ExtractZoneIndex(t.name)).ToList();
        discoveredZoneButtons.AddRange(namedZones);
        discoveredZoneButtons.AddRange(unnamedZones);
    }

    private int ExtractZoneIndex(string zoneName)
    {
        const string prefix = "ZoneButton_";
        if (string.IsNullOrEmpty(zoneName))
            return int.MaxValue;

        string trimmedName = zoneName.Trim();
        if (!trimmedName.StartsWith(prefix))
            return int.MaxValue;

        string suffix = trimmedName.Substring(prefix.Length).Trim();
        if (int.TryParse(suffix, out int parsedIndex))
            return parsedIndex;

        return int.MaxValue;
    }

    private void ResolveBoardSet(ref RectTransform board, ref Image image, ref RectTransform zones, string rootName)
    {
        if (board == null)
        {
            GameObject boardObject = GameObject.Find(rootName);
            if (boardObject != null)
                board = boardObject.GetComponent<RectTransform>();
        }

        if (image == null && board != null)
        {
            Transform imageTransform = board.Find("BaseBoardImage");
            if (imageTransform != null)
                image = imageTransform.GetComponent<Image>();
        }

        if (zones == null && board != null)
        {
            Transform zonesTransform = board.Find("ZonesRoot");
            if (zonesTransform != null)
                zones = zonesTransform as RectTransform;
        }
    }

    private BoardReferences GetBoardReferences(int pieceCount)
    {
        BoardReferences board = new BoardReferences();
        switch (pieceCount)
        {
            case 4:
                board.boardRoot = board4Root;
                board.baseBoardImage = board4Image;
                board.zonesRoot = board4ZonesRoot;
                break;
            case 5:
                board.boardRoot = board5Root;
                board.baseBoardImage = board5Image;
                board.zonesRoot = board5ZonesRoot;
                break;
            case 7:
                board.boardRoot = board7Root;
                board.baseBoardImage = board7Image;
                board.zonesRoot = board7ZonesRoot;
                break;
            default:
                return null;
        }

        return board.boardRoot != null ? board : null;
    }

    private void SetActiveBoard(int pieceCount)
    {
        SetBoardVisible(board4Root, pieceCount == 4);
        SetBoardVisible(board5Root, pieceCount == 5);
        SetBoardVisible(board7Root, pieceCount == 7);

        BoardReferences board = GetBoardReferences(pieceCount);
        activeBoardImage = board != null ? board.baseBoardImage : null;
        activeZonesRoot = board != null ? board.zonesRoot : null;
        RefreshDiscoveredZones();
    }

    private void SetBoardVisible(RectTransform boardRootToToggle, bool visible)
    {
        if (boardRootToToggle != null)
            boardRootToToggle.gameObject.SetActive(visible);
    }

    private void ResolvePauseReferences()
    {
        if (menuButton == null)
            menuButton = FindButtonInScene("BtnMenu") ?? FindButtonInScene("BtnExIt") ?? FindButtonInScene("MenuButton");

        if (pausePanel == null)
        {
            GameObject pausePanelObject = GameObject.Find("PausePanel");
            if (pausePanelObject != null)
                pausePanel = pausePanelObject;
        }

        if (pausePanel != null)
        {
            Transform pauseTransform = pausePanel.transform;
            if (resumeButton == null)
                resumeButton = FindButton(pauseTransform, "ResumenButton") ?? FindButton(pauseTransform, "ResumeButton");
            if (restartButton == null)
                restartButton = FindButton(pauseTransform, "RestartButton");
            if (closeButton == null)
                closeButton = FindButton(pauseTransform, "CloseButton");
            if (pauseMenuButton == null)
                pauseMenuButton = FindButton(pauseTransform, "MenuButton");
        }
    }

    private void ConfigureButtons()
    {
        if (startButton != null)
        {
            startButton.onClick = new Button.ButtonClickedEvent();
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        if (replayButton != null)
        {
            replayButton.onClick = new Button.ButtonClickedEvent();
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplaySequence);
        }

        if (menuButton != null)
        {
            menuButton.onClick = new Button.ButtonClickedEvent();
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick = new Button.ButtonClickedEvent();
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (closeButton != null)
        {
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick = new Button.ButtonClickedEvent();
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }

        if (pauseMenuButton != null)
        {
            pauseMenuButton.onClick = new Button.ButtonClickedEvent();
            pauseMenuButton.onClick.RemoveAllListeners();
            pauseMenuButton.onClick.AddListener(LoadMainScene);
        }
    }

    private void PauseGame()
    {
        if (pausePanel == null || isPaused)
            return;

        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetInput(false);
        if (metronomeController != null)
            metronomeController.PauseMetronome();

        if (startButton != null)
            startButton.interactable = false;
        if (replayButton != null)
            replayButton.interactable = false;
    }

    private void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (metronomeController != null && currentState != GameState.GameOver)
        {
            if (currentState == GameState.ShowingSequence)
                metronomeController.ResumeMetronome();
            else
                metronomeController.StopMetronome();
        }

        if (startButton != null)
            startButton.interactable = currentState != GameState.ShowingSequence;
        if (replayButton != null)
            replayButton.interactable = replayButton.gameObject.activeSelf && currentState != GameState.ShowingSequence;

        SetInput(currentState == GameState.PlayerTurn);
    }

    private void RestartScene()
    {
        if (metronomeController != null)
            metronomeController.StopMetronome();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadMainScene()
    {
        if (metronomeController != null)
            metronomeController.StopMetronome();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainScene");
    }

    private static Button FindButton(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform match = FindChild(root, objectName);
        return match != null ? match.GetComponent<Button>() : null;
    }

    private static Button FindButtonInScene(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<Button>() : null;
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

    private IEnumerator FlashBoardSelection(int idx)
    {
        ShowOn(idx);
        yield return new WaitForSeconds(GetBeatDuration() * soundDurationRatio);
        ShowAllOff();
    }

    private float GetBeatDuration()
    {
        return 60f / Mathf.Max(1f, currentBPM);
    }
}
