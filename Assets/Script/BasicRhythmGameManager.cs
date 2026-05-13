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
    }

    [System.Serializable]
    private class FigureDefinition
    {
        public FigureType type;
        public string displayName;
        public float noteBeats;
        public Sprite sprite;
    }

    [Header("Figuras")]
    [SerializeField] private Sprite redondaSprite;
    [SerializeField] private Sprite blancaSprite;
    [SerializeField] private Sprite negraSprite;
    [SerializeField] private Sprite corcheaSprite;

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
    [SerializeField] private float roundIntroDelay = 0.6f;
    [SerializeField] private float roundOutroDelay = 1f;
    [SerializeField] private float maxStartDelayBeats = 0.4f;
    [SerializeField] private float perfectToleranceBeats = 0.12f;
    [SerializeField] private float goodToleranceBeats = 0.25f;
    [SerializeField] private int maxLives = 3;

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

    private readonly List<FigureDefinition> figures = new List<FigureDefinition>();
    private readonly List<Image> patternIcons = new List<Image>();
    private readonly List<Image> patternFillImages = new List<Image>();
    private readonly List<FigureDefinition> currentPattern = new List<FigureDefinition>();

    private TMP_Text scoreText;
    private TMP_Text levelText;
    private TMP_Text feedbackText;
    private Image figureIconTemplate;
    private RectTransform figureIconContainer;
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

    private Coroutine roundRoutine;
    private Coroutine drumPulseRoutine;
    private bool waitingForInput;
    private bool isPressing;
    private float pressStartTime;
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

    private void Start()
    {
        ResolveReferences();
        if (!ValidateReferences())
            return;

        BuildFigureDefinitions();
        EnsureAudioSource();
        EnsureDrumInput();

        score = 0;
        level = 0;
        lives = maxLives;

        UpdateHud();
        SetFeedback(ComposeFeedback("Escucha"), normalFeedbackColor);

        roundRoutine = StartCoroutine(RunRoundLoop());
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (isPaused)
            return;

        FigureDefinition currentNote = GetCurrentNote();
        if (!waitingForInput || currentNote == null)
            return;

        float beatDuration = GetBeatDuration();
        float expectedStart = inputPhaseStartTime + GetPatternBeatsBefore(currentNoteIndex) * beatDuration;
        float releaseLimit = currentNote.noteBeats * beatDuration + goodToleranceBeats * beatDuration;

        if (UsesLongNoteTapCue(currentNote))
        {
            UpdateAllLongNoteFills(Time.time);

            float expectedEnd = expectedStart + currentNote.noteBeats * beatDuration;
            UpdateTimingCue(Mathf.Abs(Time.time - expectedEnd) <= perfectToleranceBeats * beatDuration);

            if (Time.time > expectedEnd + goodToleranceBeats * beatDuration)
            {
                ResolveFailure("Muy tarde");
                return;
            }

            return;
        }

        float latestAllowedStart = expectedStart + maxStartDelayBeats * beatDuration;

        if (!isPressing && Time.time > latestAllowedStart)
        {
            ResolveFailure("Muy tarde");
            return;
        }

        UpdateTimingCue(Mathf.Abs(Time.time - expectedStart) <= perfectToleranceBeats * beatDuration);

        if (isPressing && Time.time - pressStartTime > releaseLimit)
        {
            CompleteCurrentNote(Time.time - pressStartTime);
        }
    }

    public void OnDrumPointerDown()
    {
        if (isPaused || !waitingForInput || isPressing)
            return;

        FigureDefinition currentNote = GetCurrentNote();
        if (currentNote == null)
            return;

        if (UsesLongNoteTapCue(currentNote))
        {
            EvaluateLongNoteTap(Time.time);
            if (drumButton != null)
                drumButton.transform.localScale = Vector3.one * 0.95f;
            return;
        }

        isPressing = true;
        pressStartTime = Time.time;
        HighlightPatternIcon(currentNoteIndex, noteLitColor);

        if (drumButton != null)
            drumButton.transform.localScale = Vector3.one * 0.95f;
    }

    public void OnDrumPointerUp()
    {
        if (isPaused)
            return;

        FigureDefinition currentNote = GetCurrentNote();

        if (drumButton != null)
            drumButton.transform.localScale = Vector3.one;

        if (UsesLongNoteTapCue(currentNote))
            return;

        if (!waitingForInput || !isPressing)
            return;

        CompleteCurrentNote(Time.time - pressStartTime);
    }

    private IEnumerator RunRoundLoop()
    {
        yield return new WaitForSeconds(roundIntroDelay);

        while (lives > 0)
        {
            PrepareNextPattern();
            roundFailures = 0;
            roundCompletedSuccessfully = false;

            while (!roundCompletedSuccessfully && lives > 0)
            {
                attemptFinished = false;

                yield return StartCoroutine(PlayPreRoundVoiceIfNeeded());

                yield return StartCoroutine(ShowDemoPattern());

                SetFeedback(ComposeFeedback(GetInputInstruction()), normalFeedbackColor);
                yield return StartCoroutine(PlayBlockingVoice(yourTurnVoiceClip));

                waitingForInput = true;
                isPressing = false;
                currentNoteIndex = 0;
                roundUsedOnlyPerfectTiming = true;
                roundSuccessfulHits = 0;
                inputPhaseStartTime = Time.time;

                drumButton.interactable = true;
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

        BuildPatternVisuals();
        drumButton.interactable = false;
        drumButton.transform.localScale = Vector3.one;
    }

    private IEnumerator ShowDemoPattern()
    {
        SetFeedback(ComposeFeedback(GetDemoInstruction()), normalFeedbackColor);
        ResetPatternVisuals(true);

        for (int i = 0; i < noteCountInPattern; i++)
        {
            FigureDefinition note = GetPatternNoteAt(i);
            float noteDuration = note.noteBeats * GetBeatDuration();
            HighlightPatternIcon(i, noteLitColor);
            PlaySfx(metronomeClip);
            PulseDrum(timingCueColor);

            if (UsesLongNoteTapCue(note))
                yield return StartCoroutine(AnimateFillForDemo(i, noteDuration));
            else
                yield return new WaitForSeconds(noteDuration);

            HighlightPatternIcon(i, noteDimColor);
            SetFillAmount(i, 0f);
        }
    }

    private IEnumerator AnimateFillForDemo(int noteIndex, float duration)
    {
        SetFillAmount(noteIndex, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFillAmount(noteIndex, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetFillAmount(noteIndex, 1f);
    }

    private void UpdateAllLongNoteFills(float currentTime)
    {
        if (currentPattern.Count == 0)
            return;

        float beatDuration = GetBeatDuration();

        for (int i = 0; i < noteCountInPattern; i++)
        {
            FigureDefinition note = GetPatternNoteAt(i);
            float noteDuration = note.noteBeats * beatDuration;
            float noteStart = inputPhaseStartTime + GetPatternBeatsBefore(i) * beatDuration;
            float progress = Mathf.Clamp01((currentTime - noteStart) / Mathf.Max(0.01f, noteDuration));
            SetFillAmount(i, progress);

            if (i > currentNoteIndex)
            {
                HighlightPatternIcon(i, waitingForInput ? noteLitColor : noteDimColor);
                SetFillColor(i, longNoteFillColor);
            }
        }

        if (currentNoteIndex >= 0 && currentNoteIndex < patternIcons.Count)
        {
            HighlightPatternIcon(currentNoteIndex, waitingForInput ? noteLitColor : noteDimColor);
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
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
            PlaySfx(metronomeClip);
            PulseDrum(successFeedbackColor);
        }
        else if (timingErrorBeats <= goodToleranceBeats)
        {
            roundSuccessfulHits++;
            roundUsedOnlyPerfectTiming = false;
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, true);
            PlaySfx(metronomeClip);
            PulseDrum(successFeedbackColor);
        }
        else
        {
            ResolveFailure(tapTime < expectedEnd ? "Muy pronto" : "Muy tarde");
            return;
        }

        AdvanceToNextNote();
    }

    private void CompleteCurrentNote(float heldDuration)
    {
        FigureDefinition currentNote = GetCurrentNote();
        waitingForInput = currentNote != null;
        isPressing = false;

        if (drumButton != null)
        {
            drumButton.transform.localScale = Vector3.one;
            drumButton.interactable = false;
        }

        if (currentNote == null)
            return;

        float beatDuration = GetBeatDuration();
        float expectedStart = inputPhaseStartTime + GetPatternBeatsBefore(currentNoteIndex) * beatDuration;
        float startDelayBeats = Mathf.Max(0f, pressStartTime - expectedStart) / beatDuration;
        float durationErrorBeats = Mathf.Abs(heldDuration - currentNote.noteBeats * beatDuration) / beatDuration;

        if (startDelayBeats > maxStartDelayBeats)
        {
            ResolveFailure("Muy tarde");
            return;
        }

        if (durationErrorBeats <= perfectToleranceBeats)
        {
            roundSuccessfulHits++;
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, false);
            PlaySfx(metronomeClip);
            PulseDrum(successFeedbackColor);
        }
        else if (durationErrorBeats <= goodToleranceBeats)
        {
            roundSuccessfulHits++;
            roundUsedOnlyPerfectTiming = false;
            SetPatternResultColor(currentNoteIndex, successFeedbackColor, false);
            PlaySfx(metronomeClip);
            PulseDrum(successFeedbackColor);
        }
        else
        {
            ResolveFailure(heldDuration < currentNote.noteBeats * beatDuration ? "Muy corto" : "Muy largo");
            return;
        }

        AdvanceToNextNote();
    }

    private void CompletePatternSuccess()
    {
        waitingForInput = false;
        isPressing = false;
        roundCompletedSuccessfully = true;

        bool isPerfectAttempt = roundUsedOnlyPerfectTiming && roundFailures == 0 && roundSuccessfulHits == noteCountInPattern;

        score += isPerfectAttempt ? 10 : 5;

        if (tutorialActive)
        {
            tutorialStepIndex++;
            if (tutorialStepIndex >= 4)
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
        isPressing = false;

        if (drumButton != null)
        {
            drumButton.transform.localScale = Vector3.one;
            drumButton.interactable = false;
        }

        if (currentNoteIndex >= 0 && currentNoteIndex < patternIcons.Count)
            SetPatternResultColor(currentNoteIndex, failFeedbackColor, UsesLongNoteTapCue(GetCurrentNote()));

        if (currentNoteIndex >= 0 && !UsesLongNoteTapCue(GetCurrentNote()))
            SetFillAmount(currentNoteIndex, 0f);

        roundFailures++;
        PlaySfx(failClip);
        PulseDrum(failFeedbackColor);
        SetFeedback(ComposeFeedback(reason), failFeedbackColor);

        AdvanceToNextNote();
    }

    private void AdvanceToNextNote()
    {
        currentNoteIndex++;

        if (currentNoteIndex >= noteCountInPattern)
        {
            FinishCurrentAttempt();
            return;
        }

        if (drumButton != null)
            drumButton.interactable = true;
    }

    private void FinishCurrentAttempt()
    {
        waitingForInput = false;
        attemptFinished = true;

        if (drumButton != null)
            drumButton.interactable = false;

        int requiredHits = GetRequiredHitsForPattern();
        if (roundSuccessfulHits >= requiredHits)
        {
            CompletePatternSuccess();
            return;
        }

        lives--;
        UpdateHud();

        if (lives <= 0)
        {
            SetFeedback(ComposeFeedback("Sin vidas"), failFeedbackColor);
            if (roundRoutine != null)
                StopCoroutine(roundRoutine);
            return;
        }

        SetFeedback(ComposeFeedback("Pierdes 1 vida"), failFeedbackColor);
        PlayVoice(tryAgainVoiceClip);
    }

    private int GetRequiredHitsForPattern()
    {
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

        if (tutorialActive)
        {
            switch (tutorialStepIndex)
            {
                case 0:
                    AddPatternFigure(FigureType.Redonda, 1);
                    break;
                case 1:
                    AddPatternFigure(FigureType.Blanca, 2);
                    break;
                case 2:
                    AddPatternFigure(FigureType.Negra, 4);
                    break;
                default:
                    AddPatternFigure(FigureType.Corchea, 8);
                    break;
            }
            return;
        }

        float remainingBeats = totalPatternBeats;
        while (remainingBeats > 0.001f)
        {
            List<FigureDefinition> candidates = new List<FigureDefinition>();
            for (int i = 0; i < figures.Count; i++)
            {
                if (figures[i].noteBeats <= remainingBeats + 0.001f)
                    candidates.Add(figures[i]);
            }

            if (candidates.Count == 0)
                break;

            FigureDefinition chosen = candidates[Random.Range(0, candidates.Count)];
            currentPattern.Add(chosen);
            remainingBeats -= chosen.noteBeats;
        }
    }

    private void AddPatternFigure(FigureType type, int count)
    {
        FigureDefinition figure = GetFigure(type);
        if (figure == null)
            return;

        for (int i = 0; i < count; i++)
            currentPattern.Add(figure);
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

        RectTransform templateRect = figureIconTemplate.rectTransform;
        int columns = noteCountInPattern <= 4 ? noteCountInPattern : 4;
        int rows = Mathf.CeilToInt(noteCountInPattern / 4f);

        float availableWidth = Mathf.Max(420f, rhythmPanel.rect.width - 180f);
        float availableHeight = Mathf.Max(220f, rhythmPanel.rect.height - 140f);
        float iconWidth = (availableWidth - iconSpacing * (columns - 1)) / columns;
        float iconHeight = (availableHeight - iconSpacing * (rows - 1)) / rows;
        float templateWidth = templateRect.rect.width > 0f ? templateRect.rect.width : 140f;
        float templateHeight = templateRect.rect.height > 0f ? templateRect.rect.height : 140f;
        float iconSize = Mathf.Clamp(Mathf.Min(iconWidth, iconHeight, templateWidth * 1.8f, templateHeight * 1.8f), 110f, 190f);
        float rowHeight = iconSize + iconSpacing;

        for (int i = 0; i < noteCountInPattern; i++)
        {
            Image icon = Instantiate(figureIconTemplate, rhythmPanel);
            icon.name = "PatternIcon_" + i;
            icon.sprite = GetPatternNoteAt(i).sprite;
            icon.preserveAspect = true;
            icon.gameObject.SetActive(true);

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            int row = i / 4;
            int itemsInRow = row < rows - 1 ? 4 : noteCountInPattern - row * 4;
            if (itemsInRow <= 0)
                itemsInRow = 4;

            float rowWidth = itemsInRow * iconSize + (itemsInRow - 1) * iconSpacing;
            float rowStartX = -rowWidth * 0.5f + iconSize * 0.5f;
            int column = i % 4;
            float x = rowStartX + column * (iconSize + iconSpacing);
            float y = (rows - 1) * rowHeight * 0.5f - row * rowHeight;

            iconRect.anchoredPosition = new Vector2(x, y + 20f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);

            patternIcons.Add(icon);
            patternFillImages.Add(CreateFillImage(icon));
        }

        ResetPatternVisuals(true);
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
    }

    private void ResetPatternVisuals(bool dimIcons)
    {
        Color iconColor = dimIcons ? noteDimColor : noteLitColor;

        for (int i = 0; i < patternIcons.Count; i++)
        {
            HighlightPatternIcon(i, iconColor);
            SetFillColor(i, longNoteFillColor);
            SetFillAmount(i, 0f);
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

    private bool UsesLongNoteTapCue(FigureDefinition figure)
    {
        return figure != null;
    }

    private float GetBeatDuration()
    {
        return 60f / Mathf.Max(1f, bpm);
    }

    private string GetDemoInstruction(FigureDefinition figure)
    {
        switch (figure.type)
        {
            case FigureType.Redonda:
                return "Redonda: escucha";
            case FigureType.Blanca:
                return "Blanca: escucha";
            case FigureType.Negra:
                return "Negra: escucha";
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
                return "Toca al llenarse";
            case FigureType.Blanca:
                return "Toca al llenarse";
            case FigureType.Negra:
                return "Toca al llenarse";
            default:
                return "Toca al llenarse";
        }
    }

    private string GetInputInstruction()
    {
        if (tutorialActive)
            return "Repite el tempo";

        return "Sigue la secuencia";
    }

    private void UpdateHud()
    {
        if (scoreText != null)
            scoreText.text = "Puntos: " + score;

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
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        feedbackText.color = color;
    }

    private string ComposeFeedback(string status)
    {
        int shownStep = noteCountInPattern <= 0 ? 0 : Mathf.Min(currentNoteIndex + 1, noteCountInPattern);
        return status + "  " + shownStep + "/" + noteCountInPattern + "  Req " + GetRequiredHitsForPattern();
    }

    private void UpdateTimingCue(bool active)
    {
        if (currentNoteIndex < 0 || currentNoteIndex >= patternIcons.Count)
            return;

        if (active)
            SetPatternPreviewColor(currentNoteIndex, timingCueColor);
        else
            SetPatternPreviewColor(currentNoteIndex, waitingForInput ? noteLitColor : noteDimColor);
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
        if (tutorialStepIndex == 0 && tutorialIntroClip != null)
            return tutorialIntroClip;

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
            default:
                return corcheaVoiceClip;
        }
    }

    private void BuildFigureDefinitions()
    {
        figures.Clear();
        AddFigure(FigureType.Redonda, "Redonda", 4f, redondaSprite);
        AddFigure(FigureType.Blanca, "Blanca", 2f, blancaSprite);
        AddFigure(FigureType.Negra, "Negra", 1f, negraSprite);
        AddFigure(FigureType.Corchea, "Corchea", 0.5f, corcheaSprite);
    }

    private void AddFigure(FigureType type, string displayName, float noteBeats, Sprite sprite)
    {
        if (sprite == null)
            return;

        figures.Add(new FigureDefinition
        {
            type = type,
            displayName = displayName,
            noteBeats = noteBeats,
            sprite = sprite,
        });
    }

    private void ResolveReferences()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            return;

        feedbackText = FindText(canvas, "FeedbackText");
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

        if (figureIconTemplate != null)
            figureIconContainer = figureIconTemplate.rectTransform.parent as RectTransform;

        Transform scorePanel = FindChild(canvas.transform, "ScorePanel");
        Transform levelPanel = FindChild(canvas.transform, "LevelPanel");
        Transform livesPanel = FindChild(canvas.transform, "LivesPanel");

        if (scorePanel != null)
            scoreText = scorePanel.GetComponentInChildren<TMP_Text>(true);

        if (levelPanel != null)
            levelText = levelPanel.GetComponentInChildren<TMP_Text>(true);

        if (livesPanel != null)
        {
            lifeImages = new[]
            {
                FindImage(livesPanel, "Life1"),
                FindImage(livesPanel, "Life2"),
                FindImage(livesPanel, "Life3"),
            };
        }

        HideLegacyFigurePlaceholders();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void EnsureAudioSource()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;

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
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (drumButton != null)
            drumButton.interactable = false;
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

        if (drumButton != null)
            drumButton.interactable = waitingForInput && lives > 0;
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadMainScene()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainScene");
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

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
