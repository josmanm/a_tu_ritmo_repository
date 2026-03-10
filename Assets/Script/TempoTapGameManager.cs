using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TempoTapGameManager : MonoBehaviour
{
    public BeatController beatController;

    [Header("UI")]
    public TMP_Text feedbackText;
    public TMP_Text timerText;
    public Image stabilityFill;

    [Header("UI Panels")]
    public GameObject startPanel;

    [Header("Timing Windows (ms)")]
    public float perfectMs = 60f;
    public float goodMs = 120f;

    [Header("Session")]
    public float sessionSeconds = 30f;

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

    [Header("Asistencia (siempre activa)")]
    public float assistMs = 180f;   // ventana extra para que igual salte
    public float assistedJumpMultiplier = 0.9f; // salto un poco más bajo si quieres

    public BeatSyncedObstacleSpawner obstacleSpawner;

    bool running;
    float timeLeft;


    void Start()
    {
        running = false;
        timeLeft = sessionSeconds;
        UpdateUI();

        if (beatController != null)
            beatController.StopBeats();
    }

    public void StartSession()
    {
        running = true;
        timeLeft = sessionSeconds;
        stability = 1f;
        UpdateUI();

        if (beatController) beatController.StartBeats();
        SetFeedback("Mantén el ritmo", 0.7f);
    }

    void Update()
    {
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
        if (timerText) timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        if (stabilityFill) stabilityFill.fillAmount = stability;
    }

    void EndSession()
    {
        running = false;

        if (beatController) beatController.StopBeats();

        if (obstacleSpawner != null)
            obstacleSpawner.StopSpawner();

        SetFeedback("¡Listo!", 1.2f);
    }

    public void RegisterTap()
    {
        if (!running || beatController == null) return;

        double tapTime = AudioSettings.dspTime;
        double beatTime = beatController.LastBeatDspTime;

        double deltaSec = tapTime - beatTime;
        float deltaMs = (float)(deltaSec * 1000.0f);
        float absMs = Mathf.Abs(deltaMs);

        if (absMs <= perfectMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit);
            SetFeedback("Perfecto", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            if (runner) runner.Jump(1f);
        }
        else if (absMs <= goodMs)
        {
            stability = Mathf.Clamp01(stability + gainOnHit * 0.5f);
            SetFeedback("Bien", 0.5f);
            if (sfxSource && tapCorrect) sfxSource.PlayOneShot(tapCorrect);
            if (runner) runner.Jump(1f);
        }
        else if (absMs <= assistMs)
        {
            // Asistencia: igual salta pero “no perfecto”
            stability = Mathf.Clamp01(stability - 0.02f); // castigo mínimo o ninguno
            SetFeedback("Casi ", 0.5f);
            if (runner) runner.Jump(assistedJumpMultiplier);
        }
        else
        {
            stability = Mathf.Clamp01(stability - lossOnMiss);
            SetFeedback("Ups…", 0.6f);
            if (sfxSource && tapWrong) sfxSource.PlayOneShot(tapWrong);

        }
        UpdateUI();
    }

    void SetFeedback(string msg, float seconds)
    {
        if (!feedbackText) return;
        feedbackText.text = msg;
        StopCoroutine(nameof(ClearFeedbackRoutine));
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
}
