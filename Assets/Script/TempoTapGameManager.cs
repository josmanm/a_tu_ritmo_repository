using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class TempoTapGameManager : MonoBehaviour
{
    public BeatController beatController;

    [Header("UI")]
    public TMP_Text feedbackText;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text streakText;
    public Image stabilityFill;

    [Header("UI Panels")]
    public GameObject startPanel;

    TMP_Text startButtonText;

    [Header("Timing Windows (ms)")]
    public float perfectMs = 60f;
    public float goodMs = 120f;

    [Header("Inicio")]
    [Range(0, 4)] public int warmupBeats = 2;

    [Header("Score")]
    public int perfectScore = 100;
    public int goodScore = 60;
    public int timingOffsetScore = 20;

    [Header("Stability")]
    [Range(0f, 1f)] public float stability = 1f;
    public float gainOnHit = 0.06f;
    public float lossOnMiss = 0.12f;

    [Header("Tap SFX (opcional)")]
    public AudioSource sfxSource;
    public AudioClip tapCorrect;
    public AudioClip tapWrong;

    [Header("Runner")]
    public RunnerController2D runner;
    public bool jumpOnGood = true; // si "Bien" también salta
    public float perfectJumpMultiplier = 1f;
    public float goodJumpMultiplier = 1f;
    public float earlyAssistJumpMultiplier = 1f;
    public float lateAssistJumpMultiplier = 1.08f;

    [Header("Adaptive Jump")]
    public float perfectTargetContactTime = 0.34f;
    public float goodEarlyTargetContactTime = 0.4f;
    public float goodLateTargetContactTime = 0.3f;
    public float assistEarlyTargetContactTime = 0.46f;
    public float assistLateTargetContactTime = 0.24f;

    [Header("Asistencia (siempre activa)")]
    public float assistMs = 180f;   // ventana extra para que igual salte
    public float assistedJumpMultiplier = 0.9f; // salto un poco más bajo si quieres

    [Header("Control")]
    public bool ignoreExtraTapsOnSameBeat = true;

    public BeatSyncedObstacleSpawner obstacleSpawner;

    bool running;
    float baseBpm;
    float sessionStartedAt;
    double lastJudgedBeatTime = double.NaN;
    double lastMissedBeatTime = double.NaN;
    double firstJudgedBeatDspTime;
    bool warmupPending;
    bool turnVoicePlayed;
    float originalBeatDelay;
    bool pausedByApplication;
    float lastStartPressTime = -10f;
    Coroutine feedbackClearRoutine;
    int score;
    int streak;
    int totalTapsJudged;
    int perfectCount;
    int goodCount;
    int earlyCount;
    int lateCount;
    int missCount;
    int collisionCount;
    double sumOffsetMs;
    double lastTapDspTime = double.NaN;
    int intervalCount;
    double sumIntervalMs;
    double sumSquaredIntervalMs;
    TMP_Text stabilityLabel;

    public bool IsAcceptingTaps => running && !pausedByApplication && !warmupPending && beatController != null && beatController.IsRunning;
    public float CurrentPerfectMs => Mathf.Lerp(perfectMs, perfectMs * 0.72f, Mathf.Clamp01(streak / 20f));
    public float CurrentAssistMs => beatController == null ? assistMs : Mathf.Min(assistMs, (float)beatController.IntervalSec * 200f);

    public string GetPromptForBeat(double beatTime)
    {
        if (!IsAcceptingTaps || beatTime < firstJudgedBeatDspTime - 0.01)
            return "ESCUCHA";

        BeatSyncedObstacleSpawner.ObstacleAction action = obstacleSpawner != null
            ? obstacleSpawner.GetActionForBeat(beatTime) : BeatSyncedObstacleSpawner.ObstacleAction.Tap;
        return action == BeatSyncedObstacleSpawner.ObstacleAction.Jump ? "SALTA\nAL PULSO"
            : action == BeatSyncedObstacleSpawner.ObstacleAction.Duck ? "ABAJO\nAL PULSO" : "TOCA\nAL PULSO";
    }


    void Start()
    {
        running = false;
        ConfigureHud();
        ResetSessionStats();
        UpdateUI();

        if (beatController != null)
        {
            baseBpm = beatController.bpm;
            originalBeatDelay = beatController.startDelaySeconds;
            beatController.StopBeats();
            beatController.OnBeat += CheckMissedBeat;
        }

        if (runner != null)
            runner.SetGameStarted(false);

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawner();

        if (startPanel != null)
        {
            startPanel.SetActive(true);
            startButtonText = startPanel.GetComponentInChildren<TMP_Text>(true);
            if (startButtonText != null)
                startButtonText.text = "Iniciar";
        }

        SetFeedback("Toca Iniciar para escuchar el pulso", 0f);
        NarrationPlayer.Say("tempo_intro", interrupt: false);
    }

    private void OnDestroy()
    {
        if (beatController != null)
            beatController.OnBeat -= CheckMissedBeat;
    }

    private void ConfigureHud()
    {
        if (timerText != null)
        {
            RectTransform rect = timerText.rectTransform;
            rect.anchoredPosition = new Vector2(185f, -124f);
            rect.sizeDelta = new Vector2(350f, 86f);
            timerText.fontSize = 28f;
            timerText.fontStyle = FontStyles.Bold;
            timerText.raycastTarget = false;
        }

        if (stabilityFill == null || timerText == null)
            return;

        RectTransform bar = stabilityFill.transform.parent as RectTransform;
        if (bar == null)
            return;

        bar.sizeDelta = new Vector2(bar.sizeDelta.x, 54f);
        Transform found = bar.Find("EnergyLabel");
        if (found == null)
        {
            GameObject labelObject = new GameObject("EnergyLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(bar, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            found = labelRect;
        }
        stabilityLabel = found.GetComponent<TMP_Text>();
        stabilityLabel.font = timerText.font;
        stabilityLabel.fontSize = 28f;
        stabilityLabel.fontStyle = FontStyles.Bold;
        stabilityLabel.color = new Color(0.094f, 0.188f, 0.31f);
        stabilityLabel.alignment = TextAlignmentOptions.Center;
        stabilityLabel.raycastTarget = false;
    }

    private void CheckMissedBeat(double currentBeatTime)
    {
        if (!IsAcceptingTaps || beatController == null)
            return;

        double previousBeat = currentBeatTime - beatController.IntervalSec;
        if (previousBeat < firstJudgedBeatDspTime - 0.01 ||
            (!double.IsNaN(lastJudgedBeatTime) && Math.Abs(previousBeat - lastJudgedBeatTime) < 0.03) ||
            (!double.IsNaN(lastMissedBeatTime) && Math.Abs(previousBeat - lastMissedBeatTime) < 0.03))
            return;

        lastMissedBeatTime = previousBeat;
        missCount++;
        totalTapsJudged++;
        streak = 0;
        stability = Mathf.Clamp01(stability - lossOnMiss);
        obstacleSpawner?.SetDifficulty(0);
        SetFeedback("Escucha y toca el siguiente pulso", 0.8f);
        if (stability <= 0f)
            EndSession(false, "stability_depleted");
        else
            UpdateUI();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            if (!running)
                return;

            pausedByApplication = true;
            if (beatController != null) beatController.StopBeats();
            if (obstacleSpawner != null) obstacleSpawner.StopSpawner();
            if (runner != null) runner.SetGameStarted(false);
        }
        else if (pausedByApplication)
        {
            pausedByApplication = false;
            if (!running || beatController == null)
                return;

            beatController.StartBeats();
            firstJudgedBeatDspTime = beatController.NextBeatDspTime + beatController.IntervalSec * warmupBeats;
            warmupPending = true;
            turnVoicePlayed = false;
            lastJudgedBeatTime = double.NaN;
            lastMissedBeatTime = double.NaN;
            lastTapDspTime = double.NaN;
            if (obstacleSpawner != null) obstacleSpawner.StartSpawner();
            if (runner != null) runner.SetGameStarted(true);
            SetFeedback("Escucha el pulso", 0f);
        }
    }

    public void StartSession()
    {
        if (running || beatController == null)
            return;

        running = true;
        pausedByApplication = false;
        sessionStartedAt = Time.time;
        stability = 1f;
        lastJudgedBeatTime = double.NaN;
        lastMissedBeatTime = double.NaN;
        ResetSessionStats();
        UpdateUI();

        beatController.bpm = Mathf.Clamp(baseBpm * GameSettings.GameplaySpeed, 30f, 160f);
        float voiceSeconds = NarrationPlayer.Say(warmupBeats == 2 ? "tempo_escucha_dos_pulsos" : "tempo_intro");
        beatController.startDelaySeconds = Mathf.Max(originalBeatDelay, voiceSeconds + 0.2f);
        beatController.StartBeats();
        beatController.startDelaySeconds = originalBeatDelay;
        firstJudgedBeatDspTime = beatController.NextBeatDspTime + beatController.IntervalSec * warmupBeats;
        warmupPending = true;
        turnVoicePlayed = false;
        SetFeedback(warmupBeats > 0 ? "Escucha el pulso" : "Mantén el ritmo", warmupBeats > 0 ? 0f : 0.7f);
    }

    void Update()
    {
        if (!running || pausedByApplication) return;

        if (warmupPending)
        {
            AudioClip turnClip = NarrationPlayer.FindClip("tempo_turno");
            if (!turnVoicePlayed && warmupBeats > 0 && turnClip != null && beatController != null &&
                turnClip.length + 0.35f < beatController.IntervalSec &&
                AudioSettings.dspTime >= firstJudgedBeatDspTime - turnClip.length - 0.2f &&
                AudioSettings.dspTime + turnClip.length + 0.1f < firstJudgedBeatDspTime)
            {
                turnVoicePlayed = true;
                NarrationPlayer.Say("tempo_turno", interrupt: false);
            }

            if (AudioSettings.dspTime < firstJudgedBeatDspTime)
            {
                if (timerText != null)
                    timerText.text = $"Escucha el pulso\nPuntos: {score}";
                return;
            }

            warmupPending = false;
            SetFeedback("¡Tu turno!", 0.7f);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (timerText) timerText.text = $"Puntos: {score}\nRacha: {streak}";
        if (scoreText) scoreText.text = $"Puntos: {score}";
        if (streakText) streakText.text = $"Racha: {streak}";
        if (stabilityFill) stabilityFill.fillAmount = stability;
        if (stabilityLabel) stabilityLabel.text = $"ENERGÍA  {Mathf.RoundToInt(stability * 100f)}%";
    }

    void EndSession(bool completed, string exitReason)
    {
        if (!running)
            return;

        running = false;
        pausedByApplication = false;

        if (beatController) beatController.StopBeats();

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawner();

        if (runner != null)
            runner.SetGameStarted(false);

        ReportAttempt(completed, exitReason);
        SetFeedback($"¡Buen trabajo! {score} puntos. Toca Reiniciar para otra ronda.", 0f);
        if (exitReason != "restart")
            NarrationPlayer.Say("tempo_fin");
    }

    public void ReturnToMenu()
    {
        EndSession(false, "return_to_menu");
        SceneTransitionController.LoadScene("MainScene");
    }

    public void RegisterTap()
    {
        if (!IsAcceptingTaps) return;

        double tapTime = AudioSettings.dspTime;
        if (tapTime < firstJudgedBeatDspTime - assistMs * 0.001 || !beatController.IsRunning)
            return;

        float deltaMs = beatController.GetSignedDeltaToNearestBeatMs(tapTime, out double judgedBeatTime);
        if (judgedBeatTime < firstJudgedBeatDspTime - 0.0001)
            return;

        if (ignoreExtraTapsOnSameBeat && !double.IsNaN(lastJudgedBeatTime) && Math.Abs(judgedBeatTime - lastJudgedBeatTime) < 0.0001)
            return;

        float difficultyFactor = Mathf.Clamp01(streak / 20f);
        float effectivePerfectMs = Mathf.Min(CurrentPerfectMs, (float)beatController.IntervalSec * 120f);
        float effectiveGoodMs = Mathf.Min(Mathf.Lerp(goodMs, goodMs * 0.8f, difficultyFactor), (float)beatController.IntervalSec * 230f);
        float effectiveAssistMs = CurrentAssistMs;
        float absMs = Mathf.Abs(deltaMs);
        BeatSyncedObstacleSpawner.ObstacleAction action = obstacleSpawner != null
            ? obstacleSpawner.GetActionForBeat(judgedBeatTime) : BeatSyncedObstacleSpawner.ObstacleAction.Tap;

        if (absMs > effectiveAssistMs)
        {
            if (!double.IsNaN(lastMissedBeatTime) && Math.Abs(judgedBeatTime - lastMissedBeatTime) < 0.0001)
                return;
            lastMissedBeatTime = judgedBeatTime;
        }
        else
        {
            lastJudgedBeatTime = judgedBeatTime;
        }

        sumOffsetMs += deltaMs;
        if (!double.IsNaN(lastTapDspTime))
        {
            double intervalMs = (tapTime - lastTapDspTime) * 1000.0;
            intervalCount++;
            sumIntervalMs += intervalMs;
            sumSquaredIntervalMs += intervalMs * intervalMs;
        }
        lastTapDspTime = tapTime;

        if (absMs <= effectivePerfectMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit);
            perfectCount++;
            totalTapsJudged++;
            streak++;
            score += perfectScore * Mathf.Max(1, 1 + streak / 5);
            SetFeedback("Perfecto", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            PerformBeatAction(action, perfectJumpMultiplier, perfectTargetContactTime);
        }
        else if (absMs <= effectiveGoodMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit * 0.5f);
            goodCount++;
            totalTapsJudged++;
            streak++;
            score += goodScore * Mathf.Max(1, 1 + streak / 6);
            SetFeedback("Bien", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            if (jumpOnGood)
            {
                float targetTime = deltaMs < 0f ? goodEarlyTargetContactTime : goodLateTargetContactTime;
                PerformBeatAction(action, goodJumpMultiplier, targetTime);
            }
        }
        else if (absMs <= effectiveAssistMs)
        {
            totalTapsJudged++;
            streak = 0;
            score += timingOffsetScore;

            if (deltaMs < 0f)
            {
                earlyCount++;
                SetFeedback("Un poco antes", 0.5f);
                PerformBeatAction(action, earlyAssistJumpMultiplier, assistEarlyTargetContactTime);
            }
            else
            {
                lateCount++;
                SetFeedback("Un poco después", 0.5f);
                PerformBeatAction(action, lateAssistJumpMultiplier, assistLateTargetContactTime);
            }
        }
        else
        {
            totalTapsJudged++;
            missCount++;
            streak = 0;
            SetFeedback(deltaMs < 0f ? "Espera el pulso" : "Prueba el siguiente pulso", 0.6f);
            if (sfxSource && tapWrong) sfxSource.PlayOneShot(tapWrong);
            stability = Mathf.Clamp01(stability - lossOnMiss);
        }

        obstacleSpawner?.SetDifficulty(streak >= 24 ? 3 : streak >= 16 ? 2 : streak >= 8 ? 1 : 0);
        if (stability <= 0f)
            EndSession(false, "stability_depleted");
        UpdateUI();
    }

    private void PerformBeatAction(BeatSyncedObstacleSpawner.ObstacleAction action, float multiplier, float contactTime)
    {
        if (runner == null)
            return;

        if (action == BeatSyncedObstacleSpawner.ObstacleAction.Duck)
            runner.Duck();
        else if (action == BeatSyncedObstacleSpawner.ObstacleAction.Jump)
            runner.Jump(GetAdaptiveMultiplier(multiplier, contactTime));
    }

    public void RegisterCollision(float penalty)
    {
        if (!running || pausedByApplication)
            return;

        streak = 0;
        collisionCount++;
        stability = Mathf.Clamp01(stability - penalty);
        obstacleSpawner?.SetDifficulty(0);
        SetFeedback("No pasa nada, sigue el ritmo", 0.7f);
        if (stability <= 0f)
            EndSession(false, "stability_depleted");
        UpdateUI();
    }

    void SetFeedback(string msg, float seconds)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        if (feedbackClearRoutine != null)
        {
            StopCoroutine(feedbackClearRoutine);
            feedbackClearRoutine = null;
        }

        if (seconds <= 0f)
            return;

        feedbackClearRoutine = StartCoroutine(ClearFeedbackRoutine(seconds));
    }

    IEnumerator ClearFeedbackRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (feedbackText) feedbackText.text = "";
        feedbackClearRoutine = null;
    }
    public void StartGameFromButton()
    {
        if (Time.unscaledTime - lastStartPressTime < 0.5f)
            return;
        lastStartPressTime = Time.unscaledTime;

        if (running)
            EndSession(false, "restart");

        StartSession();
        if (!running)
            return;

        if (obstacleSpawner != null)
            obstacleSpawner.StartSpawner();

        if (runner != null)
            runner.SetGameStarted(true);

        if (startButtonText != null)
            startButtonText.text = "Reiniciar";
    }

    void ResetSessionStats()
    {
        score = 0;
        streak = 0;
        totalTapsJudged = 0;
        perfectCount = 0;
        goodCount = 0;
        earlyCount = 0;
        lateCount = 0;
        missCount = 0;
        collisionCount = 0;
        sumOffsetMs = 0.0;
        lastTapDspTime = double.NaN;
        intervalCount = 0;
        sumIntervalMs = 0.0;
        sumSquaredIntervalMs = 0.0;
    }

    void ReportAttempt(bool completed, string exitReason)
    {
        if (totalTapsJudged == 0 && collisionCount == 0)
            return;

        if (GameReportManager.Instance == null || SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession)
            return;

        int hits = perfectCount + goodCount;
        double meanIntervalMs = intervalCount > 0 ? sumIntervalMs / intervalCount : 0.0;
        AttemptReportData report = new AttemptReportData
        {
            miniGame = "tempo_tap",
            level = 1,
            difficulty = "adaptativa",
            bpm = beatController != null ? Mathf.RoundToInt(beatController.bpm) : 0,
            errors = earlyCount + lateCount + missCount + collisionCount,
            correctAnswers = hits,
            levelRepetitions = 0,
            completed = completed,
            timeSeconds = Mathf.Max(1, Mathf.RoundToInt(Time.time - sessionStartedAt)),
            scoreFinal = score,
            wasTutorial = false,
            exitReason = exitReason,
            tempoTap = new TempoTapMetricsData
            {
                judgedTaps = totalTapsJudged,
                perfectTaps = perfectCount,
                goodTaps = goodCount,
                earlyTaps = earlyCount,
                lateTaps = lateCount,
                missedTaps = missCount,
                collisions = collisionCount,
                hitRatePercent = totalTapsJudged > 0 ? 100f * hits / totalTapsJudged : 0f,
                meanOffsetMs = totalTapsJudged > 0 ? (float)(sumOffsetMs / totalTapsJudged) : 0f,
                intervalCount = intervalCount,
                intervalStdDevMs = intervalCount > 0
                    ? (float)Math.Sqrt(Math.Max(0.0, sumSquaredIntervalMs / intervalCount - meanIntervalMs * meanIntervalMs))
                    : 0f,
            },
        };

        GameReportManager.Instance.ReportAttempt(report, null, error => Debug.LogWarning("TempoTap report error: " + error));
    }

    float GetAdaptiveMultiplier(float defaultMultiplier, float targetContactTime)
    {
        if (obstacleSpawner == null)
            return defaultMultiplier;

        return obstacleSpawner.GetAdaptiveJumpMultiplier(defaultMultiplier, targetContactTime);
    }
}
