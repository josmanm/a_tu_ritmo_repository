using System;
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
        public Color accentColor = Color.white;
    }

    [System.Serializable]
    private class MusicalSequence
    {
        public string name;
        public List<int> notes = new List<int>();
    }

    private enum SimonMode
    {
        Classic,
        Compass,
    }

    private enum CompassBeatType
    {
        Color,
        Silence,
    }

    [System.Serializable]
    private class CompassBeatStep
    {
        public CompassBeatType type;
        public int colorIndex;
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
    [SerializeField] private TopHudPanelView topHudPanelView;
    [SerializeField] private Button startButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image statusIconImage;
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
    [SerializeField] private int levelsBeforeFiveColors = 7;
    [SerializeField] private int levelsBeforeSevenColors = 14;

    [Header("Secuencias musicales")]
    [SerializeField] private bool useMusicalSequences = true;
    [SerializeField] [Range(0f, 1f)] private float musicalSequenceChance = 0.3f;
    [SerializeField] private int musicalSequenceStartLevel = 4;
    [SerializeField] private List<MusicalSequence> musicalSequences = new List<MusicalSequence>();

    [Header("Modo de juego")]
    [SerializeField] private SimonMode simonMode = SimonMode.Classic;

    [Header("Modo Compas")]
    [SerializeField] private int beatsPerMeasure = 4;
    [SerializeField] private int levelsBeforeCompassSilence = 4;
    [SerializeField] private int levelsBeforeTwoMeasures = 10;
    [SerializeField] [Range(0f, 1f)] private float silenceChance = 0.22f;
    [SerializeField] private Color compassSlotIdleColor = new Color(1f, 1f, 1f, 0.42f);
    [SerializeField] private Color compassSlotActiveColor = new Color(1f, 0.92f, 0.36f, 1f);
    [SerializeField] private Color compassSlotSuccessColor = new Color(0.22f, 0.88f, 0.42f, 1f);
    [SerializeField] private Color compassSlotSilenceColor = new Color(0.45f, 0.62f, 1f, 0.72f);
    [SerializeField] private Sprite compassSilenceSprite;

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
    [SerializeField] private float shakeDuration = 0.22f;
    [SerializeField] private float shakeDistance = 28f;
    [SerializeField] private float overlayFlashDuration = 0.35f;
    [SerializeField] private Vector2 statusMessageSize = new Vector2(1400f, 320f);
    [SerializeField] private float statusMessageOffsetY = 0f;
    [SerializeField] private float statusMessageMaxFontSize = 88f;
    [SerializeField] private float statusMessageMinFontSize = 42f;
    [SerializeField] private Vector2 statusIconSize = new Vector2(220f, 220f);
    [SerializeField] private float statusIconOffsetY = 200f;
    [SerializeField] private Color bgWarningColor = new Color(0.35f, 0.28f, 0.08f, 0.28f);
    [SerializeField] private float sequenceIntroMessageSeconds = 1.5f;
    [SerializeField] private float playerTurnMessageSeconds = 1.2f;
    [SerializeField] private float feedbackMessageSeconds = 1.25f;
    [SerializeField] private float gameOverMessageSeconds = 2.2f;
    [SerializeField] private Sprite neutralStatusSprite;
    [SerializeField] private Sprite successStatusSprite;
    [SerializeField] private Sprite warningStatusSprite;
    [SerializeField] private Sprite errorStatusSprite;

    [Header("Pulido visual UI")]
    [SerializeField] private float statusFadeDuration = 0.18f;
    [SerializeField] private float statusHiddenScale = 0.92f;
    [SerializeField] private float statusIconPopScale = 1.08f;
    [SerializeField] private float pausePanelFadeDuration = 0.2f;
    [SerializeField] private float hudPopDuration = 0.16f;
    [SerializeField] private float hudPopScale = 1.1f;
    [SerializeField] private float criticalBarPulseScale = 1.06f;
    [SerializeField] private float boardObserveScale = 1.03f;
    [SerializeField] private float boardPlayerTurnScale = 1f;
    [SerializeField] private float boardIdleScale = 0.98f;
    [SerializeField] private float boardStateTransitionDuration = 0.18f;
    [SerializeField] private float boardSuccessScale = 1.08f;
    [SerializeField] private float boardShakeDistance = 22f;
    [SerializeField] private float boardShakeDuration = 0.22f;
    [SerializeField] private Color boardRingColor = new Color(1f, 0.95f, 0.7f, 0.9f);
    [SerializeField] private float boardRingStartScale = 0.55f;
    [SerializeField] private float boardRingEndScale = 1.45f;
    [SerializeField] private float boardRingDuration = 0.45f;
    [SerializeField] private float boardRingFontSize = 220f;

    [Header("Barra de tiempo")]
    [SerializeField] private Color timeOkColor = Color.white;
    [SerializeField] private Color timeWarnColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color timeLowColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float warnThreshold = 0.35f;
    [SerializeField] private float lowThreshold = 0.15f;

    [Header("Vidas")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    [Header("Puntuacion y ayudas")]
    [SerializeField] private int pointsPerRound = 10;
    [SerializeField] private int streakBonusEvery = 3;
    [SerializeField] private int streakBonusPoints = 5;
    [SerializeField] private int maxReplaysPerLevel = 1;

    private enum GameState { Idle, ShowingSequence, PlayerTurn, GameOver }
    private GameState currentState = GameState.Idle;

    private int currentLevel = 0;

    private readonly List<int> sequence = new List<int>();
    private readonly List<CompassBeatStep> compassSequence = new List<CompassBeatStep>();
    private int playerIndex = 0;

    private float currentInputTime;
    private float timeLimit;
    private float currentBPM;

    private Coroutine statusAnim;
    private Coroutine statusVisibilityRoutine;
    private Coroutine overlayFlashRoutine;
    private Coroutine showSequenceRoutine;
    private Coroutine loseLifeRoutine;

    private int lives;
    private int score;

    private readonly List<SimonZoneUI> activeZones = new List<SimonZoneUI>();
    private readonly List<Transform> discoveredZoneButtons = new List<Transform>();
    private Image activeBoardImage;
    private RectTransform activeBoardRoot;
    private RectTransform activeZonesRoot;
    private int currentColorCount = 4;
    private MusicalSequence activeMusicalSequence;
    private int activeMusicalSequenceIndex;
    private TMP_Text boardCenterRingText;
    [SerializeField] private RectTransform compassGuideRoot;
    private CompassGuideView compassGuideView;
    private CanvasGroup compassGuideCanvasGroup;

    private float lastTapTime = -1f;
    private float tapCooldown = 0.2f;
    private bool isPaused;
    private bool isRoundTransitioning;
    private Button menuButton;
    private TMP_Text scoreText;
    private Image timeBarFill;
    private GameObject timeBarRoot;
    private PausePanelView pausePanelView;
    private CanvasGroup pausePanelCanvasGroup;
    private UIPanelTransition pausePanelTransition;
    private Coroutine pausePanelRoutine;
    private Coroutine scoreAnimRoutine;
    private Coroutine timeBarPulseRoutine;
    private Coroutine boardStateRoutine;
    private Coroutine boardFeedbackRoutine;
    private Coroutine boardRingRoutine;
    private Image[] lifeIcons = Array.Empty<Image>();
    private readonly Dictionary<int, Coroutine> lifeAnimRoutines = new Dictionary<int, Coroutine>();
    private Vector3 scoreBaseScale = Vector3.one;
    private Vector3 timeBarBaseScale = Vector3.one;
    private Vector3 boardBaseScale = Vector3.one;
    private Vector2 boardBaseAnchoredPosition = Vector2.zero;
    private float sfxBaseVolume = 1f;
    private float attemptStartedAt;
    private int attemptErrors;
    private int attemptCorrectAnswers;
    private int attemptLevelRepetitions;
    private bool attemptReportSubmitted;
    private string attemptExitReason = "completed";
    private int currentStreak;
    private int replaysUsedThisLevel;
    private int compassCurrentBeatIndex = -1;
    private bool compassAwaitingBeatInput;
    private bool compassBeatResolved;

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        ResolveBoardReferences();
        ApplyEditorPreview();
    }

    private void Start()
    {
        GameSettings.EnsureInitialized();
        currentBPM = initialBPM;
        ResolveBoardReferences();
        ResolveHudReferences();
        ResolvePauseReferences();
        ValidateReferences();
        EnsureDefaultMusicalSequences();
        ConfigureStatusOverlay();
        ConfigurePausePanelVisuals();
        EnsureCompassGuide();
        HideMetronomeVisual();
        BuildBoard(4);
        StartBoardStateTransition(boardIdleScale);
        SetInput(false);
        ConfigureButtons();

        if (sfxSource != null)
            sfxBaseVolume = sfxSource.volume;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (scoreText != null)
            scoreBaseScale = scoreText.rectTransform.localScale;

        if (timeBarFill != null)
            timeBarBaseScale = timeBarFill.rectTransform.localScale;

        SetStartButtonVisible(true);
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
        }
        score = 0;
        RefreshScoreUI();

        SetStatus("Presiona JUGAR para comenzar", normalColor, animate: false);
        ApplyGameSettings();
        SetCompassGuideVisible(false);
    }

    private void HideMetronomeVisual()
    {
        if (metronomeController != null)
            metronomeController.SetVisualVisible(false);
    }

    private void SetStartButtonVisible(bool visible)
    {
        if (startButton == null)
            return;

        startButton.gameObject.SetActive(visible);
        SetStartButtonInteractable(visible);
    }

    private void SetStartButtonInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += ApplyGameSettings;
    }

    private void OnDisable()
    {
        GameSettings.SettingsChanged -= ApplyGameSettings;
    }

    private void Update()
    {
        if (isPaused)
            return;

        if (simonMode == SimonMode.Compass)
            return;

        if (currentState != GameState.PlayerTurn) return;

        currentInputTime -= Time.deltaTime;

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
        if (level < levelsBeforeFiveColors) return 4;
        if (level < levelsBeforeSevenColors) return 5;
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
        ResetTransientVisualState();
        if (metronomeController != null)
            metronomeController.StopMetronome();

        sequence.Clear();
        playerIndex = 0;
        score = 0;
        currentLevel = 1;
        currentBPM = initialBPM;
        activeMusicalSequence = null;
        activeMusicalSequenceIndex = 0;
        isRoundTransitioning = false;
        currentStreak = 0;
        replaysUsedThisLevel = 0;
        attemptStartedAt = Time.time;
        attemptErrors = 0;
        attemptCorrectAnswers = 0;
        attemptLevelRepetitions = 0;
        attemptReportSubmitted = false;
        attemptExitReason = "completed";
        RefreshScoreUI();
        SetStartButtonVisible(false);

        BuildBoard(4);
        if (simonMode == SimonMode.Compass)
            GenerateCompassSequenceForLevel(currentLevel);
        else
            AddStep();
        UpdateBoardForLevel();
        UpdateTempoForRound();
        ResetPlayerTimeLimit();

        SetStatus(BuildRoundIntroMessage(), warningColor);

        if (replayButton != null) replayButton.gameObject.SetActive(true);

        lives = maxLives;
        UpdateLivesUI();
        showSequenceRoutine = StartCoroutine(BeginSequenceAfterMessage(BuildRoundIntroMessage(), warningColor, sequenceIntroMessageSeconds));
    }

    public void ReplaySequence()
    {
        if (isPaused)
            return;

        if (isRoundTransitioning)
            return;

        if (currentState != GameState.PlayerTurn && currentState != GameState.Idle) return;

        if (currentState == GameState.PlayerTurn && maxReplaysPerLevel >= 0 && replaysUsedThisLevel >= maxReplaysPerLevel)
        {
            SetStatus("Ya repetiste este nivel\nConfia en tu memoria", warningColor);
            StartHideStatusCountdown(feedbackMessageSeconds);
            return;
        }

        if (currentState == GameState.PlayerTurn)
            replaysUsedThisLevel++;

        StopAllCoroutines();
        ResetTransientVisualState();
        if (metronomeController != null)
            metronomeController.StopMetronome();

        showSequenceRoutine = StartCoroutine(BeginSequenceAfterMessage(BuildReplayMessage(), warningColor, sequenceIntroMessageSeconds));
    }

    private void AddStep()
    {
        if (TryAddMusicalStep())
            return;

        sequence.Add(UnityEngine.Random.Range(0, activeZones.Count));
    }

    private bool TryAddMusicalStep()
    {
        int effectiveColorCount = GetColorCountForLevel(currentLevel);
        if (!useMusicalSequences || currentLevel < musicalSequenceStartLevel || effectiveColorCount != 4)
            return false;

        if (activeMusicalSequence == null)
        {
            if (UnityEngine.Random.value > musicalSequenceChance)
                return false;

            List<MusicalSequence> availableSequences = GetPlayableMusicalSequences(effectiveColorCount);
            if (availableSequences.Count == 0)
                return false;

            activeMusicalSequence = availableSequences[UnityEngine.Random.Range(0, availableSequences.Count)];
            activeMusicalSequenceIndex = 0;
        }

        if (activeMusicalSequence.notes == null || activeMusicalSequence.notes.Count == 0)
        {
            activeMusicalSequence = null;
            activeMusicalSequenceIndex = 0;
            return false;
        }

        int note = activeMusicalSequence.notes[activeMusicalSequenceIndex];
        sequence.Add(note);
        activeMusicalSequenceIndex++;

        if (activeMusicalSequenceIndex >= activeMusicalSequence.notes.Count)
        {
            activeMusicalSequence = null;
            activeMusicalSequenceIndex = 0;
        }

        return true;
    }

    private void GenerateCompassSequenceForLevel(int level)
    {
        compassSequence.Clear();

        int colorCount = GetColorCountForLevel(level);
        int measureCount = level >= levelsBeforeTwoMeasures ? 2 : 1;
        bool allowSilence = level >= levelsBeforeCompassSilence;
        int totalBeats = Mathf.Max(1, beatsPerMeasure) * measureCount;

        for (int i = 0; i < totalBeats; i++)
        {
            bool useSilence = allowSilence && UnityEngine.Random.value < silenceChance;
            CompassBeatStep step = new CompassBeatStep
            {
                type = useSilence ? CompassBeatType.Silence : CompassBeatType.Color,
                colorIndex = useSilence ? -1 : UnityEngine.Random.Range(0, colorCount),
            };

            compassSequence.Add(step);
        }

        if (compassSequence.Count > 0 && compassSequence.TrueForAll(step => step.type == CompassBeatType.Silence))
            compassSequence[0].type = CompassBeatType.Color;
    }

    private void HandleCompassPlayerPress(int idx)
    {
        if (currentState != GameState.PlayerTurn || !compassAwaitingBeatInput || compassBeatResolved)
            return;

        if (compassCurrentBeatIndex < 0 || compassCurrentBeatIndex >= compassSequence.Count)
            return;

        StartCoroutine(FlashBoardSelection(idx));

        CompassBeatStep step = compassSequence[compassCurrentBeatIndex];
        if (step.type == CompassBeatType.Silence)
        {
            TriggerHapticError();
            StartLoseLife("Debes esperar");
            return;
        }

        if (idx != step.colorIndex)
        {
            TriggerHapticError();
            StartLoseLife("Color incorrecto");
            return;
        }

        compassBeatResolved = true;
        attemptCorrectAnswers++;
        PlayColorSound(idx);
        TriggerHapticSuccess();
        UpdateCompassGuideBeatState(compassCurrentBeatIndex, compassSlotSuccessColor);
    }

    private IEnumerator ShowCompassSequence()
    {
        currentState = GameState.ShowingSequence;
        ShowAllOff();
        SetCompassGuideVisible(true);

        if (timeBarRoot != null)
            timeBarRoot.SetActive(true);

        SetInput(false);
        SetStartButtonInteractable(false);
        if (replayButton != null)
            replayButton.interactable = false;
        StartBoardStateTransition(boardObserveScale);

        yield return new WaitForSeconds(0.4f);

        float beatDuration = GetBeatDuration();
        if (metronomeController != null)
            metronomeController.StartMetronome(currentBPM);

        for (int i = 0; i < compassSequence.Count; i++)
        {
            CompassBeatStep step = compassSequence[i];
            int beatInMeasure = i % beatsPerMeasure;
            int measureStart = i - beatInMeasure;
            RefreshCompassGuideForMeasure(measureStart);
            UpdateCompassGuideBeatState(i, compassSlotActiveColor);

            if (step.type == CompassBeatType.Color)
            {
                ShowOn(step.colorIndex);
                PlayColorSound(step.colorIndex);
            }
            else
            {
                ShowAllOff();
            }

            yield return StartCoroutine(RunCompassBeatCountdown(beatDuration));
            ShowAllOff();
            UpdateCompassGuideBeatState(i, step.type == CompassBeatType.Silence ? compassSlotSilenceColor : compassSlotIdleColor);
        }

        if (metronomeController != null)
            metronomeController.StopMetronome();

        yield return StartCoroutine(ShowStatusThenHide(BuildPlayerTurnMessage(), normalColor, playerTurnMessageSeconds, animate: false));
        yield return StartCoroutine(RunCompassPlayerTurn());
    }

    private IEnumerator RunCompassPlayerTurn()
    {
        currentState = GameState.PlayerTurn;
        StartBoardStateTransition(boardPlayerTurnScale);
        SetInput(true);
        SetStartButtonInteractable(true);
        if (replayButton != null)
            replayButton.interactable = true;

        float beatDuration = GetBeatDuration();
        for (int i = 0; i < compassSequence.Count; i++)
        {
            CompassBeatStep step = compassSequence[i];
            compassCurrentBeatIndex = i;
            compassAwaitingBeatInput = true;
            compassBeatResolved = false;

            int beatInMeasure = i % beatsPerMeasure;
            int measureStart = i - beatInMeasure;
            RefreshCompassGuideForMeasure(measureStart);
            UpdateCompassGuideBeatState(i, compassSlotActiveColor);

            float elapsed = 0f;
            ResetCompassBeatBar();
            while (elapsed < beatDuration)
            {
                elapsed += Time.deltaTime;
                UpdateCompassBeatBar(elapsed / beatDuration);
                yield return null;

                if (currentState != GameState.PlayerTurn)
                    yield break;

                if (step.type == CompassBeatType.Color && compassBeatResolved)
                    break;
            }

            compassAwaitingBeatInput = false;
            ResetCompassBeatBar();

            if (currentState != GameState.PlayerTurn)
                yield break;

            if (step.type == CompassBeatType.Color)
            {
                if (!compassBeatResolved)
                {
                    StartLoseLife("Te falto tocar");
                    yield break;
                }
            }
            else if (step.type == CompassBeatType.Silence)
            {
                attemptCorrectAnswers++;
                UpdateCompassGuideBeatState(i, compassSlotSuccessColor);
            }

            if (step.type != CompassBeatType.Silence)
                UpdateCompassGuideBeatState(i, compassSlotSuccessColor);
        }

        SetInput(false);
        score += AwardRoundScore();
        RefreshScoreUI();
        AnimateScorePop();
        PlayBoardCenterRing();
        currentState = GameState.Idle;
        isRoundTransitioning = true;
        StartCoroutine(NextRound());
    }

    private List<MusicalSequence> GetPlayableMusicalSequences(int maxColorCount)
    {
        List<MusicalSequence> availableSequences = new List<MusicalSequence>();
        for (int i = 0; i < musicalSequences.Count; i++)
        {
            MusicalSequence sequenceData = musicalSequences[i];
            if (sequenceData == null || sequenceData.notes == null || sequenceData.notes.Count == 0)
                continue;

            bool canPlay = true;
            for (int j = 0; j < sequenceData.notes.Count; j++)
            {
                if (sequenceData.notes[j] < 0 || sequenceData.notes[j] >= maxColorCount)
                {
                    canPlay = false;
                    break;
                }
            }

            if (canPlay)
                availableSequences.Add(sequenceData);
        }

        return availableSequences;
    }

    private void EnsureDefaultMusicalSequences()
    {
        if (musicalSequences != null && musicalSequences.Count > 0)
            return;

        musicalSequences = new List<MusicalSequence>
        {
            CreateMusicalSequence("Do Re Mi Sol", 2, 0, 3, 1),
            CreateMusicalSequence("Mi Do Re Do", 3, 2, 0, 2),
            CreateMusicalSequence("Sol Mi Do Re", 1, 3, 2, 0),
            CreateMusicalSequence("Do Do Mi Sol", 2, 2, 3, 1),
            CreateMusicalSequence("Re Do Sol Mi", 0, 2, 1, 3),
            CreateMusicalSequence("Mi Sol Mi Do", 3, 1, 3, 2),
            CreateMusicalSequence("Do Re Do Mi Sol", 2, 0, 2, 3, 1),
            CreateMusicalSequence("Sol Do Mi Do", 1, 2, 3, 2),
            CreateMusicalSequence("Re Mi Sol Re", 0, 3, 1, 0),
            CreateMusicalSequence("Do Mi Do Re Sol", 2, 3, 2, 0, 1),
        };
    }

    private static MusicalSequence CreateMusicalSequence(string name, params int[] notes)
    {
        MusicalSequence sequence = new MusicalSequence
        {
            name = name,
            notes = new List<int>()
        };

        if (notes != null)
            sequence.notes.AddRange(notes);

        return sequence;
    }

    private IEnumerator ShowSequence()
    {
        currentState = GameState.ShowingSequence;
        ShowAllOff();

        if (timeBarRoot != null) timeBarRoot.SetActive(true);
        SetInput(false);
        SetStartButtonInteractable(false);
        if (replayButton != null) replayButton.interactable = false;
        StartBoardStateTransition(boardObserveScale);

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

        currentState = GameState.Idle;
        playerIndex = 0;

        yield return StartCoroutine(ShowStatusThenHide(BuildPlayerTurnMessage(), normalColor, playerTurnMessageSeconds, animate: false));

        ResetPlayerTimeLimit();
        currentState = GameState.PlayerTurn;
        StartBoardStateTransition(boardPlayerTurnScale);
        SetInput(true);
        SetStartButtonInteractable(true);
        if (replayButton != null) replayButton.interactable = true;

        if (timeBarFill != null)
            timeBarFill.transform.parent.gameObject.SetActive(true);
    }

    private void OnPlayerPress(int idx)
    {
        if (isPaused)
            return;

        if (simonMode == SimonMode.Compass)
        {
            HandleCompassPlayerPress(idx);
            return;
        }

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
        attemptCorrectAnswers++;

        Vector3 piecePos = activeZones[idx].transform.position;
        if (celebrationEffect != null)
            celebrationEffect.PlayPerfectEffect(piecePos);
        PlayBoardSuccessPulse();

        playerIndex++;
        ResetPlayerTimeLimit();

        if (playerIndex >= sequence.Count)
        {
            score += AwardRoundScore();
            RefreshScoreUI();
            AnimateScorePop();
            PlayBoardCenterRing();

            currentState = GameState.Idle;
            isRoundTransitioning = true;
            SetInput(false);
            StartCoroutine(NextRound());
        }
    }

    private IEnumerator NextRound()
    {
        if (roundTransitionClip != null && sfxSource != null)
            sfxSource.PlayOneShot(roundTransitionClip);

        currentLevel++;
        replaysUsedThisLevel = 0;
        yield return StartCoroutine(ShowStatusThenHide(BuildLevelCompleteMessage(), successColor, feedbackMessageSeconds));

        if (celebrationEffect != null)
            celebrationEffect.PlayLevelUpEffect();
        PlayBoardSuccessPulse();

        yield return new WaitForSeconds(pauseBetweenRounds);

        if (simonMode == SimonMode.Compass)
            GenerateCompassSequenceForLevel(currentLevel);
        else
            AddStep();
        UpdateBoardForLevel();
        UpdateTempoForRound();
        ResetPlayerTimeLimit();

        isRoundTransitioning = false;
        yield return StartCoroutine(simonMode == SimonMode.Compass ? ShowCompassSequence() : ShowSequence());
    }

    private int AwardRoundScore()
    {
        currentStreak++;

        int earned = Mathf.Max(0, pointsPerRound);
        if (streakBonusEvery > 0 && currentStreak % streakBonusEvery == 0)
            earned += Mathf.Max(0, streakBonusPoints);

        return earned;
    }

    private string BuildRoundIntroMessage()
    {
        int stepCount = simonMode == SimonMode.Compass ? compassSequence.Count : sequence.Count;
        string action = simonMode == SimonMode.Compass ? "Escucha el compas" : "Observa";
        return $"Nivel {currentLevel}\n{action} {FormatStepCount(stepCount)}";
    }

    private string BuildReplayMessage()
    {
        if (maxReplaysPerLevel < 0)
            return simonMode == SimonMode.Compass
                ? $"Escucha el compas\nNivel {currentLevel}"
                : $"Escucha de nuevo\nNivel {currentLevel}";

        int remaining = Mathf.Max(0, maxReplaysPerLevel - replaysUsedThisLevel);
        string title = simonMode == SimonMode.Compass ? "Escucha el compas" : "Escucha de nuevo";
        return $"{title}\nTe quedan {remaining} ayudas";
    }

    private string BuildPlayerTurnMessage()
    {
        int stepCount = simonMode == SimonMode.Compass ? compassSequence.Count : sequence.Count;
        string action = simonMode == SimonMode.Compass ? "Sigue" : "Repite";
        return $"Tu turno\n{action} {FormatStepCount(stepCount)}\n{BuildReplayHint()}";
    }

    private string FormatStepCount(int stepCount)
    {
        if (simonMode == SimonMode.Compass)
            return stepCount == 1 ? "1 tiempo" : $"{stepCount} tiempos";

        return stepCount == 1 ? "1 paso" : $"{stepCount} pasos";
    }

    private string BuildReplayHint()
    {
        if (maxReplaysPerLevel < 0)
            return "Repeticiones libres";

        if (maxReplaysPerLevel == 0)
            return "Sin repeticion";

        return maxReplaysPerLevel == 1 ? "Puedes repetir una vez" : $"Puedes repetir {maxReplaysPerLevel} veces";
    }

    private string BuildLevelCompleteMessage()
    {
        string bonusText = streakBonusEvery > 0 && currentStreak > 0 && currentStreak % streakBonusEvery == 0
            ? $"\nBonus de racha +{Mathf.Max(0, streakBonusPoints)}"
            : string.Empty;

        return $"Muy bien!\nSiguiente nivel {currentLevel}\nRacha x{currentStreak}{bonusText}";
    }

    private void UpdateTempoForRound()
    {
        int roundIndex = Mathf.Max(0, currentLevel - 1);
        int increaseSteps = Mathf.Max(0, roundIndex / Mathf.Max(1, roundsPerSpeedIncrease));
        currentBPM = Mathf.Min(maximumBPM, initialBPM + increaseSteps * bpmIncrease);

        if (metronomeController != null)
            metronomeController.SetBpm(currentBPM * GameSettings.GameplaySpeed);
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

        if (pct <= lowThreshold)
        {
            timeBarFill.color = timeLowColor;
            StartCriticalTimeBarPulse();
        }
        else
        {
            StopCriticalTimeBarPulse();
            timeBarFill.color = pct <= warnThreshold ? timeWarnColor : timeOkColor;
        }
    }

    private void GameOver(string reason)
    {
        StopAllCoroutines();
        ResetTransientVisualState();

        currentState = GameState.GameOver;
        isRoundTransitioning = false;
        SetInput(false);
        SetCompassGuideVisible(false);
        SetStartButtonVisible(true);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (metronomeController != null)
            metronomeController.StopMetronome();

        if (gameOverClip != null && sfxSource != null)
            sfxSource.PlayOneShot(gameOverClip);

        TriggerHapticError();
        if (celebrationEffect != null)
            celebrationEffect.PlayErrorEffect(Vector3.zero);

        int nivelAlcanzado = currentLevel;

        SetStatus($"Fin del juego\nLlegaste al nivel {nivelAlcanzado}\nPulsa JUGAR para intentarlo de nuevo", errorColor);
        StartHideStatusCountdown(gameOverMessageSeconds);
        StartBoardStateTransition(boardIdleScale);

        attemptExitReason = string.IsNullOrEmpty(reason) ? "game_over" : reason;
        ReportAttempt(false);
        sequence.Clear();
        playerIndex = 0;
        currentLevel = 0;
    }

    private void SetInput(bool value)
    {
        for (int i = 0; i < activeZones.Count; i++)
            activeZones[i].SetInteractable(value);
    }

    private void SetStatus(string msg, Color color, bool animate = true)
    {
        if (statusText == null) return;

        statusText.gameObject.SetActive(true);
        statusText.text = msg;
        statusText.color = color;
        UpdateStatusIcon(color);

        PlayOverlayFlash(color);

        if (!animate) return;

        if (statusAnim != null) StopCoroutine(statusAnim);

        if (color == errorColor)
            statusAnim = StartCoroutine(StatusShake());
        else
            statusAnim = StartCoroutine(StatusPop());
    }

    private IEnumerator ShowStatusThenHide(string msg, Color color, float visibleSeconds, bool animate = true)
    {
        SetStatus(msg, color, animate);

        yield return StartCoroutine(AnimateStatusVisibility(show: true));

        if (visibleSeconds > 0f)
            yield return new WaitForSeconds(visibleSeconds);

        yield return StartCoroutine(AnimateStatusVisibility(show: false));
        HideStatus();
    }

    private IEnumerator BeginSequenceAfterMessage(string msg, Color color, float visibleSeconds)
    {
        yield return StartCoroutine(ShowStatusThenHide(msg, color, visibleSeconds));
        showSequenceRoutine = simonMode == SimonMode.Compass
            ? StartCoroutine(ShowCompassSequence())
            : StartCoroutine(ShowSequence());
    }

    private void StartHideStatusCountdown(float visibleSeconds)
    {
        if (statusVisibilityRoutine != null)
            StopCoroutine(statusVisibilityRoutine);

        statusVisibilityRoutine = StartCoroutine(HideStatusAfterDelay(visibleSeconds));
    }

    private IEnumerator HideStatusAfterDelay(float visibleSeconds)
    {
        if (visibleSeconds > 0f)
            yield return new WaitForSeconds(visibleSeconds);

        HideStatus();
        statusVisibilityRoutine = null;
    }

    private void HideStatus()
    {
        if (statusText == null)
            return;

        if (statusAnim != null)
        {
            StopCoroutine(statusAnim);
            statusAnim = null;
        }

        statusText.rectTransform.localScale = Vector3.one;
        statusText.rectTransform.anchoredPosition = new Vector2(0f, statusMessageOffsetY);
        SetStatusAlpha(1f);
        statusText.gameObject.SetActive(false);
        HideStatusIcon();

        if (backgroundOverlay != null)
            backgroundOverlay.color = Color.clear;
    }

    private IEnumerator AnimateStatusVisibility(bool show)
    {
        if (statusText == null)
            yield break;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, statusFadeDuration);
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;
        float startScale = show ? statusHiddenScale : 1f;
        float endScale = show ? 1f : statusHiddenScale;

        if (show)
        {
            SetStatusAlpha(startAlpha);
            SetStatusScale(startScale);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            SetStatusAlpha(Mathf.Lerp(startAlpha, endAlpha, k));
            SetStatusScale(Mathf.Lerp(startScale, endScale, k));
            yield return null;
        }

        SetStatusAlpha(endAlpha);
        SetStatusScale(endScale);
    }

    private void SetStatusAlpha(float alpha)
    {
        if (statusText != null)
        {
            Color textColor = statusText.color;
            textColor.a = alpha;
            statusText.color = textColor;
        }

        if (statusIconImage != null && statusIconImage.gameObject.activeSelf)
        {
            Color iconColor = statusIconImage.color;
            iconColor.a = alpha;
            statusIconImage.color = iconColor;
        }
    }

    private void SetStatusScale(float scale)
    {
        if (statusText != null)
            statusText.rectTransform.localScale = Vector3.one * scale;

        if (statusIconImage != null && statusIconImage.gameObject.activeSelf)
            statusIconImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(scale, scale * statusIconPopScale, 0.35f);
    }

    private IEnumerator StatusPop()
    {
        RectTransform rt = statusText.rectTransform;
        Vector3 original = rt.localScale;

        rt.localScale = original * 0.9f;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / popDuration;
            rt.localScale = Vector3.Lerp(original * 0.9f, original * popScale, k);
            yield return null;
        }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / popDuration;
            rt.localScale = Vector3.Lerp(original * popScale, original, k);
            yield return null;
        }

        rt.localScale = original;
    }

    private IEnumerator StatusShake()
    {
        RectTransform rt = statusText.rectTransform;
        Vector2 original = rt.anchoredPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = 1f - Mathf.Clamp01(t / shakeDuration);
            float offsetX = Mathf.Sin(t * 70f) * shakeDistance * normalized;
            rt.anchoredPosition = original + new Vector2(offsetX, 0f);
            yield return null;
        }

        rt.anchoredPosition = original;
    }

    private void PlayOverlayFlash(Color color)
    {
        if (backgroundOverlay == null)
            return;

        Color targetColor = Color.clear;
        if (color == successColor)
            targetColor = bgSuccessColor;
        else if (color == errorColor)
            targetColor = bgErrorColor;
        else if (color == warningColor)
            targetColor = bgWarningColor;

        if (overlayFlashRoutine != null)
            StopCoroutine(overlayFlashRoutine);

        overlayFlashRoutine = StartCoroutine(FadeOverlay(targetColor));
    }

    private IEnumerator FadeOverlay(Color flashColor)
    {
        backgroundOverlay.color = flashColor;

        if (flashColor == Color.clear)
            yield break;

        float t = 0f;
        while (t < overlayFlashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / overlayFlashDuration);
            backgroundOverlay.color = Color.Lerp(flashColor, Color.clear, k);
            yield return null;
        }

        backgroundOverlay.color = Color.clear;
        overlayFlashRoutine = null;
    }

    private void ShowOn(int idx)
    {
        if (idx < 0 || idx >= activeZones.Count) return;

        Sprite litSprite = GetLitBoardSprite(currentColorCount, idx);
        if (activeBoardImage != null && litSprite != null)
            activeBoardImage.sprite = litSprite;

        activeZones[idx].SetHighlighted(GetZoneAccentColor(idx));
    }

    private Color GetZoneAccentColor(int index)
    {
        if (index < 0 || index >= colorPool.Count || colorPool[index] == null)
            return Color.white;

        return colorPool[index].accentColor;
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
        isRoundTransitioning = false;

        if (showSequenceRoutine != null)
        {
            StopCoroutine(showSequenceRoutine);
            showSequenceRoutine = null;
        }

        SetInput(false);

        lives--;
        attemptErrors++;
        attemptLevelRepetitions++;
        currentStreak = 0;
        replaysUsedThisLevel = 0;
        UpdateLivesUI();
        AnimateLifeLost(lives);
        PlayBoardErrorFeedback();

        if (celebrationEffect != null)
            celebrationEffect.PlayErrorEffect(Vector3.zero);

        if (lives <= 0)
        {
            GameOver(reason);
            loseLifeRoutine = null;
            yield break;
        }

        if (sfxSource != null) sfxSource.Stop();
        if (failClip != null && sfxSource != null) sfxSource.PlayOneShot(failClip);

        yield return StartCoroutine(ShowStatusThenHide($"{reason}\nTe quedan {lives} vidas", errorColor, feedbackMessageSeconds));
        ShowAllOff();

        playerIndex = 0;
        compassAwaitingBeatInput = false;
        compassBeatResolved = false;

        showSequenceRoutine = StartCoroutine(simonMode == SimonMode.Compass ? ShowCompassSequence() : ShowSequence());
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
            scoreText.text = "Puntos " + score;
            scoreText.color = scoreColor;
        }
    }

    private void AnimateScorePop()
    {
        if (scoreText == null)
            return;

        if (scoreAnimRoutine != null)
            StopCoroutine(scoreAnimRoutine);

        scoreAnimRoutine = StartCoroutine(AnimateRectTransformPop(scoreText.rectTransform, scoreBaseScale, hudPopScale, hudPopDuration));
    }

    private void AnimateLifeLost(int emptyLifeIndex)
    {
        if (lifeIcons == null || emptyLifeIndex < 0 || emptyLifeIndex >= lifeIcons.Length)
            return;

        Image lifeImage = lifeIcons[emptyLifeIndex];
        if (lifeImage == null)
            return;

        if (lifeAnimRoutines.TryGetValue(emptyLifeIndex, out Coroutine runningRoutine) && runningRoutine != null)
            StopCoroutine(runningRoutine);

        lifeAnimRoutines[emptyLifeIndex] = StartCoroutine(AnimateLifeIconLoss(emptyLifeIndex, lifeImage));
    }

    private IEnumerator AnimateLifeIconLoss(int iconIndex, Image lifeImage)
    {
        RectTransform rect = lifeImage.rectTransform;
        Vector3 baseScale = rect.localScale;
        Color baseColor = lifeImage.color;
        float elapsed = 0f;

        while (elapsed < hudPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / hudPopDuration);
            rect.localScale = Vector3.Lerp(baseScale * 1.18f, baseScale, k);
            lifeImage.color = Color.Lerp(errorColor, Color.white, k);
            yield return null;
        }

        rect.localScale = baseScale;
        lifeImage.color = baseColor;
        lifeAnimRoutines[iconIndex] = null;
    }

    private IEnumerator AnimateRectTransformPop(RectTransform rect, Vector3 baseScaleValue, float targetScaleMultiplier, float duration)
    {
        if (rect == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            rect.localScale = Vector3.Lerp(baseScaleValue, baseScaleValue * targetScaleMultiplier, k);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            rect.localScale = Vector3.Lerp(baseScaleValue * targetScaleMultiplier, baseScaleValue, k);
            yield return null;
        }

        rect.localScale = baseScaleValue;
    }

    private void StartCriticalTimeBarPulse()
    {
        if (timeBarFill == null || timeBarPulseRoutine != null)
            return;

        timeBarPulseRoutine = StartCoroutine(CriticalTimeBarPulseRoutine());
    }

    private void StopCriticalTimeBarPulse()
    {
        if (timeBarFill == null)
            return;

        if (timeBarPulseRoutine != null)
        {
            StopCoroutine(timeBarPulseRoutine);
            timeBarPulseRoutine = null;
        }

        timeBarFill.rectTransform.localScale = timeBarBaseScale;
    }

    private void ResetTransientVisualState()
    {
        StopCriticalTimeBarPulse();
        HideStatus();
        SetCompassGuideVisible(false);
    }

    private void EnsureCompassGuide()
    {
        if (compassGuideRoot == null)
        {
            GameObject foundRoot = GameObject.Find("CompassGuideRoot");
            if (foundRoot != null)
                compassGuideRoot = foundRoot.GetComponent<RectTransform>();
        }

        if (compassGuideRoot == null)
            return;

        compassGuideCanvasGroup = compassGuideRoot.GetComponent<CanvasGroup>();
        if (compassGuideCanvasGroup == null)
            compassGuideCanvasGroup = compassGuideRoot.gameObject.AddComponent<CanvasGroup>();

        compassGuideView = compassGuideRoot.GetComponent<CompassGuideView>();
        if (compassGuideView == null)
            compassGuideView = compassGuideRoot.gameObject.AddComponent<CompassGuideView>();

        compassGuideView.InitializeIfNeeded(beatsPerMeasure);
    }

    private void SetCompassGuideVisible(bool visible)
    {
        if (compassGuideRoot == null)
            return;

        if (compassGuideCanvasGroup == null)
            compassGuideCanvasGroup = compassGuideRoot.GetComponent<CanvasGroup>();

        if (compassGuideCanvasGroup == null)
            compassGuideCanvasGroup = compassGuideRoot.gameObject.AddComponent<CanvasGroup>();

        // Keep the root active because the scene currently nests the EventSystem under CompassGuideRoot.
        compassGuideRoot.gameObject.SetActive(true);
        compassGuideCanvasGroup.alpha = visible ? 1f : 0f;
        compassGuideCanvasGroup.interactable = false;
        compassGuideCanvasGroup.blocksRaycasts = false;
    }

    private void RefreshCompassGuideForMeasure(int measureStartIndex)
    {
        if (compassGuideView == null)
            return;

        int measureNumber = measureStartIndex / beatsPerMeasure + 1;
        int totalMeasures = Mathf.Max(1, Mathf.CeilToInt(compassSequence.Count / (float)beatsPerMeasure));
        CompassBeatDisplay[] display = new CompassBeatDisplay[beatsPerMeasure];
        for (int i = 0; i < beatsPerMeasure; i++)
        {
            int index = measureStartIndex + i;
            CompassBeatStep step = index < compassSequence.Count ? compassSequence[index] : null;

            display[i] = new CompassBeatDisplay
            {
                isSilence = step != null && step.type == CompassBeatType.Silence,
                label = step == null ? "-" : step.type == CompassBeatType.Silence ? string.Empty : colorPool[step.colorIndex].label,
                color = step != null && step.type == CompassBeatType.Color ? GetCompassBeatColor(step.colorIndex) : Color.white,
            };
        }

        compassGuideView.RefreshMeasure(display, measureNumber, totalMeasures, compassSlotIdleColor, compassSlotSilenceColor, compassSilenceSprite);
    }

    private void UpdateCompassGuideBeatState(int globalBeatIndex, Color color)
    {
        if (compassGuideView == null || globalBeatIndex < 0 || globalBeatIndex >= compassSequence.Count)
            return;

        int beatIndex = globalBeatIndex % beatsPerMeasure;
        CompassBeatStep step = compassSequence[globalBeatIndex];
        CompassBeatDisplay display = new CompassBeatDisplay
        {
            isSilence = step != null && step.type == CompassBeatType.Silence,
            label = step == null ? string.Empty : step.type == CompassBeatType.Silence ? string.Empty : colorPool[step.colorIndex].label,
            color = step != null && step.type == CompassBeatType.Color ? GetCompassBeatColor(step.colorIndex) : Color.white,
        };
        compassGuideView.UpdateBeatState(beatIndex, color, display);
    }

    private Color GetCompassBeatColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= colorPool.Count || colorPool[colorIndex] == null)
            return Color.white;

        return colorPool[colorIndex].accentColor;
    }

    private IEnumerator RunCompassBeatCountdown(float beatDuration)
    {
        float elapsed = 0f;
        ResetCompassBeatBar();

        while (elapsed < beatDuration)
        {
            elapsed += Time.deltaTime;
            UpdateCompassBeatBar(elapsed / beatDuration);
            yield return null;
        }

        ResetCompassBeatBar();
    }

    private void UpdateCompassBeatBar(float progress)
    {
        if (timeBarFill == null)
            return;

        float clamped = Mathf.Clamp01(progress);
        timeBarFill.fillAmount = 1f - clamped;
        timeBarFill.color = Color.Lerp(compassSlotActiveColor, compassSlotSuccessColor, clamped);
    }

    private void ResetCompassBeatBar()
    {
        if (timeBarFill == null)
            return;

        timeBarFill.fillAmount = 1f;
        timeBarFill.color = compassSlotActiveColor;
    }

    private IEnumerator CriticalTimeBarPulseRoutine()
    {
        while (timeBarFill != null)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 12f) * (criticalBarPulseScale - 1f);
            timeBarFill.rectTransform.localScale = timeBarBaseScale * pulse;
            yield return null;
        }

        timeBarPulseRoutine = null;
    }

    private void StartBoardStateTransition(float targetScaleMultiplier)
    {
        if (activeBoardRoot == null)
            return;

        if (boardStateRoutine != null)
            StopCoroutine(boardStateRoutine);

        boardStateRoutine = StartCoroutine(AnimateBoardState(targetScaleMultiplier));
    }

    private IEnumerator AnimateBoardState(float targetScaleMultiplier)
    {
        if (activeBoardRoot == null)
            yield break;

        Vector3 startScale = activeBoardRoot.localScale;
        Vector3 targetScale = boardBaseScale * targetScaleMultiplier;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, boardStateTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            activeBoardRoot.localScale = Vector3.Lerp(startScale, targetScale, k);
            yield return null;
        }

        activeBoardRoot.localScale = targetScale;
        boardStateRoutine = null;
    }

    private void PlayBoardSuccessPulse()
    {
        if (activeBoardRoot == null)
            return;

        if (boardFeedbackRoutine != null)
            StopCoroutine(boardFeedbackRoutine);

        boardFeedbackRoutine = StartCoroutine(AnimateBoardSuccessPulse());
    }

    private IEnumerator AnimateBoardSuccessPulse()
    {
        if (activeBoardRoot == null)
            yield break;

        Vector3 startScale = activeBoardRoot.localScale;
        Vector3 peakScale = boardBaseScale * boardSuccessScale;
        float elapsed = 0f;

        while (elapsed < hudPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / hudPopDuration);
            activeBoardRoot.localScale = Vector3.Lerp(startScale, peakScale, k);
            yield return null;
        }

        elapsed = 0f;
        Vector3 targetScale = boardBaseScale * (currentState == GameState.PlayerTurn ? boardPlayerTurnScale : boardObserveScale);
        if (currentState == GameState.Idle || currentState == GameState.GameOver)
            targetScale = boardBaseScale * boardIdleScale;

        while (elapsed < hudPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / hudPopDuration);
            activeBoardRoot.localScale = Vector3.Lerp(peakScale, targetScale, k);
            yield return null;
        }

        activeBoardRoot.localScale = targetScale;
        boardFeedbackRoutine = null;
    }

    private void PlayBoardErrorFeedback()
    {
        if (activeBoardRoot == null)
            return;

        if (boardFeedbackRoutine != null)
            StopCoroutine(boardFeedbackRoutine);

        boardFeedbackRoutine = StartCoroutine(AnimateBoardErrorShake());
    }

    private IEnumerator AnimateBoardErrorShake()
    {
        if (activeBoardRoot == null)
            yield break;

        Vector2 originalPosition = boardBaseAnchoredPosition;
        float elapsed = 0f;

        while (elapsed < boardShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = 1f - Mathf.Clamp01(elapsed / boardShakeDuration);
            float offsetX = Mathf.Sin(elapsed * 75f) * boardShakeDistance * normalized;
            activeBoardRoot.anchoredPosition = originalPosition + new Vector2(offsetX, 0f);
            yield return null;
        }

        activeBoardRoot.anchoredPosition = originalPosition;
        boardFeedbackRoutine = null;
    }

    private void PlayBoardCenterRing()
    {
        EnsureBoardCenterRing();
        if (boardCenterRingText == null)
            return;

        if (boardRingRoutine != null)
            StopCoroutine(boardRingRoutine);

        boardRingRoutine = StartCoroutine(AnimateBoardCenterRing());
    }

    private void EnsureBoardCenterRing()
    {
        if (activeBoardRoot == null)
            return;

        Transform existingRing = activeBoardRoot.Find("BoardCenterRing");
        if (existingRing != null)
        {
            boardCenterRingText = existingRing.GetComponent<TMP_Text>();
        }

        if (boardCenterRingText == null)
        {
            GameObject ringObject = new GameObject("BoardCenterRing", typeof(RectTransform));
            RectTransform ringRect = ringObject.GetComponent<RectTransform>();
            ringRect.SetParent(activeBoardRoot, false);
            ringRect.anchorMin = new Vector2(0.5f, 0.5f);
            ringRect.anchorMax = new Vector2(0.5f, 0.5f);
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = Vector2.zero;
            ringRect.sizeDelta = new Vector2(boardRingFontSize * 1.5f, boardRingFontSize * 1.5f);

            TextMeshProUGUI ringText = ringObject.AddComponent<TextMeshProUGUI>();
            ringText.text = "◌";
            ringText.alignment = TextAlignmentOptions.Center;
            ringText.raycastTarget = false;
            ringText.enableAutoSizing = false;
            ringText.fontSize = boardRingFontSize;
            ringText.color = boardRingColor;

            if (statusText != null)
                ringText.font = statusText.font;

            boardCenterRingText = ringText;
        }

        if (boardCenterRingText != null)
        {
            boardCenterRingText.transform.SetParent(activeBoardRoot, false);
            boardCenterRingText.rectTransform.anchoredPosition = Vector2.zero;
            boardCenterRingText.rectTransform.localScale = Vector3.one * boardRingStartScale;
            Color color = boardRingColor;
            color.a = 0f;
            boardCenterRingText.color = color;
            boardCenterRingText.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateBoardCenterRing()
    {
        if (boardCenterRingText == null)
            yield break;

        RectTransform ringRect = boardCenterRingText.rectTransform;
        Color startColor = boardRingColor;
        startColor.a = 0f;
        Color peakColor = boardRingColor;
        Color endColor = boardRingColor;
        endColor.a = 0f;

        boardCenterRingText.gameObject.SetActive(true);
        ringRect.localScale = Vector3.one * boardRingStartScale;
        boardCenterRingText.color = startColor;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, boardRingDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            ringRect.localScale = Vector3.one * Mathf.Lerp(boardRingStartScale, boardRingEndScale, k);
            boardCenterRingText.color = Color.Lerp(k < 0.35f ? startColor : peakColor, k < 0.35f ? peakColor : endColor, k < 0.35f ? k / 0.35f : (k - 0.35f) / 0.65f);
            yield return null;
        }

        boardCenterRingText.color = endColor;
        ringRect.localScale = Vector3.one * boardRingStartScale;
        boardCenterRingText.gameObject.SetActive(false);
        boardRingRoutine = null;
    }

    private void UpdateBoardForLevel()
    {
        int level = Mathf.Max(1, currentLevel);
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
        activeBoardRoot = board != null ? board.boardRoot : null;
        activeBoardImage = board != null ? board.baseBoardImage : null;
        activeZonesRoot = board != null ? board.zonesRoot : null;

        if (activeBoardRoot != null)
        {
            boardBaseScale = activeBoardRoot.localScale;
            boardBaseAnchoredPosition = activeBoardRoot.anchoredPosition;
        }

        EnsureBoardCenterRing();

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
        {
            ResolveHudReferences();
            menuButton ??= FindButtonInScene("BtnMenu") ?? FindButtonInScene("BtnExIt") ?? FindButtonInScene("MenuButton");
        }

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

    private void ResolveHudReferences()
    {
        Transform topHud = FindTopHudPanel();
        if (topHud == null)
            return;

        ApplyTopHudStyle(topHud);

        if (topHudPanelView == null)
            topHudPanelView = topHud.GetComponent<TopHudPanelView>();

        if (topHudPanelView == null)
            topHudPanelView = topHud.gameObject.AddComponent<TopHudPanelView>();

        topHudPanelView.ResolveMissingReferences();

        scoreText = topHudPanelView.ScoreText;
        menuButton = topHudPanelView.MenuButton;
        lifeIcons = topHudPanelView.LifeIcons;
        timeBarRoot = topHudPanelView.TimeBarRoot ?? FindTimeBarPanel(topHud)?.gameObject;
        timeBarFill = topHudPanelView.TimeBarFill;

        if (timeBarFill == null && timeBarRoot != null)
            timeBarFill = FindImage(timeBarRoot.transform, "Fill") ?? FindImage(timeBarRoot.transform, "TimeBarFill");
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

    private void ConfigureStatusOverlay()
    {
        if (statusText == null)
            return;

        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.anchoredPosition = new Vector2(0f, statusMessageOffsetY);
        statusRect.sizeDelta = statusMessageSize;

        statusText.gameObject.SetActive(true);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.raycastTarget = false;
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.overflowMode = TextOverflowModes.Ellipsis;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMax = statusMessageMaxFontSize;
        statusText.fontSizeMin = statusMessageMinFontSize;
        statusText.fontStyle = FontStyles.Bold;

        ConfigureStatusIcon();

        if (backgroundOverlay != null)
        {
            backgroundOverlay.raycastTarget = false;
            backgroundOverlay.color = Color.clear;
        }

        statusText.gameObject.SetActive(false);
    }

    private void ConfigureStatusIcon()
    {
        if (statusIconImage == null)
            return;

        RectTransform iconRect = statusIconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, statusMessageOffsetY + statusIconOffsetY);
        iconRect.sizeDelta = statusIconSize;
        statusIconImage.preserveAspect = true;
        statusIconImage.raycastTarget = false;
        statusIconImage.gameObject.SetActive(false);
    }

    private void UpdateStatusIcon(Color color)
    {
        if (statusIconImage == null)
            return;

        Sprite iconSprite = GetStatusSprite(color);
        if (iconSprite == null)
        {
            statusIconImage.gameObject.SetActive(false);
            return;
        }

        statusIconImage.sprite = iconSprite;
        statusIconImage.color = Color.white;
        statusIconImage.gameObject.SetActive(true);
    }

    private void HideStatusIcon()
    {
        if (statusIconImage == null)
            return;

        statusIconImage.gameObject.SetActive(false);
    }

    private Sprite GetStatusSprite(Color color)
    {
        if (color == successColor)
            return successStatusSprite;

        if (color == errorColor)
            return errorStatusSprite;

        if (color == warningColor)
            return warningStatusSprite != null ? warningStatusSprite : neutralStatusSprite;

        return neutralStatusSprite;
    }

    private void PauseGame()
    {
        if (pausePanel == null || isPaused)
            return;

        isPaused = true;
        if (pausePanelView != null)
            pausePanelView.Show();
        else
            pausePanel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetInput(false);
        if (metronomeController != null)
            metronomeController.PauseMetronome();

        SetStartButtonInteractable(false);
        if (replayButton != null)
            replayButton.interactable = false;

        if (pausePanelTransition == null)
            StartPausePanelTransition(show: true);
    }

    private void ResumeGame()
    {
        if (!isPaused)
            return;

        if (pausePanelView != null)
            pausePanelView.Hide(FinishResumeGame);
        else
            StartPausePanelTransition(show: false, onComplete: FinishResumeGame);
    }

    private void FinishResumeGame()
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

        SetStartButtonInteractable(currentState != GameState.ShowingSequence);
        if (replayButton != null)
            replayButton.interactable = replayButton.gameObject.activeSelf && currentState != GameState.ShowingSequence;

        SetInput(currentState == GameState.PlayerTurn);

        if (currentState == GameState.PlayerTurn)
            HideStatus();
        else if (currentState == GameState.ShowingSequence)
            HideStatus();
    }

    private void ConfigurePausePanelVisuals()
    {
        if (pausePanel == null)
            return;

        pausePanelTransition = pausePanel.GetComponent<UIPanelTransition>();
        pausePanelView = pausePanel.GetComponent<PausePanelView>();
        pausePanelCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
        if (pausePanelCanvasGroup == null)
            pausePanelCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
        if (pausePanelView == null)
            pausePanelView = pausePanel.AddComponent<PausePanelView>();

        pausePanelCanvasGroup.alpha = 0f;
        pausePanelCanvasGroup.interactable = false;
        pausePanelCanvasGroup.blocksRaycasts = false;

        if (pausePanelView != null)
            pausePanelView.InitializeHidden();
    }

    private void StartPausePanelTransition(bool show, Action onComplete = null)
    {
        if (pausePanelCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (pausePanelRoutine != null)
            StopCoroutine(pausePanelRoutine);

        pausePanelRoutine = StartCoroutine(AnimatePausePanel(show, onComplete));
    }

    private IEnumerator AnimatePausePanel(bool show, Action onComplete)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, pausePanelFadeDuration);
        float startAlpha = pausePanelCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        RectTransform rect = pausePanel.transform as RectTransform;

        if (rect != null)
            rect.localScale = show ? Vector3.one * 0.96f : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            pausePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, k);
            if (rect != null)
            {
                float scale = show ? Mathf.Lerp(0.96f, 1f, k) : Mathf.Lerp(1f, 0.96f, k);
                rect.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        pausePanelCanvasGroup.alpha = endAlpha;
        pausePanelCanvasGroup.interactable = show;
        pausePanelCanvasGroup.blocksRaycasts = show;
        if (rect != null)
            rect.localScale = Vector3.one;

        pausePanelRoutine = null;
        onComplete?.Invoke();
    }

    private void RestartScene()
    {
        if (metronomeController != null)
            metronomeController.StopMetronome();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        attemptExitReason = "restart";
        ReportAttempt(false);
        SceneTransitionController.ReloadCurrentScene();
    }

    private void LoadMainScene()
    {
        if (metronomeController != null)
            metronomeController.StopMetronome();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        attemptExitReason = lives > 0 ? "return_to_menu" : "game_over_return";
        ReportAttempt(lives > 0);
        SceneTransitionController.LoadScene("MainScene");
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

    private static Transform FindTopHudPanel()
    {
        GameObject found = GameObject.Find("TopHudPanel");
        return found != null ? found.transform : null;
    }

    private static void ApplyTopHudStyle(Transform topHud)
    {
        if (topHud == null)
            return;

        TopHudPanelStyle style = topHud.GetComponent<TopHudPanelStyle>();
        if (style == null)
            style = topHud.gameObject.AddComponent<TopHudPanelStyle>();

        style.Apply();
    }

    private static Image FindImage(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform match = FindChild(root, objectName);
        return match != null ? match.GetComponent<Image>() : null;
    }

    private static Transform FindTimeBarPanel(Transform topHud)
    {
        Transform timeBarPanel = FindChild(topHud, "TimeBar")
            ?? FindChild(topHud, "TimeBarRoot")
            ?? FindChild(topHud, "Timebar")
            ?? FindChild(topHud, "TimeBarBG");

        if (timeBarPanel != null)
            return timeBarPanel;

        return FindSceneChild("TimeBarBG")
            ?? FindSceneChild("TimeBar")
            ?? FindSceneChild("TimeBarRoot")
            ?? FindSceneChild("Timebar");
    }

    private static Transform FindSceneChild(string objectName)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform match = FindChild(roots[i].transform, objectName);
                if (match != null)
                    return match;
            }
        }

        return null;
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
        return 60f / Mathf.Max(1f, currentBPM * GameSettings.GameplaySpeed);
    }

    private void ApplyGameSettings()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxBaseVolume * GameSettings.EffectsVolume;

        if (metronomeController != null)
            metronomeController.SetBpm(currentBPM * GameSettings.GameplaySpeed);
    }

    private void ReportAttempt(bool completed)
    {
        if (attemptReportSubmitted || GameReportManager.Instance == null || SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
            return;

        attemptReportSubmitted = true;
        AttemptReportData report = new AttemptReportData
        {
            miniGame = "simon_dice",
            level = Mathf.Max(1, currentLevel),
            difficulty = currentColorCount <= 4 ? "facil" : currentColorCount == 5 ? "media" : "dificil",
            bpm = Mathf.RoundToInt(currentBPM * GameSettings.GameplaySpeed),
            errors = attemptErrors,
            correctAnswers = attemptCorrectAnswers,
            levelRepetitions = attemptLevelRepetitions,
            completed = completed,
            timeSeconds = Mathf.Max(1, Mathf.RoundToInt(Time.time - attemptStartedAt)),
            scoreFinal = score,
            wasTutorial = false,
            exitReason = attemptExitReason,
        };

        GameReportManager.Instance.ReportAttempt(report, null, error => Debug.LogWarning("Simon report error: " + error));
    }
}
