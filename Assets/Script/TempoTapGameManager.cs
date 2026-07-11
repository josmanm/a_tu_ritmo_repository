using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.InputSystem;

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

    [Header("Timing Windows (ms)")]
    public float perfectMs = 60f;
    public float goodMs = 120f;

    [Header("Session")]
    public float sessionSeconds = 30f;

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
    float timeLeft;
    double lastJudgedBeatTime = double.NaN;
    int score;
    int streak;
    int totalTapsJudged;
    int perfectCount;
    int goodCount;
    int earlyCount;
    int lateCount;
    int missCount;


    void Start()
    {
        running = false;
        timeLeft = sessionSeconds;
        ResetSessionStats();
        UpdateUI();

        if (beatController != null)
            beatController.StopBeats();

        if (runner != null)
            runner.SetGameStarted(false);

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawner();

        if (startPanel != null)
            startPanel.SetActive(true);

        SetFeedback("Pulsa iniciar", 0f);
    }

    public void StartSession()
    {
        running = true;
        timeLeft = sessionSeconds;
        stability = 1f;
        lastJudgedBeatTime = double.NaN;
        ResetSessionStats();
        UpdateUI();

        if (beatController) beatController.StartBeats();
        SetFeedback("Mantén el ritmo", 0.7f);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            RegisterTap();

        if (!running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            EndSession();
            return;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (timerText) timerText.text = $"Tiempo: {Mathf.CeilToInt(timeLeft)}\nPuntos: {score}\nRacha: {streak}";
        if (scoreText) scoreText.text = $"Puntos: {score}";
        if (streakText) streakText.text = $"Racha: {streak}";
        if (stabilityFill) stabilityFill.fillAmount = stability;
    }

    void EndSession()
    {
        running = false;

        if (beatController) beatController.StopBeats();

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawner();

        if (runner != null)
            runner.SetGameStarted(false);

        SetFeedback($"Fin - {score} pts", 0f);
    }

    public void RegisterTap()
    {
        if (!running || beatController == null) return;

        double tapTime = AudioSettings.dspTime;
        float deltaMs = beatController.GetSignedDeltaToNearestBeatMs(tapTime, out double judgedBeatTime);

        if (ignoreExtraTapsOnSameBeat && !double.IsNaN(lastJudgedBeatTime) && Math.Abs(judgedBeatTime - lastJudgedBeatTime) < 0.0001)
            return;

        lastJudgedBeatTime = judgedBeatTime;
        float absMs = Mathf.Abs(deltaMs);

        if (absMs <= perfectMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit);
            perfectCount++;
            totalTapsJudged++;
            streak++;
            score += perfectScore * Mathf.Max(1, 1 + streak / 5);
            SetFeedback("Perfecto", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            if (runner) runner.Jump(GetAdaptiveMultiplier(perfectJumpMultiplier, perfectTargetContactTime));
        }
        else if (absMs <= goodMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit * 0.5f);
            goodCount++;
            totalTapsJudged++;
            streak++;
            score += goodScore * Mathf.Max(1, 1 + streak / 6);
            SetFeedback("Bien", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            if (jumpOnGood && runner)
            {
                float targetTime = deltaMs < 0f ? goodEarlyTargetContactTime : goodLateTargetContactTime;
                runner.Jump(GetAdaptiveMultiplier(goodJumpMultiplier, targetTime));
            }
        }
        else if (absMs <= assistMs)
        {
            totalTapsJudged++;
            streak = 0;
            score += timingOffsetScore;

            if (deltaMs < 0f)
            {
                earlyCount++;
                SetFeedback("Muy temprano", 0.5f);
                if (runner) runner.Jump(GetAdaptiveMultiplier(earlyAssistJumpMultiplier, assistEarlyTargetContactTime));
            }
            else
            {
                lateCount++;
                SetFeedback("Muy tarde", 0.5f);
                if (runner) runner.Jump(GetAdaptiveMultiplier(lateAssistJumpMultiplier, assistLateTargetContactTime));
            }
        }
        else
        {
            totalTapsJudged++;
            missCount++;
            streak = 0;
            SetFeedback(deltaMs < 0f ? "Temprano" : "Tarde", 0.6f);
            if (sfxSource && tapWrong) sfxSource.PlayOneShot(tapWrong);

        }

        if (stability <= 0f)
        {
            EndSession();
            return;
        }

        UpdateUI();
    }

    public void RegisterCollision(float penalty)
    {
        if (!running)
            return;

        streak = 0;
        missCount++;
        stability = Mathf.Clamp01(stability - penalty);
        SetFeedback("Inténtalo otra vez", 0.7f);
        UpdateUI();

        if (stability <= 0f)
            EndSession();
    }

    void SetFeedback(string msg, float seconds)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        StopCoroutine(nameof(ClearFeedbackRoutine));

        if (seconds <= 0f)
            return;

        StartCoroutine(ClearFeedbackRoutine(seconds));
    }

    IEnumerator ClearFeedbackRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (feedbackText) feedbackText.text = "";
    }
    public void StartGameFromButton()
    {
        if (startPanel != null)
            startPanel.SetActive(false);

        if (obstacleSpawner != null)
            obstacleSpawner.StartSpawner();

        if (runner != null)
            runner.SetGameStarted(true);

        StartSession();
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
    }

    float GetAdaptiveMultiplier(float defaultMultiplier, float targetContactTime)
    {
        if (obstacleSpawner == null)
            return defaultMultiplier;

        return obstacleSpawner.GetAdaptiveJumpMultiplier(defaultMultiplier, targetContactTime);
    }
}
