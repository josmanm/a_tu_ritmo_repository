using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BasicRhythmGameManager : MonoBehaviour
{
    private enum FigureType
    {
        Redonda,
        Blanca,
        Negra,
        Corchea,
        Silencio,
    }

    [System.Serializable]
    private class FigureDefinition
    {
        public FigureType type;
        public string displayName;
        public float noteBeats;
        public Sprite sprite;
        public Sprite[] orderedSprites;
    }

    [Header("Figuras")]
    [SerializeField] private Sprite redondaSprite;
    [SerializeField] private Sprite blancaSprite;
    [SerializeField] private Sprite blancaASprite;
    [SerializeField] private Sprite blancaBSprite;
    [SerializeField] private Sprite negraSprite;
    [SerializeField] private Sprite corcheaSprite;
    [SerializeField] private Sprite corcheaMaSprite;
    [SerializeField] private Sprite corcheaRiSprite;
    [SerializeField] private Sprite corcheaPoSprite;
    [SerializeField] private Sprite corcheaSaSprite;
    [SerializeField] private Sprite silencioSprite;
    [SerializeField] private float silencioBeats = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip metronomeClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failClip;

    [Header("Audio Tutorial")]
    [SerializeField] private AudioClip tutorialIntroClip;
    [SerializeField] private AudioClip redondaVoiceClip;
    [SerializeField] private AudioClip blancaVoiceClip;
    [SerializeField] private AudioClip negraVoiceClip;
    [SerializeField] private AudioClip corcheaVoiceClip;

    [Header("Audio Juego")]
    [SerializeField] private AudioClip yourTurnVoiceClip;
    [SerializeField] private AudioClip veryGoodVoiceClip;
    [SerializeField] private AudioClip waitALittleVoiceClip;
    [SerializeField] private AudioClip observeVoiceClip;
    [SerializeField] private AudioClip tryAgainVoiceClip;

    [Header("Juego")]
    [SerializeField] private float bpm = 80f;
    [SerializeField] private float totalPatternBeats = 4f;
    [SerializeField] private int patternMeasures = 4;
    [SerializeField] [Range(0.4f, 1f)] private float longPatternRequiredHitRatio = 0.55f;
    [SerializeField] private float failureAdvanceDelay = 0.35f;
    [SerializeField] private float roundIntroDelay = 0.6f;
    [SerializeField] private float roundOutroDelay = 1f;
    [SerializeField] private float perfectToleranceBeats = 0.12f;
    [SerializeField] private float goodToleranceBeats = 0.25f;
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int maxConsecutiveFigureRepeats = 2;
    [SerializeField] private float figureRecencyWeightStep = 0.75f;

    [Header("Feedback")]
    [SerializeField] private Color normalFeedbackColor = Color.white;
    [SerializeField] private Color successFeedbackColor = new Color(0.1f, 1f, 0.2f, 1f);
    [SerializeField] private Color failFeedbackColor = new Color(1f, 0.12f, 0.12f, 1f);
    [SerializeField] private Color timingCueColor = new Color(1f, 0.85f, 0.1f, 1f);
    [SerializeField] private Color noteLitColor = Color.white;
    [SerializeField] private Color noteDimColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color longNoteFillColor = new Color(0.35f, 0.8f, 1f, 0.75f);
    [SerializeField] private float iconSpacing = 16f;
    [SerializeField] private float drumPulseScale = 1.08f;
    [SerializeField] private float drumPulseDuration = 0.12f;

    [Header("UI Juice")]
    [SerializeField] private float feedbackPanelAlpha = 0.14f;
    [SerializeField] private float feedbackPopScale = 1.06f;
    [SerializeField] private float feedbackAnimDuration = 0.14f;
    [SerializeField] private float hudPopScale = 1.08f;
    [SerializeField] private float hudAnimDuration = 0.12f;
    [SerializeField] private float drumReadyPulseScale = 1.03f;
    [SerializeField] private float drumReadyPulseSpeed = 4.6f;
    [SerializeField] private int focusedPatternThreshold = 5;
    [SerializeField] private float focusedPatternIconSize = 240f;
    [SerializeField] private Vector2 focusedPatternPosition = new Vector2(0f, 40f);
    [SerializeField] private Vector2 focusedPatternAreaSize = new Vector2(760f, 360f);
    [SerializeField] private float focusedBlockSlideDistance = 90f;
    [SerializeField] private float focusedBlockTransitionDuration = 0.18f;
    [SerializeField] private float activeFillMoveDistance = 10f;
    [SerializeField] private float activeFillMoveSpeed = 4.5f;
    [SerializeField] private float noteResultHoldSeconds = 0.12f;
    [SerializeField] private float statusMessageDuration = 0.45f;
    [SerializeField] private float statusMessageLongDuration = 0.65f;
    [SerializeField] private float statusMessagePopScale = 1.08f;
    [SerializeField] private float statusMessageAnimDuration = 0.16f;
    [SerializeField] private Sprite successStatusSprite;
    [SerializeField] private Sprite regularStatusSprite;
    [SerializeField] private Sprite failStatusSprite;
    [SerializeField] private Sprite observeStatusSprite;
    [SerializeField] private Sprite turnStatusSprite;
    [SerializeField] private Sprite waitStatusSprite;
    [SerializeField] private Sprite silenceStatusSprite;
    [SerializeField] private Color roundCelebrationColor = new Color(1f, 0.94f, 0.72f, 0.92f);
    [SerializeField] private Color perfectRoundCelebrationColor = new Color(1f, 0.98f, 0.82f, 0.95f);
    [SerializeField] private Color tutorialCompletionCelebrationColor = new Color(0.78f, 0.95f, 1f, 0.95f);
    [SerializeField] private float roundCelebrationDuration = 0.42f;

    private readonly List<FigureDefinition> figures = new List<FigureDefinition>();
    private readonly List<Image> patternIcons = new List<Image>();
    private readonly List<Image> patternFillImages = new List<Image>();
    private readonly List<FigureDefinition> currentPattern = new List<FigureDefinition>();
    private readonly List<int> currentPatternBlockNoteCounts = new List<int>();

    private TMP_Text scoreText;
    private TMP_Text levelText;
    private TMP_Text feedbackText;
    private Image figureIconTemplate;
    private RectTransform rhythmPanel;
    private Button drumButton;
    private Button menuButton;
    private GameObject pausePanel;
    private Button resumeButton;
    private Button restartButton;
    private Button closeButton;
    private Button pauseMenuButton;
    private Image[] lifeImages;
    private AudioSource sfxSource;
    private AudioSource voiceSource;
    private Image drumButtonImage;
    private Color drumButtonBaseColor = Color.white;
    private PausePanelView pausePanelView;
    private float sfxBaseVolume = 1f;
    private float voiceBaseVolume = 1f;
    private Canvas rootCanvas;
    private TMP_Text statusOverlayText;
    private CanvasGroup statusOverlayGroup;
    private RectTransform statusOverlayRect;
    private Image statusOverlayIconImage;
    private RectTransform feedbackPanelRect;
    private Image feedbackPanelImage;
    private RectTransform scorePanelRect;
    private RectTransform levelPanelRect;
    private RectTransform livesPanelRect;
    private TMP_Text focusedBlockProgressText;
    private Vector3 feedbackPanelBaseScale = Vector3.one;
    private Vector3 scorePanelBaseScale = Vector3.one;
    private Vector3 levelPanelBaseScale = Vector3.one;
    private Vector3 livesPanelBaseScale = Vector3.one;

    private Coroutine roundRoutine;
    private Coroutine drumPulseRoutine;
    private Coroutine drumReadyRoutine;
    private Coroutine feedbackAnimRoutine;
    private Coroutine statusOverlayRoutine;
    private Coroutine scoreHudRoutine;
    private Coroutine levelHudRoutine;
    private Coroutine livesHudRoutine;
    private Coroutine focusedBlockTransitionRoutine;
    private Coroutine roundCelebrationRoutine;
    private bool waitingForInput;
    private float inputPhaseStartTime;
    private int score;
    private int level;
    private int lives;
    private int currentNoteIndex;
    private int noteCountInPattern;
    private bool roundUsedOnlyPerfectTiming;
    private int roundSuccessfulHits;
    private int roundFailures;
    private bool roundCompletedSuccessfully;
    private bool attemptFinished;
    private bool isPaused;
    private bool tutorialActive = true;
    private int tutorialStepIndex;
    private bool tutorialDemoShownForCurrentStep;
    private bool tutorialWarmupCompleted;
    private int tutorialDrumWarmupRemaining;
    private int tutorialFillWarmupRemaining;
    private bool tutorialFillWarmupWaitingForTap;
    private bool tutorialFillWarmupAttemptCompleted;
    private int currentNoteTapCount;
    private int sustainedBeatCuesPlayed;
    private int lastShownScore = -1;
    private int lastShownLevel = -999;
    private int lastShownLives = -1;
    private bool useFocusedPatternPresentation;
    private readonly List<int> patternBlockIndexByNote = new List<int>();
    private readonly List<Vector2> focusedBasePositions = new List<Vector2>();
    private int lastFocusedBlockIndex = -1;
    private bool noteTransitionLocked;
    private FigureType? lastGeneratedFigureType;
    private int consecutiveGeneratedFigureRepeats;
    private TMP_Text roundCelebrationRingText;
    private Color currentRoundCelebrationColor;
    private readonly Dictionary<FigureType, int> roundsSinceFigureShown = new Dictionary<FigureType, int>();
    private bool isShowingDemoPattern;
    private float attemptStartedAt;
    private int attemptErrors;
    private int attemptCorrectAnswers;
    private int attemptLevelRepetitions;
    private bool attemptReportSubmitted;
    private string attemptExitReason = "completed";

    private void Start()
    {
        GameSettings.EnsureInitialized();
        ResolveReferences();
        if (!ValidateReferences())
            return;

        BuildFigureDefinitions();
        EnsureAudioSource();
        EnsureDrumInput();
        EnsureStatusOverlay();
        EnsureFocusedBlockProgressText();
        EnsureRoundCelebrationRing();

        score = 0;
        level = 0;
        lives = maxLives;
        attemptStartedAt = Time.time;
        attemptErrors = 0;
        attemptCorrectAnswers = 0;
        attemptLevelRepetitions = 0;
        attemptReportSubmitted = false;
        attemptExitReason = "completed";

        UpdateHud();
        SetFeedback(ComposeFeedback("Escucha"), normalFeedbackColor);

        roundRoutine = StartCoroutine(RunRoundLoop());
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameSettings.SettingsChanged -= ApplyGameSettings;
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += ApplyGameSettings;
        ApplyGameSettings();
    }

    private bool InTutorialDrumWarmup()
    {
        return tutorialActive && !tutorialWarmupCompleted && tutorialDrumWarmupRemaining > 0;
    }

    private bool InTutorialFillWarmup()
    {
        return tutorialActive && !tutorialWarmupCompleted && tutorialFillWarmupRemaining > 0;
    }

    private void Update()
    {
        if (isPaused)
            return;

        if (InTutorialDrumWarmup() || InTutorialFillWarmup())
            return;

        if (noteTransitionLocked)
            return;

        FigureDefinition currentNote = GetCurrentNote();
        if (!waitingForInput || currentNote == null)
            return;

        float beatDuration = GetBeatDuration();
        float expectedStart = inputPhaseStartTime + GetPatternBeatsBefore(currentNoteIndex) * beatDuration;
        float expectedEnd = expectedStart + currentNote.noteBeats * beatDuration;

        UpdateAllLongNoteFills(Time.time);

        if (IsSilenceFigure(currentNote))
        {
            UpdateTimingCue(false);

            if (Time.time >= expectedEnd)
                CompleteSilenceNote();

            return;
        }

        if (UsesSegmentedTapFigure(currentNote))
        {
            float progress = Mathf.Clamp01((Time.time - expectedStart) / Mathf.Max(0.01f, currentNote.noteBeats * beatDuration));
            UpdateTimingCue(Mathf.Abs(Time.time - GetExpectedTapTime(currentNote, currentNoteTapCount + 1, expectedStart, beatDuration)) <= perfectToleranceBeats * beatDuration);
            UpdateSegmentedTapCues(currentNote, progress);

            if (Time.time > GetExpectedTapTime(currentNote, currentNoteTapCount + 1, expectedStart, beatDuration) + goodToleranceBeats * beatDuration)
            {
                ResolveFailure("Muy tarde");
                return;
            }

            return;
        }

        if (UsesLongNoteTapCue(currentNote))
        {
            UpdateTimingCue(Mathf.Abs(Time.time - expectedEnd) <= perfectToleranceBeats * beatDuration);

            if (Time.time > expectedEnd + goodToleranceBeats * beatDuration)
            {
                ResolveFailure("Muy tarde");
                return;
            }

            return;
        }
    }

    public void OnDrumPointerDown()
    {
        if (HandleTutorialWarmupTap())
            return;

        if (isPaused || !waitingForInput || noteTransitionLocked)
            return;

        FigureDefinition currentNote = GetCurrentNote();
        if (currentNote == null)
            return;

        if (IsSilenceFigure(currentNote))
        {
            ResolveFailure("No tocar");
            return;
        }

        if (UsesSegmentedTapFigure(currentNote))
        {
            EvaluateSegmentedTap(Time.time);
            if (drumButton != null)
                drumButton.transform.localScale = Vector3.one * 0.95f;
            return;
        }

        if (UsesLongNoteTapCue(currentNote))
        {
            EvaluateLongNoteTap(Time.time);
            if (drumButton != null)
                drumButton.transform.localScale = Vector3.one * 0.95f;
            return;
        }
    }

    public void OnDrumPointerUp()
    {
        if (isPaused)
            return;

        if (drumButton != null)
            drumButton.transform.localScale = Vector3.one;
    }

    private IEnumerator RunRoundLoop()
    {
        yield return new WaitForSeconds(roundIntroDelay);

        if (tutorialActive && !tutorialWarmupCompleted)
            yield return StartCoroutine(RunTutorialWarmup());

        while (lives > 0)
        {
            PrepareNextPattern();
            roundFailures = 0;
            roundCompletedSuccessfully = false;

            while (!roundCompletedSuccessfully && lives > 0)
            {
                attemptFinished = false;

                if (!tutorialActive || !tutorialDemoShownForCurrentStep)
                {
                    yield return StartCoroutine(PlayPreRoundVoiceIfNeeded());
                    yield return StartCoroutine(ShowDemoPattern());
                    tutorialDemoShownForCurrentStep = true;
                }

                SetFeedback(ComposeFeedback(GetInputInstruction()), normalFeedbackColor);
                yield return StartCoroutine(PlayBlockingVoice(yourTurnVoiceClip));

                waitingForInput = true;
                currentNoteIndex = 0;
                currentNoteTapCount = 0;
                sustainedBeatCuesPlayed = 0;
                noteTransitionLocked = false;
                roundUsedOnlyPerfectTiming = true;
                roundSuccessfulHits = 0;
                inputPhaseStartTime = Time.time;

                if (focusedBlockTransitionRoutine != null)
                {
                    StopCoroutine(focusedBlockTransitionRoutine);
                    focusedBlockTransitionRoutine = null;
                }

                if (useFocusedPatternPresentation)
                {
                    lastFocusedBlockIndex = GetPatternBlockIndex(0);
                    ApplyFocusedBlockVisibilityInstant(lastFocusedBlockIndex);
                    ResetPatternPositions();
                }

                SetDrumInteractable(true);
                ResetPatternVisuals(false);

                yield return new WaitUntil(() => attemptFinished || lives <= 0);
            }

            if (lives <= 0)
                yield break;

            yield return new WaitForSeconds(roundOutroDelay);
        }
    }

    private void PrepareNextPattern()
    {
        BuildCurrentPattern();
        noteCountInPattern = currentPattern.Count;
        tutorialDemoShownForCurrentStep = false;
        currentNoteIndex = 0;
        currentNoteTapCount = 0;
        sustainedBeatCuesPlayed = 0;
        noteTransitionLocked = false;
        lastFocusedBlockIndex = -1;

        if (focusedBlockProgressText != null)
            focusedBlockProgressText.gameObject.SetActive(false);

        BuildPatternVisuals();
        SetDrumInteractable(false);
        drumButton.transform.localScale = Vector3.one;
    }

    private IEnumerator ShowDemoPattern()
    {
        SetFeedback(ComposeFeedback(GetDemoInstruction()), normalFeedbackColor);
        ResetPatternVisuals(true);
        isShowingDemoPattern = true;

        for (int i = 0; i < noteCountInPattern; i++)
        {
            FigureDefinition note = GetPatternNoteAt(i);
            float noteDuration = note.noteBeats * GetBeatDuration();

            if (useFocusedPatternPresentation)
                yield return StartCoroutine(EnsureFocusedBlockForNote(i, animate: true));

            HighlightPatternIcon(i, noteLitColor);
            yield return StartCoroutine(AnimateFillForDemo(i, note, noteDuration));

            HighlightPatternIcon(i, noteLitColor);
            SetFillAmount(i, 0f);
        }

        isShowingDemoPattern = false;
        ResetPatternPositions();
    }

    private IEnumerator AnimateFillForDemo(int noteIndex, FigureDefinition note, float duration)
    {
        SetFillAmount(noteIndex, 0f);

        float elapsed = 0f;

        if (UsesSegmentedTapFigure(note))
            sustainedBeatCuesPlayed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetFillAmount(noteIndex, progress);
            UpdateActiveFillMotion(noteIndex);

            if (UsesSegmentedTapFigure(note))
            {
                UpdateSegmentedTapCues(note, progress);
            }

            yield return null;
        }

        SetFillAmount(noteIndex, 1f);

        if (UsesSegmentedTapFigure(note))
        {
            UpdateSegmentedTapCues(note, 1f);
        }
        else if (!IsSilenceFigure(note))
        {
            PlaySfx(metronomeClip);
            PulseDrum(timingCueColor);
        }
    }

    private IEnumerator RunTutorialWarmup()
    {
        if (tutorialIntroClip != null)
            yield return StartCoroutine(PlayBlockingVoice(tutorialIntroClip));

        yield return StartCoroutine(RunDrumDiscoveryWarmup());
        yield return StartCoroutine(RunFillTimingWarmup());

        tutorialWarmupCompleted = true;
        ClearPatternVisuals();
    }

    private IEnumerator RunDrumDiscoveryWarmup()
    {
        tutorialDrumWarmupRemaining = 5;
        SetDrumInteractable(true);
        SetFeedback("Toca el tambor 5 veces", normalFeedbackColor);

        while (tutorialDrumWarmupRemaining > 0)
            yield return null;

        SetDrumInteractable(false);
        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator RunFillTimingWarmup()
    {
        FigureDefinition practiceFigure = GetFigure(FigureType.Negra);
        if (practiceFigure == null)
            yield break;

        currentPattern.Clear();
        currentPatternBlockNoteCounts.Clear();
        currentPattern.Add(practiceFigure);
        currentPatternBlockNoteCounts.Add(1);
        noteCountInPattern = 1;
        BuildPatternVisuals();
        SetDrumInteractable(true);

        tutorialFillWarmupRemaining = 3;
        while (tutorialFillWarmupRemaining > 0)
        {
            tutorialFillWarmupAttemptCompleted = false;
            tutorialFillWarmupWaitingForTap = false;
            HighlightPatternIcon(0, noteLitColor);
            SetFillColor(0, longNoteFillColor);
            SetFillAmount(0, 0f);
            SetFeedback("Toca cuando se llene: " + tutorialFillWarmupRemaining, normalFeedbackColor);

            float duration = practiceFigure.noteBeats * GetBeatDuration();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetFillAmount(0, progress);
                yield return null;
            }

            SetFillAmount(0, 1f);
            tutorialFillWarmupWaitingForTap = true;
            PlaySfx(metronomeClip);
            PulseDrum(timingCueColor);
            SetFeedback("Ahora toca el tambor: " + tutorialFillWarmupRemaining, timingCueColor);

            while (!tutorialFillWarmupAttemptCompleted)
                yield return null;

            tutorialFillWarmupRemaining--;
            yield return new WaitForSeconds(0.2f);
        }

        tutorialFillWarmupWaitingForTap = false;
        SetDrumInteractable(false);
    }

    private bool HandleTutorialWarmupTap()
    {
        if (InTutorialDrumWarmup())
        {
            tutorialDrumWarmupRemaining--;
            PlaySfx(metronomeClip);
            PulseDrum(timingCueColor);

            if (tutorialDrumWarmupRemaining > 0)
                SetFeedback("Toca el tambor: " + tutorialDrumWarmupRemaining, normalFeedbackColor);
            else
                SetFeedback("Muy bien", successFeedbackColor);

            return true;
        }

        if (InTutorialFillWarmup())
        {
            if (!tutorialFillWarmupWaitingForTap)
            {
                SetFeedback("Espera a que se llene", normalFeedbackColor);
                PulseDrum(timingCueColor);
                return true;
            }

            tutorialFillWarmupWaitingForTap = false;
            tutorialFillWarmupAttemptCompleted = true;
            SetPatternResultColor(0, successFeedbackColor, true);
            PlaySfx(metronomeClip);
            PulseDrum(successFeedbackColor);
            SetFeedback("Muy bien", successFeedbackColor);
            return true;
        }

        return false;
    }

    private void UpdateAllLongNoteFills(float currentTime)
    {
        if (currentPattern.Count == 0)
            return;

        float beatDuration = GetBeatDuration();
        ResetPatternPositions();

        for (int i = 0; i < noteCountInPattern; i++)
        {
            FigureDefinition note = GetPatternNoteAt(i);
            float noteDuration = note.noteBeats * beatDuration;
            float noteStart = inputPhaseStartTime + GetPatternBeatsBefore(i) * beatDuration;

            if (i < currentNoteIndex)
                continue;

            if (i == currentNoteIndex)
            {
                float progress = Mathf.Clamp01((currentTime - noteStart) / Mathf.Max(0.01f, noteDuration));
                SetFillAmount(i, progress);
                UpdateActiveFillMotion(i);
            }
            else
            {
                SetFillAmount(i, 0f);
            }

            if (i > currentNoteIndex)
            {
                HighlightPatternIcon(i, noteLitColor);
                SetFillColor(i, longNoteFillColor);
            }
        }

        if (currentNoteIndex >= 0 && currentNoteIndex < patternIcons.Count)
        {
            HighlightPatternIcon(currentNoteIndex, noteLitColor);
            SetFillColor(currentNoteIndex, longNoteFillColor);
        }
    }

    private void EvaluateLongNoteTap(float tapTime)
    {
        FigureDefinition currentNote = GetCurrentNote();
        if (currentNote == null)
            return;

        float beatDuration = GetBeatDuration();
        float expectedStart = inputPhaseStartTime + GetPatternBeatsBefore(currentNoteIndex) * beatDuration;
        float expectedEnd = expectedStart + currentNote.noteBeats * beatDuration;
        float timingErrorBeats = Mathf.Abs(tapTime - expectedEnd) / beatDuration;

        if (timingErrorBeats <= perfectToleranceBeats)
        {
            roundSuccessfulHits++;
            attemptCorrectAnswers++;
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
            PlaySuccessCue(currentNote);
            PulseDrum(successFeedbackColor);
        }
        else if (timingErrorBeats <= goodToleranceBeats)
        {
            roundSuccessfulHits++;
            attemptCorrectAnswers++;
            roundUsedOnlyPerfectTiming = false;
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
            PlaySuccessCue(currentNote);
            PulseDrum(successFeedbackColor);
        }
        else
        {
            ResolveFailure(tapTime < expectedEnd ? "Muy pronto" : "Muy tarde");
            return;
        }

        QueueAdvanceToNextNote();
    }

    private void EvaluateSegmentedTap(float tapTime)
    {
        FigureDefinition currentNote = GetCurrentNote();
        if (!UsesSegmentedTapFigure(currentNote))
            return;

        float beatDuration = GetBeatDuration();
        float expectedStart = inputPhaseStartTime + GetPatternBeatsBefore(currentNoteIndex) * beatDuration;
        int nextTapIndex = currentNoteTapCount + 1;
        float expectedTapTime = GetExpectedTapTime(currentNote, nextTapIndex, expectedStart, beatDuration);
        float timingErrorBeats = Mathf.Abs(tapTime - expectedTapTime) / beatDuration;

        if (timingErrorBeats > goodToleranceBeats)
        {
            ResolveFailure(tapTime < expectedTapTime ? "Muy pronto" : "Muy tarde");
            return;
        }

        if (timingErrorBeats > perfectToleranceBeats)
            roundUsedOnlyPerfectTiming = false;

        currentNoteTapCount++;
        PulseDrum(successFeedbackColor);

        if (currentNoteTapCount < GetRequiredTapCount(currentNote))
            return;

        roundSuccessfulHits++;
        attemptCorrectAnswers++;
        SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
        QueueAdvanceToNextNote();
    }

    private void CompleteSilenceNote()
    {
        FigureDefinition currentNote = GetCurrentNote();
        if (!IsSilenceFigure(currentNote))
            return;

        roundSuccessfulHits++;
        attemptCorrectAnswers++;
        SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
        QueueAdvanceToNextNote(true);
    }

    private void CompletePatternSuccess()
    {
        waitingForInput = false;
        roundCompletedSuccessfully = true;

        bool isPerfectAttempt = roundUsedOnlyPerfectTiming && roundFailures == 0 && roundSuccessfulHits == noteCountInPattern;
        bool tutorialWillComplete = tutorialActive && tutorialStepIndex + 1 >= 5;

        score += isPerfectAttempt ? 10 : 5;

        if (tutorialActive)
        {
            tutorialStepIndex++;
            if (tutorialStepIndex >= 5)
            {
                tutorialActive = false;
                level = 1;
            }
        }
        else
        {
            level++;
        }

        UpdateHud();
        PlaySfx(successClip);
        currentRoundCelebrationColor = tutorialWillComplete
            ? tutorialCompletionCelebrationColor
            : isPerfectAttempt ? perfectRoundCelebrationColor : roundCelebrationColor;
        PlayRoundCelebration();
        attemptFinished = true;
        waitingForInput = false;

        if (isPerfectAttempt)
        {
            SetFeedback(ComposeFeedback("Perfecto +10"), successFeedbackColor);
            PlayVoice(veryGoodVoiceClip);
        }
        else
        {
            SetFeedback(ComposeFeedback("Bien +5"), successFeedbackColor);
        }
    }

    private void ResolveFailure(string reason)
    {
        currentNoteTapCount = 0;
        sustainedBeatCuesPlayed = 0;

        if (drumButton != null)
        {
            drumButton.transform.localScale = Vector3.one;
            SetDrumInteractable(false);
        }

        if (currentNoteIndex >= 0 && currentNoteIndex < patternIcons.Count)
            SetPatternResultColor(currentNoteIndex, failFeedbackColor, UsesLongNoteTapCue(GetCurrentNote()));

        if (currentNoteIndex >= 0 && !UsesLongNoteTapCue(GetCurrentNote()))
            SetFillAmount(currentNoteIndex, 0f);

        roundFailures++;
        attemptErrors++;
        PlaySfx(failClip);
        PulseDrum(failFeedbackColor);
        SetFeedback(ComposeFeedback(reason), failFeedbackColor);

        QueueAdvanceToNextNote(true);
    }

    private void AdvanceToNextNote(bool resetTimingWindow = false)
    {
        currentNoteIndex++;
        currentNoteTapCount = 0;
        sustainedBeatCuesPlayed = 0;

        if (currentNoteIndex >= noteCountInPattern)
        {
            FinishCurrentAttempt();
            return;
        }

        if (useFocusedPatternPresentation && GetPatternBlockIndex(currentNoteIndex) != lastFocusedBlockIndex)
        {
            StartCoroutine(EnterFocusedGameplayBlock(resetTimingWindow));
            return;
        }

        if (drumButton != null)
            SetDrumInteractable(true);

        if (resetTimingWindow)
            inputPhaseStartTime = Time.time + failureAdvanceDelay - GetPatternBeatsBefore(currentNoteIndex) * GetBeatDuration();

        SetFeedback(ComposeFeedback(GetInputInstruction()), normalFeedbackColor);
    }

    private void QueueAdvanceToNextNote(bool resetTimingWindow = false)
    {
        if (noteTransitionLocked)
            return;

        StartCoroutine(AdvanceToNextNoteAfterDelay(resetTimingWindow));
    }

    private IEnumerator AdvanceToNextNoteAfterDelay(bool resetTimingWindow)
    {
        noteTransitionLocked = true;
        if (drumButton != null)
            SetDrumInteractable(false);

        yield return new WaitForSeconds(noteResultHoldSeconds);
        AdvanceToNextNote(resetTimingWindow);
        if (!useFocusedPatternPresentation || GetPatternBlockIndex(currentNoteIndex) == lastFocusedBlockIndex)
            noteTransitionLocked = false;
    }

    private IEnumerator EnterFocusedGameplayBlock(bool resetTimingWindow)
    {
        yield return StartCoroutine(EnsureFocusedBlockForNote(currentNoteIndex, animate: true));

        float baseTime = Time.time + (resetTimingWindow ? failureAdvanceDelay : 0f);
        inputPhaseStartTime = baseTime - GetPatternBeatsBefore(currentNoteIndex) * GetBeatDuration();

        if (drumButton != null)
            SetDrumInteractable(true);

        SetFeedback(ComposeFeedback(GetInputInstruction()), normalFeedbackColor);
        noteTransitionLocked = false;
    }

    private void FinishCurrentAttempt()
    {
        waitingForInput = false;
        attemptFinished = true;

        if (drumButton != null)
            SetDrumInteractable(false);

        int requiredHits = GetRequiredHitsForPattern();
        if (roundSuccessfulHits >= requiredHits)
        {
            CompletePatternSuccess();
            return;
        }

        if (tutorialActive)
            tutorialDemoShownForCurrentStep = false;

        attemptLevelRepetitions++;
        lives--;
        UpdateHud();

        if (lives <= 0)
        {
            SetFeedback(ComposeFeedback("Sin vidas"), failFeedbackColor);
            attemptExitReason = "sin_vidas";
            ReportAttempt(false);
            if (roundRoutine != null)
                StopCoroutine(roundRoutine);
            return;
        }

        SetFeedback(ComposeFeedback("Pierdes 1 vida"), failFeedbackColor);
        PlayVoice(tryAgainVoiceClip);
    }

    private int GetRequiredHitsForPattern()
    {
        if (tutorialActive)
            return noteCountInPattern;

        if (!tutorialActive && patternMeasures > 1)
            return Mathf.Clamp(Mathf.CeilToInt(noteCountInPattern * longPatternRequiredHitRatio), 1, noteCountInPattern);

        if (noteCountInPattern <= 1)
            return 1;

        if (noteCountInPattern == 2)
            return 2;

        if (noteCountInPattern == 4)
            return 3;

        if (noteCountInPattern == 8)
            return 5;

        return Mathf.Max(1, noteCountInPattern);
    }

    private void BuildCurrentPattern()
    {
        currentPattern.Clear();
        currentPatternBlockNoteCounts.Clear();
        patternBlockIndexByNote.Clear();

        if (tutorialActive)
        {
            switch (tutorialStepIndex)
            {
                case 0:
                    AddPatternBlock(FigureType.Redonda, 1);
                    break;
                case 1:
                    AddPatternBlock(FigureType.Blanca, 2);
                    break;
                case 2:
                    AddPatternBlock(FigureType.Negra, 4);
                    break;
                case 3:
                    AddPatternBlock(FigureType.Silencio, 4);
                    break;
                default:
                    AddPatternBlock(FigureType.Corchea, 8);
                    break;
            }
            return;
        }

        for (int i = 0; i < Mathf.Max(1, patternMeasures); i++)
            BuildHomogeneousPatternBlock();
    }

    private void BuildHomogeneousPatternBlock()
    {
        List<FigureDefinition> candidates = new List<FigureDefinition>();
        for (int i = 0; i < figures.Count; i++)
        {
            float repetitions = totalPatternBeats / figures[i].noteBeats;
            if (Mathf.Abs(repetitions - Mathf.Round(repetitions)) <= 0.001f)
                candidates.Add(figures[i]);
        }

        if (candidates.Count == 0)
            return;

        if (lastGeneratedFigureType.HasValue && consecutiveGeneratedFigureRepeats >= maxConsecutiveFigureRepeats)
            candidates.RemoveAll(candidate => candidate.type == lastGeneratedFigureType.Value);

        if (candidates.Count == 0)
        {
            for (int i = 0; i < figures.Count; i++)
            {
                float repetitions = totalPatternBeats / figures[i].noteBeats;
                if (Mathf.Abs(repetitions - Mathf.Round(repetitions)) <= 0.001f)
                    candidates.Add(figures[i]);
            }
        }

        FigureDefinition chosen = ChooseWeightedCandidate(candidates);
        RegisterGeneratedFigure(chosen.type);
        int repetitionsCount = Mathf.RoundToInt(totalPatternBeats / chosen.noteBeats);
        AddPatternBlock(chosen.type, repetitionsCount);
    }

    private void RegisterGeneratedFigure(FigureType type)
    {
        List<FigureType> figureTypes = new List<FigureType>();
        for (int i = 0; i < figures.Count; i++)
        {
            if (!figureTypes.Contains(figures[i].type))
                figureTypes.Add(figures[i].type);
        }

        for (int i = 0; i < figureTypes.Count; i++)
        {
            FigureType figureType = figureTypes[i];
            if (figureType == type)
                roundsSinceFigureShown[figureType] = 0;
            else
                roundsSinceFigureShown[figureType] = GetRoundsSinceShown(figureType) + 1;
        }

        if (lastGeneratedFigureType.HasValue && lastGeneratedFigureType.Value == type)
            consecutiveGeneratedFigureRepeats++;
        else
        {
            lastGeneratedFigureType = type;
            consecutiveGeneratedFigureRepeats = 1;
        }
    }

    private FigureDefinition ChooseWeightedCandidate(List<FigureDefinition> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        float totalWeight = 0f;
        float[] weights = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            float weight = 1f + GetRoundsSinceShown(candidates[i].type) * figureRecencyWeightStep;
            weights[i] = Mathf.Max(0.01f, weight);
            totalWeight += weights[i];
        }

        float randomPoint = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (randomPoint <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private int GetRoundsSinceShown(FigureType type)
    {
        return roundsSinceFigureShown.TryGetValue(type, out int value) ? value : 0;
    }

    private void AddPatternBlock(FigureType type, int count)
    {
        FigureDefinition figure = GetFigure(type);
        if (figure == null)
            return;

        int blockIndex = currentPatternBlockNoteCounts.Count;

        for (int i = 0; i < count; i++)
        {
            currentPattern.Add(figure);
            patternBlockIndexByNote.Add(blockIndex);
        }

        currentPatternBlockNoteCounts.Add(count);
    }

    private FigureDefinition GetFigure(FigureType type)
    {
        for (int i = 0; i < figures.Count; i++)
        {
            if (figures[i].type == type)
                return figures[i];
        }

        return null;
    }

    private FigureDefinition GetPatternNoteAt(int index)
    {
        if (index < 0 || index >= currentPattern.Count)
            return null;

        return currentPattern[index];
    }

    private FigureDefinition GetCurrentNote()
    {
        return GetPatternNoteAt(currentNoteIndex);
    }

    private float GetPatternBeatsBefore(int index)
    {
        float beats = 0f;
        for (int i = 0; i < index && i < currentPattern.Count; i++)
            beats += currentPattern[i].noteBeats;

        return beats;
    }

    private void BuildPatternVisuals()
    {
        ClearPatternVisuals();

        if (figureIconTemplate == null || rhythmPanel == null)
            return;

        figureIconTemplate.gameObject.SetActive(false);

        useFocusedPatternPresentation = noteCountInPattern >= focusedPatternThreshold;
        if (useFocusedPatternPresentation)
        {
            BuildFocusedPatternVisuals();
            ResetPatternVisuals(true);
            return;
        }

        RectTransform templateRect = figureIconTemplate.rectTransform;
        float availableWidth = Mathf.Max(560f, rhythmPanel.rect.width - 90f);
        float availableHeight = Mathf.Max(340f, rhythmPanel.rect.height - 70f);
        float templateWidth = templateRect.rect.width > 0f ? templateRect.rect.width : 140f;
        float templateHeight = templateRect.rect.height > 0f ? templateRect.rect.height : 140f;
        int blockCount = Mathf.Max(1, currentPatternBlockNoteCounts.Count);
        int blocksPerRow = blockCount <= 2 ? blockCount : 2;
        int blockRows = Mathf.CeilToInt(blockCount / 2f);
        float blockSpacing = iconSpacing * 1.2f;
        float blockWidth = (availableWidth - blockSpacing * (blocksPerRow - 1)) / blocksPerRow;
        float blockHeight = (availableHeight - blockSpacing * (blockRows - 1)) / blockRows;

        int maxBlockColumns = 1;
        int maxBlockRows = 1;
        for (int i = 0; i < currentPatternBlockNoteCounts.Count; i++)
        {
            maxBlockColumns = Mathf.Max(maxBlockColumns, Mathf.Min(4, currentPatternBlockNoteCounts[i]));
            maxBlockRows = Mathf.Max(maxBlockRows, Mathf.CeilToInt(currentPatternBlockNoteCounts[i] / 4f));
        }

        float iconWidth = (blockWidth - iconSpacing * (maxBlockColumns - 1)) / maxBlockColumns;
        float iconHeight = (blockHeight - iconSpacing * (maxBlockRows - 1)) / maxBlockRows;
        float iconSize = Mathf.Clamp(Mathf.Min(iconWidth, iconHeight, templateWidth * 2f, templateHeight * 2f), 84f, 210f);
        float rowHeight = iconSize + iconSpacing;
        int patternIndex = 0;

        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            int notesInBlock = blockIndex < currentPatternBlockNoteCounts.Count ? currentPatternBlockNoteCounts[blockIndex] : noteCountInPattern;
            int blockRow = blockIndex / blocksPerRow;
            int blockColumn = blockIndex % blocksPerRow;
            float blockGridWidth = blocksPerRow * blockWidth + (blocksPerRow - 1) * blockSpacing;
            float blockGridHeight = blockRows * blockHeight + (blockRows - 1) * blockSpacing;
            float blockX = -blockGridWidth * 0.5f + blockWidth * 0.5f + blockColumn * (blockWidth + blockSpacing);
            float blockY = blockGridHeight * 0.5f - blockHeight * 0.5f - blockRow * (blockHeight + blockSpacing) + 10f;
            int noteRowsInBlock = Mathf.CeilToInt(notesInBlock / 4f);

            for (int noteIndex = 0; noteIndex < notesInBlock && patternIndex < noteCountInPattern; noteIndex++, patternIndex++)
            {
                Image icon = Instantiate(figureIconTemplate, rhythmPanel);
                icon.name = "PatternIcon_" + patternIndex;
                icon.sprite = GetVisualSpriteForPatternIndex(patternIndex, noteIndex);
                icon.preserveAspect = true;
                icon.gameObject.SetActive(true);

                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);

                int row = noteIndex / 4;
                int itemsInRow = row < noteRowsInBlock - 1 ? 4 : notesInBlock - row * 4;
                float rowWidth = itemsInRow * iconSize + (itemsInRow - 1) * iconSpacing;
                float rowStartX = -rowWidth * 0.5f + iconSize * 0.5f;
                int column = noteIndex % 4;
                float localX = rowStartX + column * (iconSize + iconSpacing);
                float localY = (noteRowsInBlock - 1) * rowHeight * 0.5f - row * rowHeight;

                iconRect.anchoredPosition = new Vector2(blockX + localX, blockY + localY);
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);

                FigureDefinition patternNote = GetPatternNoteAt(patternIndex);
                patternIcons.Add(icon);
                patternFillImages.Add(CreateFillImage(icon));
                focusedBasePositions.Add(iconRect.anchoredPosition);

                if (UsesSegmentedTapFigure(patternNote))
                    CreateSegmentDividers(icon, GetRequiredTapCount(patternNote));
            }
        }

        ResetPatternVisuals(true);
    }

    private void BuildFocusedPatternVisuals()
    {
        focusedBasePositions.Clear();
        lastFocusedBlockIndex = -1;

        int patternIndex = 0;
        for (int blockIndex = 0; blockIndex < currentPatternBlockNoteCounts.Count; blockIndex++)
        {
            int notesInBlock = currentPatternBlockNoteCounts[blockIndex];
            int columns = Mathf.Min(4, Mathf.Max(1, notesInBlock));
            int rows = Mathf.CeilToInt(notesInBlock / 4f);
            float cellWidth = focusedPatternAreaSize.x / columns;
            float cellHeight = focusedPatternAreaSize.y / rows;
            float iconSize = Mathf.Clamp(Mathf.Min(cellWidth, cellHeight) - iconSpacing, 96f, focusedPatternIconSize);
                float totalHeight = rows * iconSize + (rows - 1) * iconSpacing;

            for (int noteIndex = 0; noteIndex < notesInBlock && patternIndex < noteCountInPattern; noteIndex++, patternIndex++)
            {
                Image icon = Instantiate(figureIconTemplate, rhythmPanel);
                icon.name = "PatternIcon_" + patternIndex;
                icon.sprite = GetVisualSpriteForPatternIndex(patternIndex, noteIndex);
                icon.preserveAspect = true;
                icon.gameObject.SetActive(true);

                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);

                int row = noteIndex / 4;
                int column = noteIndex % 4;
                float rowItems = Mathf.Min(4, notesInBlock - row * 4);
                float rowWidth = rowItems * iconSize + (rowItems - 1) * iconSpacing;
                float localX = -rowWidth * 0.5f + iconSize * 0.5f + column * (iconSize + iconSpacing);
                float localY = ((rows - 1) * (iconSize + iconSpacing) * 0.5f) - row * (iconSize + iconSpacing);
                Vector2 anchored = focusedPatternPosition + new Vector2(localX, localY);
                iconRect.anchoredPosition = anchored;
                iconRect.sizeDelta = new Vector2(iconSize, iconSize);

                FigureDefinition patternNote = GetPatternNoteAt(patternIndex);
                patternIcons.Add(icon);
                patternFillImages.Add(CreateFillImage(icon));
                focusedBasePositions.Add(anchored);

                if (UsesSegmentedTapFigure(patternNote))
                    CreateSegmentDividers(icon, GetRequiredTapCount(patternNote));
            }
        }
    }

    private void ClearPatternVisuals()
    {
        for (int i = 0; i < patternIcons.Count; i++)
        {
            if (patternIcons[i] != null)
                Destroy(patternIcons[i].gameObject);
        }

        patternIcons.Clear();
        patternFillImages.Clear();
        focusedBasePositions.Clear();
    }

    private Sprite GetVisualSpriteForPatternIndex(int patternIndex, int indexWithinBlock)
    {
        FigureDefinition figure = GetPatternNoteAt(patternIndex);
        if (figure == null)
            return null;

        if (figure.orderedSprites != null && figure.orderedSprites.Length > 0)
        {
            Sprite orderedSprite = figure.orderedSprites[indexWithinBlock % figure.orderedSprites.Length];
            if (orderedSprite != null)
                return orderedSprite;
        }

        return figure.sprite;
    }

    private void ResetPatternVisuals(bool dimIcons)
    {
        Color iconColor = noteLitColor;

        for (int i = 0; i < patternIcons.Count; i++)
        {
            HighlightPatternIcon(i, iconColor);
            SetFillColor(i, longNoteFillColor);
            SetFillAmount(i, 0f);
        }

        if (useFocusedPatternPresentation)
        {
            lastFocusedBlockIndex = GetPatternBlockIndex(0);
            ApplyFocusedBlockVisibilityInstant(lastFocusedBlockIndex);
            UpdateFocusedBlockProgress(lastFocusedBlockIndex);
        }

        UpdateTimingCue(false);
    }

    private void HighlightPatternIcon(int index, Color color)
    {
        if (index < 0 || index >= patternIcons.Count || patternIcons[index] == null)
            return;

        patternIcons[index].color = color;
    }

    private void SetPatternResultColor(int index, Color color, bool fillFully)
    {
        HighlightPatternIcon(index, color);
        SetFillColor(index, color);
        if (fillFully)
            SetFillAmount(index, 1f);
    }

    private void SetFillAmount(int index, float amount)
    {
        if (index < 0 || index >= patternFillImages.Count || patternFillImages[index] == null)
            return;

        patternFillImages[index].fillAmount = Mathf.Clamp01(amount);
        patternFillImages[index].gameObject.SetActive(amount > 0f);
    }

    private void SetFillColor(int index, Color color)
    {
        if (index < 0 || index >= patternFillImages.Count || patternFillImages[index] == null)
            return;

        patternFillImages[index].color = color;
    }

    private void UpdateFocusedPatternVisibility(int activeIndex)
    {
        if (!useFocusedPatternPresentation)
            return;

        int activeBlockIndex = GetPatternBlockIndex(activeIndex);

        if (activeBlockIndex != lastFocusedBlockIndex)
        {
            lastFocusedBlockIndex = activeBlockIndex;
            UpdateFocusedBlockProgress(activeBlockIndex);
            if (focusedBlockTransitionRoutine != null)
            {
                StopCoroutine(focusedBlockTransitionRoutine);
                focusedBlockTransitionRoutine = null;
            }

            if (isShowingDemoPattern)
                focusedBlockTransitionRoutine = StartCoroutine(AnimateFocusedBlockTransition(activeBlockIndex));
            else
                ApplyFocusedBlockVisibilityInstant(activeBlockIndex);

            return;
        }

        ApplyFocusedBlockVisibilityInstant(activeBlockIndex);
    }

    private void ApplyFocusedBlockVisibilityInstant(int activeBlockIndex)
    {
        for (int i = 0; i < patternIcons.Count; i++)
        {
            bool visible = GetPatternBlockIndex(i) == activeBlockIndex;
            if (patternIcons[i] != null)
            {
                patternIcons[i].gameObject.SetActive(visible);
                if (visible)
                {
                    patternIcons[i].rectTransform.anchoredPosition = focusedBasePositions[i];
                    Color iconColor = patternIcons[i].color;
                    iconColor.a = 1f;
                    patternIcons[i].color = iconColor;
                }
            }

            if (i < patternFillImages.Count && patternFillImages[i] != null)
                patternFillImages[i].gameObject.SetActive(visible && patternFillImages[i].fillAmount > 0f);
        }
    }

    private IEnumerator EnsureFocusedBlockForNote(int noteIndex, bool animate)
    {
        if (!useFocusedPatternPresentation)
            yield break;

        int targetBlockIndex = GetPatternBlockIndex(noteIndex);
        if (targetBlockIndex < 0)
            yield break;

        if (targetBlockIndex == lastFocusedBlockIndex)
        {
            ApplyFocusedBlockVisibilityInstant(targetBlockIndex);
            yield break;
        }

        lastFocusedBlockIndex = targetBlockIndex;
        UpdateFocusedBlockProgress(targetBlockIndex);

        if (focusedBlockTransitionRoutine != null)
        {
            StopCoroutine(focusedBlockTransitionRoutine);
            focusedBlockTransitionRoutine = null;
        }

        if (animate)
            yield return StartCoroutine(AnimateFocusedBlockTransition(targetBlockIndex));
        else
            ApplyFocusedBlockVisibilityInstant(targetBlockIndex);
    }

    private void ResetPatternPositions()
    {
        int count = Mathf.Min(patternIcons.Count, focusedBasePositions.Count);
        for (int i = 0; i < count; i++)
        {
            if (patternIcons[i] != null)
                patternIcons[i].rectTransform.anchoredPosition = focusedBasePositions[i];
        }
    }

    private void UpdateActiveFillMotion(int activeIndex)
    {
        if (activeIndex < 0 || activeIndex >= patternIcons.Count || activeIndex >= focusedBasePositions.Count)
            return;

        float offsetY = Mathf.Sin(Time.time * activeFillMoveSpeed) * activeFillMoveDistance;

        if (patternIcons[activeIndex] != null)
            patternIcons[activeIndex].rectTransform.anchoredPosition = focusedBasePositions[activeIndex] + new Vector2(0f, offsetY);
    }

    private IEnumerator AnimateFocusedBlockTransition(int activeBlockIndex)
    {
        List<int> activeIndices = new List<int>();
        for (int i = 0; i < patternIcons.Count; i++)
        {
            bool visible = GetPatternBlockIndex(i) == activeBlockIndex;
            if (patternIcons[i] != null)
            {
                patternIcons[i].gameObject.SetActive(visible);
                if (visible)
                    activeIndices.Add(i);
            }

            if (i < patternFillImages.Count && patternFillImages[i] != null)
                patternFillImages[i].gameObject.SetActive(visible && patternFillImages[i].fillAmount > 0f);
        }

        float elapsed = 0f;
        while (elapsed < focusedBlockTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / focusedBlockTransitionDuration);
            for (int i = 0; i < activeIndices.Count; i++)
            {
                int iconIndex = activeIndices[i];
                if (patternIcons[iconIndex] == null)
                    continue;

                RectTransform rect = patternIcons[iconIndex].rectTransform;
                Vector2 targetPosition = focusedBasePositions[iconIndex];
                rect.anchoredPosition = Vector2.Lerp(targetPosition + new Vector2(focusedBlockSlideDistance, 0f), targetPosition, t);

                Color iconColor = patternIcons[iconIndex].color;
                iconColor.a = t;
                patternIcons[iconIndex].color = iconColor;

                if (iconIndex < patternFillImages.Count && patternFillImages[iconIndex] != null)
                {
                    Color fillColor = patternFillImages[iconIndex].color;
                    fillColor.a = t * longNoteFillColor.a;
                    patternFillImages[iconIndex].color = fillColor;
                }
            }

            yield return null;
        }

        for (int i = 0; i < activeIndices.Count; i++)
        {
            int iconIndex = activeIndices[i];
            if (patternIcons[iconIndex] == null)
                continue;

            patternIcons[iconIndex].rectTransform.anchoredPosition = focusedBasePositions[iconIndex];
            Color iconColor = patternIcons[iconIndex].color;
            iconColor.a = 1f;
            patternIcons[iconIndex].color = iconColor;

            if (iconIndex < patternFillImages.Count && patternFillImages[iconIndex] != null)
            {
                Color fillColor = patternFillImages[iconIndex].color;
                fillColor.a = longNoteFillColor.a;
                patternFillImages[iconIndex].color = fillColor;
            }
        }

        focusedBlockTransitionRoutine = null;
    }

    private int GetPatternBlockIndex(int noteIndex)
    {
        if (noteIndex < 0 || noteIndex >= patternBlockIndexByNote.Count)
            return -1;

        return patternBlockIndexByNote[noteIndex];
    }

    private void EnsureFocusedBlockProgressText()
    {
        if (rhythmPanel == null)
            return;

        Transform existing = rhythmPanel.Find("FocusedBlockProgressText");
        if (existing == null)
        {
            GameObject textObject = new GameObject("FocusedBlockProgressText", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(rhythmPanel, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = focusedPatternPosition + new Vector2(0f, -220f);
            rect.sizeDelta = new Vector2(380f, 56f);

            focusedBlockProgressText = textObject.GetComponent<TextMeshProUGUI>();
            focusedBlockProgressText.alignment = TextAlignmentOptions.Center;
            focusedBlockProgressText.enableAutoSizing = true;
            focusedBlockProgressText.fontSizeMin = 22f;
            focusedBlockProgressText.fontSizeMax = 34f;
            focusedBlockProgressText.fontStyle = FontStyles.Bold;
            focusedBlockProgressText.color = new Color(1f, 1f, 1f, 0.92f);
            focusedBlockProgressText.raycastTarget = false;
            if (feedbackText != null)
                focusedBlockProgressText.font = feedbackText.font;
        }
        else
        {
            focusedBlockProgressText = existing.GetComponent<TMP_Text>();
        }

        if (focusedBlockProgressText != null)
            focusedBlockProgressText.gameObject.SetActive(false);
    }

    private void UpdateFocusedBlockProgress(int activeBlockIndex)
    {
        if (focusedBlockProgressText == null)
            return;

        if (!useFocusedPatternPresentation || activeBlockIndex < 0)
        {
            focusedBlockProgressText.gameObject.SetActive(false);
            return;
        }

        focusedBlockProgressText.gameObject.SetActive(true);
        focusedBlockProgressText.text = "Bloque " + (activeBlockIndex + 1) + "/" + currentPatternBlockNoteCounts.Count;
    }

    private void EnsureRoundCelebrationRing()
    {
        if (rhythmPanel == null)
            return;

        Transform existing = rhythmPanel.Find("RoundCelebrationRing");
        if (existing == null)
        {
            GameObject ringObject = new GameObject("RoundCelebrationRing", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = ringObject.GetComponent<RectTransform>();
            rect.SetParent(rhythmPanel, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = focusedPatternPosition;
            rect.sizeDelta = new Vector2(420f, 420f);

            roundCelebrationRingText = ringObject.GetComponent<TextMeshProUGUI>();
            roundCelebrationRingText.text = "◌";
            roundCelebrationRingText.alignment = TextAlignmentOptions.Center;
            roundCelebrationRingText.enableAutoSizing = false;
            roundCelebrationRingText.fontSize = 220f;
            roundCelebrationRingText.color = roundCelebrationColor;
            roundCelebrationRingText.raycastTarget = false;
            if (feedbackText != null)
                roundCelebrationRingText.font = feedbackText.font;
        }
        else
        {
            roundCelebrationRingText = existing.GetComponent<TMP_Text>();
        }

        if (roundCelebrationRingText != null)
            roundCelebrationRingText.gameObject.SetActive(false);
    }

    private void PlayRoundCelebration()
    {
        if (roundCelebrationRingText == null)
            return;

        if (currentRoundCelebrationColor.a <= 0f)
            currentRoundCelebrationColor = roundCelebrationColor;

        if (roundCelebrationRoutine != null)
            StopCoroutine(roundCelebrationRoutine);

        roundCelebrationRoutine = StartCoroutine(AnimateRoundCelebration());
    }

    private IEnumerator AnimateRoundCelebration()
    {
        RectTransform rect = roundCelebrationRingText.rectTransform;
        Color startColor = currentRoundCelebrationColor;
        startColor.a = 0f;
        Color peakColor = currentRoundCelebrationColor;
        Color endColor = currentRoundCelebrationColor;
        endColor.a = 0f;

        roundCelebrationRingText.gameObject.SetActive(true);
        roundCelebrationRingText.color = startColor;
        rect.localScale = Vector3.one * 0.55f;

        float elapsed = 0f;
        while (elapsed < roundCelebrationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / roundCelebrationDuration);
            rect.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.4f, t);
            roundCelebrationRingText.color = Color.Lerp(t < 0.35f ? startColor : peakColor, t < 0.35f ? peakColor : endColor, t < 0.35f ? t / 0.35f : (t - 0.35f) / 0.65f);
            yield return null;
        }

        roundCelebrationRingText.gameObject.SetActive(false);
        rect.localScale = Vector3.one;
        roundCelebrationRoutine = null;
    }

    private Image CreateFillImage(Image parentIcon)
    {
        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(parentIcon.transform, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.sprite = parentIcon.sprite;
        fillImage.color = longNoteFillColor;
        fillImage.preserveAspect = true;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Vertical;
        fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImage.fillAmount = 0f;
        fillImage.raycastTarget = false;
        fillObject.SetActive(false);
        return fillImage;
    }

    private void CreateSegmentDividers(Image parentIcon, int segments)
    {
        if (segments <= 1)
            return;

        for (int i = 1; i < segments; i++)
        {
            GameObject dividerObject = new GameObject("Divider_" + i, typeof(RectTransform), typeof(Image));
            RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.SetParent(parentIcon.transform, false);
            dividerRect.anchorMin = new Vector2(0.18f, i / (float)segments);
            dividerRect.anchorMax = new Vector2(0.82f, i / (float)segments);
            dividerRect.pivot = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(0f, 4f);

            Image dividerImage = dividerObject.GetComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.85f);
            dividerImage.raycastTarget = false;
        }
    }

    private bool UsesSegmentedTapFigure(FigureDefinition figure)
    {
        return figure != null && figure.type == FigureType.Redonda;
    }

    private bool IsSilenceFigure(FigureDefinition figure)
    {
        return figure != null && figure.type == FigureType.Silencio;
    }

    private bool UsesLongNoteTapCue(FigureDefinition figure)
    {
        return figure != null && !UsesSegmentedTapFigure(figure) && !IsSilenceFigure(figure);
    }

    private int GetRequiredTapCount(FigureDefinition figure)
    {
        if (figure == null)
            return 0;

        switch (figure.type)
        {
            case FigureType.Redonda:
                return 4;
            default:
                return 0;
        }
    }

    private float GetExpectedTapTime(FigureDefinition figure, int tapIndex, float expectedStart, float beatDuration)
    {
        return expectedStart + Mathf.Clamp(tapIndex, 1, GetRequiredTapCount(figure)) * beatDuration;
    }

    private void UpdateSegmentedTapCues(FigureDefinition figure, float progress)
    {
        int beatCueCount = GetRequiredTapCount(figure);
        if (beatCueCount <= 0)
            return;

        while (sustainedBeatCuesPlayed < beatCueCount)
        {
            float cueThreshold = (sustainedBeatCuesPlayed + 1) / (float)beatCueCount;
            if (progress < cueThreshold)
                break;

            PlaySfx(metronomeClip);
            PulseDrum(timingCueColor);
            sustainedBeatCuesPlayed++;
        }
    }

    private void PlaySuccessCue(FigureDefinition figure)
    {
        if (!UsesSegmentedTapFigure(figure) && !IsSilenceFigure(figure))
            PlaySfx(metronomeClip);
    }

    private float GetBeatDuration()
    {
        return 60f / Mathf.Max(1f, bpm * GameSettings.GameplaySpeed);
    }

    private string GetDemoInstruction(FigureDefinition figure)
    {
        switch (figure.type)
        {
            case FigureType.Redonda:
                return "Redonda: 4 toques";
            case FigureType.Blanca:
                return "Blanca: escucha";
            case FigureType.Negra:
                return "Negra: escucha";
            case FigureType.Silencio:
                return "Silencio: espera";
            default:
                return "Corchea: escucha";
        }
    }

    private string GetDemoInstruction()
    {
        if (tutorialActive)
            return "Tutorial: " + GetPatternNoteAt(0).displayName;

        return "Escucha la secuencia";
    }

    private string GetInputInstruction(FigureDefinition figure)
    {
        switch (figure.type)
        {
            case FigureType.Redonda:
                return "Toca 4 veces";
            case FigureType.Blanca:
                return "Toca al llenarse";
            case FigureType.Negra:
                return "Toca al llenarse";
            case FigureType.Silencio:
                return "No toques";
            default:
                return "Toca al llenarse";
        }
    }

    private string GetInputInstruction()
    {
        FigureDefinition note = GetCurrentNote();
        if (note == null)
            note = GetPatternNoteAt(0);

        if (note != null)
            return GetInputInstruction(note);

        return "Sigue la secuencia";
    }

    private void UpdateHud()
    {
        bool scoreChanged = lastShownScore != score;
        int shownLevel = tutorialActive ? -1 : level;
        bool levelChanged = lastShownLevel != shownLevel;
        bool livesChanged = lastShownLives != lives;

        if (scoreText != null)
            scoreText.text = ""+score;

        if (levelText != null)
            levelText.text = tutorialActive ? "Tutorial" : "Nivel " + level;

        if (lifeImages == null)
            return;

        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null)
                continue;

            Color color = lifeImages[i].color;
            color.a = i < lives ? 1f : 0.25f;
            lifeImages[i].color = color;
        }

        if (scoreChanged)
            AnimateScorePanel();

        if (levelChanged)
            AnimateLevelPanel();

        if (livesChanged)
            AnimateLivesPanel();

        lastShownScore = score;
        lastShownLevel = shownLevel;
        lastShownLives = lives;
    }

    private void SetFeedback(string message, Color color)
    {
        if (statusOverlayText == null)
            return;

        RefreshFeedbackPanelStatus();

        if (color == normalFeedbackColor && waitingForInput)
        {
            HideStatusOverlay();
            return;
        }

        if (statusOverlayRoutine != null)
            StopCoroutine(statusOverlayRoutine);

        float duration = color == failFeedbackColor ? statusMessageLongDuration : statusMessageDuration;
        statusOverlayRoutine = StartCoroutine(ShowStatusOverlay(message, color, duration));
    }

    private string ComposeFeedback(string status)
    {
        return status;
    }

    private void UpdateTimingCue(bool active)
    {
        if (currentNoteIndex < 0 || currentNoteIndex >= patternIcons.Count)
            return;

        if (active)
            SetPatternPreviewColor(currentNoteIndex, timingCueColor);
        else
            SetPatternPreviewColor(currentNoteIndex, noteLitColor);
    }

    private void SetPatternPreviewColor(int index, Color color)
    {
        if (index < 0 || index >= patternIcons.Count)
            return;

        HighlightPatternIcon(index, color);
        SetFillColor(index, longNoteFillColor);
    }

    private void PulseDrum(Color pulseColor)
    {
        if (drumButton == null)
            return;

        StopDrumReadyPulse();

        if (drumPulseRoutine != null)
            StopCoroutine(drumPulseRoutine);

        drumPulseRoutine = StartCoroutine(PulseDrumRoutine(pulseColor));
    }

    private IEnumerator PulseDrumRoutine(Color pulseColor)
    {
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = baseScale * drumPulseScale;

        if (drumButtonImage != null)
            drumButtonImage.color = pulseColor;

        float elapsed = 0f;
        while (elapsed < drumPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drumPulseDuration);
            drumButton.transform.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < drumPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / drumPulseDuration);
            drumButton.transform.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }

        drumButton.transform.localScale = baseScale;
        if (drumButtonImage != null)
            drumButtonImage.color = drumButtonBaseColor;
        drumPulseRoutine = null;

        if (drumButton != null && drumButton.interactable)
            StartDrumReadyPulse();
    }

    private IEnumerator AnimateFeedbackPanel()
    {
        if (feedbackPanelRect == null)
            yield break;

        float elapsed = 0f;
        Vector3 peakScale = feedbackPanelBaseScale * feedbackPopScale;
        while (elapsed < feedbackAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / feedbackAnimDuration);
            feedbackPanelRect.localScale = Vector3.Lerp(feedbackPanelBaseScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < feedbackAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / feedbackAnimDuration);
            feedbackPanelRect.localScale = Vector3.Lerp(peakScale, feedbackPanelBaseScale, t);
            yield return null;
        }

        feedbackPanelRect.localScale = feedbackPanelBaseScale;
        feedbackAnimRoutine = null;
    }

    private IEnumerator ShowStatusOverlay(string message, Color color, float visibleDuration)
    {
        if (statusOverlayText == null || statusOverlayGroup == null || statusOverlayRect == null)
            yield break;

        statusOverlayText.text = message;
        statusOverlayText.color = color;
        UpdateStatusOverlayIcon(color);
        statusOverlayGroup.gameObject.SetActive(true);

        float elapsed = 0f;
        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = baseScale * statusMessagePopScale;
        statusOverlayGroup.alpha = 0f;
        statusOverlayRect.localScale = baseScale * 0.92f;

        while (elapsed < statusMessageAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / statusMessageAnimDuration);
            statusOverlayGroup.alpha = t;
            statusOverlayRect.localScale = Vector3.Lerp(baseScale * 0.92f, peakScale, t);
            yield return null;
        }

        statusOverlayGroup.alpha = 1f;
        statusOverlayRect.localScale = peakScale;

        yield return new WaitForSecondsRealtime(visibleDuration);

        elapsed = 0f;
        while (elapsed < statusMessageAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / statusMessageAnimDuration);
            statusOverlayGroup.alpha = 1f - t;
            statusOverlayRect.localScale = Vector3.Lerp(peakScale, baseScale, t);
            yield return null;
        }

        HideStatusOverlay();
        statusOverlayRoutine = null;
    }

    private void HideStatusOverlay()
    {
        if (statusOverlayGroup == null || statusOverlayRect == null)
            return;

        statusOverlayGroup.alpha = 0f;
        statusOverlayRect.localScale = Vector3.one;
        statusOverlayGroup.gameObject.SetActive(false);

        if (statusOverlayIconImage != null)
            statusOverlayIconImage.gameObject.SetActive(false);
    }

    private void EnsureStatusOverlay()
    {
        if (rootCanvas == null)
            return;

        Transform existing = rootCanvas.transform.Find("BasicRhythmStatusOverlay");
        if (existing == null)
        {
            GameObject overlayObject = new GameObject("BasicRhythmStatusOverlay", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.SetParent(rootCanvas.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(1100f, 280f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(0f, 0f);
            textRect.offsetMax = new Vector2(0f, -20f);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rect, false);
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, -24f);
            iconRect.sizeDelta = new Vector2(132f, 132f);
            statusOverlayIconImage = iconObject.GetComponent<Image>();
            statusOverlayIconImage.preserveAspect = true;
            statusOverlayIconImage.raycastTarget = false;

            statusOverlayText = textObject.GetComponent<TextMeshProUGUI>();
            statusOverlayText.alignment = TextAlignmentOptions.Center;
            statusOverlayText.enableAutoSizing = true;
            statusOverlayText.fontSizeMin = 34f;
            statusOverlayText.fontSizeMax = 74f;
            statusOverlayText.fontStyle = FontStyles.Bold;
            statusOverlayText.textWrappingMode = TextWrappingModes.Normal;
            statusOverlayText.raycastTarget = false;
            statusOverlayText.margin = new Vector4(32f, 120f, 32f, 0f);

            if (feedbackText != null)
                statusOverlayText.font = feedbackText.font;

            statusOverlayGroup = overlayObject.GetComponent<CanvasGroup>();
            statusOverlayRect = rect;
        }
        else
        {
            statusOverlayGroup = existing.GetComponent<CanvasGroup>();
            statusOverlayRect = existing as RectTransform;
            statusOverlayText = existing.GetComponentInChildren<TMP_Text>(true);
            statusOverlayIconImage = existing.GetComponentInChildren<Image>(true);
        }

        HideStatusOverlay();
    }

    private void UpdateStatusOverlayIcon(Color color)
    {
        if (statusOverlayIconImage == null)
            return;

        Sprite icon = GetRegularStatusSprite();
        if (color == successFeedbackColor)
            icon = successStatusSprite;
        else if (color == failFeedbackColor)
            icon = failStatusSprite;

        if (icon == null)
        {
            statusOverlayIconImage.gameObject.SetActive(false);
            return;
        }

        statusOverlayIconImage.sprite = icon;
        statusOverlayIconImage.color = Color.white;
        statusOverlayIconImage.gameObject.SetActive(true);
    }

    private Sprite GetRegularStatusSprite()
    {
        FigureDefinition note = GetCurrentNote();
        if (note != null && note.type == FigureType.Silencio && silenceStatusSprite != null)
            return silenceStatusSprite;

        if (tutorialFillWarmupWaitingForTap || waitingForInput)
            return turnStatusSprite != null ? turnStatusSprite : regularStatusSprite;

        if (regularStatusSprite == null)
            return observeStatusSprite != null ? observeStatusSprite : waitStatusSprite;

        if (observeStatusSprite != null)
            return observeStatusSprite;

        if (waitStatusSprite != null)
            return waitStatusSprite;

        return regularStatusSprite;
    }

    private void RefreshFeedbackPanelStatus()
    {
        if (feedbackText == null)
            return;

        int requiredHits = GetRequiredHitsForPattern();
        if (tutorialActive)
            feedbackText.text = $"Objetivo: {requiredHits} aciertos";
        else
            feedbackText.text = $"Objetivo: {requiredHits} aciertos  |  Logrados: {roundSuccessfulHits}";

        if (feedbackPanelImage != null)
        {
            Color panelColor = Color.white;
            panelColor.a = feedbackPanelAlpha;
            feedbackPanelImage.color = panelColor;
        }

        if (feedbackAnimRoutine != null)
            StopCoroutine(feedbackAnimRoutine);

        feedbackAnimRoutine = StartCoroutine(AnimateFeedbackPanel());
    }

    private void AnimateScorePanel()
    {
        scoreHudRoutine = RestartHudRoutine(scoreHudRoutine, scorePanelRect, scorePanelBaseScale);
    }

    private void AnimateLevelPanel()
    {
        levelHudRoutine = RestartHudRoutine(levelHudRoutine, levelPanelRect, levelPanelBaseScale);
    }

    private void AnimateLivesPanel()
    {
        livesHudRoutine = RestartHudRoutine(livesHudRoutine, livesPanelRect, livesPanelBaseScale);
    }

    private Coroutine RestartHudRoutine(Coroutine currentRoutine, RectTransform rect, Vector3 baseScale)
    {
        if (rect == null)
            return currentRoutine;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        return StartCoroutine(AnimateHudPanelRoutine(rect, baseScale));
    }

    private IEnumerator AnimateHudPanelRoutine(RectTransform rect, Vector3 baseScale)
    {
        float elapsed = 0f;
        Vector3 peakScale = baseScale * hudPopScale;
        while (elapsed < hudAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hudAnimDuration);
            rect.localScale = Vector3.Lerp(baseScale, peakScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < hudAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hudAnimDuration);
            rect.localScale = Vector3.Lerp(peakScale, baseScale, t);
            yield return null;
        }

        rect.localScale = baseScale;
    }

    private void SetDrumInteractable(bool value)
    {
        if (drumButton == null)
            return;

        drumButton.interactable = value;
        if (value)
            StartDrumReadyPulse();
        else
            StopDrumReadyPulse();
    }

    private void StartDrumReadyPulse()
    {
        if (drumButton == null || !drumButton.interactable || drumPulseRoutine != null || drumReadyRoutine != null)
            return;

        drumReadyRoutine = StartCoroutine(DrumReadyPulseRoutine());
    }

    private void StopDrumReadyPulse()
    {
        if (drumReadyRoutine != null)
        {
            StopCoroutine(drumReadyRoutine);
            drumReadyRoutine = null;
        }

        if (drumButton != null)
            drumButton.transform.localScale = Vector3.one;
    }

    private IEnumerator DrumReadyPulseRoutine()
    {
        while (drumButton != null && drumButton.interactable)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * drumReadyPulseSpeed) * (drumReadyPulseScale - 1f);
            drumButton.transform.localScale = Vector3.one * pulse;
            yield return null;
        }

        if (drumButton != null)
            drumButton.transform.localScale = Vector3.one;

        drumReadyRoutine = null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceSource == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private IEnumerator PlayPreRoundVoiceIfNeeded()
    {
        if (voiceSource == null)
            yield break;

        if (tutorialActive)
        {
            AudioClip tutorialClip = GetTutorialVoiceClip();
            if (tutorialClip != null)
                yield return StartCoroutine(PlayBlockingVoice(tutorialClip));

            yield break;
        }

        if (observeVoiceClip != null)
            yield return StartCoroutine(PlayBlockingVoice(observeVoiceClip));
    }

    private IEnumerator PlayBlockingVoice(AudioClip clip)
    {
        if (clip == null || voiceSource == null)
            yield break;

        PlayVoice(clip);
        while (voiceSource.isPlaying)
            yield return null;
    }

    private AudioClip GetTutorialVoiceClip()
    {
        FigureDefinition firstNote = GetPatternNoteAt(0);
        if (firstNote == null)
            return null;

        switch (firstNote.type)
        {
            case FigureType.Redonda:
                return redondaVoiceClip;
            case FigureType.Blanca:
                return blancaVoiceClip;
            case FigureType.Negra:
                return negraVoiceClip;
            case FigureType.Silencio:
                return null;
            default:
                return corcheaVoiceClip;
        }
    }

    private void BuildFigureDefinitions()
    {
        figures.Clear();
        AddFigure(FigureType.Redonda, "Redonda", 4f, redondaSprite);
        AddFigure(FigureType.Blanca, "Blanca", 2f, blancaSprite, blancaASprite, blancaBSprite);
        AddFigure(FigureType.Negra, "Negra", 1f, negraSprite);
        AddFigure(FigureType.Corchea, "Corchea", 0.5f, corcheaSprite, corcheaMaSprite, corcheaRiSprite, corcheaPoSprite, corcheaSaSprite);
        AddFigure(FigureType.Silencio, "Silencio", silencioBeats, silencioSprite);
    }

    private void AddFigure(FigureType type, string displayName, float noteBeats, Sprite sprite, params Sprite[] orderedSprites)
    {
        if (sprite == null)
            return;

        figures.Add(new FigureDefinition
        {
            type = type,
            displayName = displayName,
            noteBeats = noteBeats,
            sprite = sprite,
            orderedSprites = orderedSprites,
        });
    }

    private void ResolveReferences()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            return;

        rootCanvas = canvas;

        feedbackText = FindText(canvas, "FeedbackText");
        if (feedbackText != null)
        {
            feedbackPanelRect = feedbackText.rectTransform.parent as RectTransform;
            if (feedbackPanelRect != null)
            {
                feedbackPanelBaseScale = feedbackPanelRect.localScale;
                feedbackPanelImage = feedbackPanelRect.GetComponent<Image>();
            }
        }
        figureIconTemplate = FindImage(canvas, "FigureIcon");
        drumButton = FindButton(canvas, "DrumButton");
        menuButton = FindButton(canvas, "MenuButton");
        Transform pausePanelTransform = FindChild(canvas.transform, "PausePanel");
        if (pausePanelTransform != null)
        {
            pausePanel = pausePanelTransform.gameObject;
            resumeButton = FindButton(pausePanelTransform, "ResumenButton") ?? FindButton(pausePanelTransform, "ResumeButton");
            restartButton = FindButton(pausePanelTransform, "RestartButton");
            closeButton = FindButton(pausePanelTransform, "CloseButton");
            pauseMenuButton = FindButton(pausePanelTransform, "MenuButton");
        }

        if (drumButton != null)
        {
            drumButtonImage = drumButton.GetComponent<Image>();
            if (drumButtonImage != null)
                drumButtonBaseColor = drumButtonImage.color;
        }

        Transform rhythmPanelTransform = FindChild(canvas.transform, "RhythmPanel");
        if (rhythmPanelTransform != null)
            rhythmPanel = rhythmPanelTransform as RectTransform;

        Transform scorePanel = FindChild(canvas.transform, "ScorePanel");
        Transform levelPanel = FindChild(canvas.transform, "LevelPanel");
        Transform livesPanel = FindChild(canvas.transform, "LivesPanel");

        if (scorePanel != null)
        {
            scoreText = scorePanel.GetComponentInChildren<TMP_Text>(true);
            scorePanelRect = scorePanel as RectTransform;
            if (scorePanelRect != null)
                scorePanelBaseScale = scorePanelRect.localScale;
        }

        if (levelPanel != null)
        {
            levelText = levelPanel.GetComponentInChildren<TMP_Text>(true);
            levelPanelRect = levelPanel as RectTransform;
            if (levelPanelRect != null)
                levelPanelBaseScale = levelPanelRect.localScale;
        }

        if (livesPanel != null)
        {
            livesPanelRect = livesPanel as RectTransform;
            if (livesPanelRect != null)
                livesPanelBaseScale = livesPanelRect.localScale;

            lifeImages = new[]
            {
                FindImage(livesPanel, "Life1"),
                FindImage(livesPanel, "Life2"),
                FindImage(livesPanel, "Life3"),
            };
        }

        HideLegacyFigurePlaceholders();

        if (pausePanel != null)
        {
            pausePanelView = pausePanel.GetComponent<PausePanelView>();
            if (pausePanelView == null)
                pausePanelView = pausePanel.AddComponent<PausePanelView>();

            pausePanelView.InitializeHidden();
        }
    }

    private void EnsureAudioSource()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxBaseVolume = sfxSource.volume;

        AudioSource[] sources = GetComponents<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != sfxSource)
            {
                voiceSource = sources[i];
                break;
            }
        }

        if (voiceSource == null)
            voiceSource = gameObject.AddComponent<AudioSource>();

        voiceSource.playOnAwake = false;
        voiceBaseVolume = voiceSource.volume;
        ApplyGameSettings();
    }

    private void ApplyGameSettings()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxBaseVolume * GameSettings.EffectsVolume;

        if (voiceSource != null)
            voiceSource.volume = voiceBaseVolume * GameSettings.EffectsVolume;
    }

    private void EnsureDrumInput()
    {
        if (drumButton == null)
            return;

        EventTrigger trigger = drumButton.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = drumButton.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();
        AddTrigger(trigger, EventTriggerType.PointerDown, _ => OnDrumPointerDown());
        AddTrigger(trigger, EventTriggerType.PointerUp, _ => OnDrumPointerUp());
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => OnDrumPointerUp());

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartScene);
        }

        if (pauseMenuButton != null)
        {
            pauseMenuButton.onClick.RemoveAllListeners();
            pauseMenuButton.onClick.AddListener(LoadMainScene);
        }
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

        if (drumButton != null)
            SetDrumInteractable(false);
    }

    private void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        if (pausePanelView != null)
            pausePanelView.Hide();
        else if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (drumButton != null)
            SetDrumInteractable(waitingForInput && lives > 0);
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        attemptExitReason = "restart";
        ReportAttempt(false);
        SceneTransitionController.ReloadCurrentScene();
    }

    private void LoadMainScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        attemptExitReason = lives > 0 ? "return_to_menu" : "game_over_return";
        ReportAttempt(lives > 0);
        SceneTransitionController.LoadScene("MainScene");
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (figureIconTemplate == null || rhythmPanel == null)
        {
            Debug.LogError("BasicRhythmGameManager: Falta FigureIcon o RhythmPanel.");
            valid = false;
        }

        if (feedbackText == null)
        {
            Debug.LogError("BasicRhythmGameManager: Falta FeedbackText.");
            valid = false;
        }

        if (drumButton == null)
        {
            Debug.LogError("BasicRhythmGameManager: Falta DrumButton.");
            valid = false;
        }

        if (scoreText == null || levelText == null)
        {
            Debug.LogError("BasicRhythmGameManager: No se pudo resolver ScoreText o LevelText.");
            valid = false;
        }

        if (lifeImages == null || lifeImages.Length < 3 || lifeImages[0] == null || lifeImages[1] == null || lifeImages[2] == null)
        {
            Debug.LogError("BasicRhythmGameManager: No se pudieron resolver las 3 vidas.");
            valid = false;
        }

        if (redondaSprite == null || blancaSprite == null || negraSprite == null || corcheaSprite == null)
        {
            Debug.LogError("BasicRhythmGameManager: Faltan sprites de figuras.");
            valid = false;
        }

        if (blancaASprite == null || blancaBSprite == null)
        {
            Debug.LogError("BasicRhythmGameManager: Faltan BlancaA o BlancaB.");
            valid = false;
        }

        if (corcheaMaSprite == null || corcheaRiSprite == null || corcheaPoSprite == null || corcheaSaSprite == null)
        {
            Debug.LogError("BasicRhythmGameManager: Faltan variantes de corchea ma-ri-po-sa.");
            valid = false;
        }

        if (silencioSprite == null)
        {
            Debug.LogError("BasicRhythmGameManager: Falta SilencioSprite.");
            valid = false;
        }

        return valid;
    }

    private static TMP_Text FindText(Component root, string objectName)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
                return texts[i];
        }

        return null;
    }

    private static Image FindImage(Component root, string objectName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == objectName)
                return images[i];
        }

        return null;
    }

    private static Button FindButton(Component root, string objectName)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName)
                return buttons[i];
        }

        return null;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform nested = FindChild(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void HideLegacyFigurePlaceholders()
    {
        if (rhythmPanel == null)
            return;

        string[] namesToHide =
        {
            "FigureIcon1",
            "FigureIcon2",
            "FigureIcon3",
            "FigureIcon4",
            "FigureNameText",
        };

        for (int i = 0; i < namesToHide.Length; i++)
        {
            Transform target = FindChild(rhythmPanel, namesToHide[i]);
            if (target != null)
                target.gameObject.SetActive(false);
        }
    }

    private void ReportAttempt(bool completed)
    {
        if (attemptReportSubmitted || GameReportManager.Instance == null || SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
            return;

        attemptReportSubmitted = true;
        AttemptReportData report = new AttemptReportData
        {
            miniGame = "basic_rhythm",
            level = tutorialActive ? 0 : Mathf.Max(1, level),
            difficulty = tutorialActive ? "tutorial" : (patternMeasures > 2 ? "media" : "facil"),
            bpm = Mathf.RoundToInt(bpm * GameSettings.GameplaySpeed),
            errors = attemptErrors,
            correctAnswers = attemptCorrectAnswers,
            levelRepetitions = attemptLevelRepetitions,
            completed = completed,
            timeSeconds = Mathf.Max(1, Mathf.RoundToInt(Time.time - attemptStartedAt)),
            scoreFinal = score,
            wasTutorial = tutorialActive,
            exitReason = attemptExitReason,
        };

        GameReportManager.Instance.ReportAttempt(report, null, error => Debug.LogWarning("BasicRhythm report error: " + error));
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
