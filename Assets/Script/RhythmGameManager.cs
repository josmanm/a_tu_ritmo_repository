using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RhythmGameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons (3 lanes)")]
    [SerializeField] private Button[] laneButtons;

    [Header("Lane Points")]
    [SerializeField] private RectTransform[] spawnPoints;
    [SerializeField] private RectTransform[] hitPoints;

    [Header("Note Prefab")]
    [SerializeField] private NoteUI notePrefab;
    [SerializeField] private RectTransform notesParent;

    [Header("Gameplay")]
    [SerializeField] private float noteSpeed = 320f;
    [SerializeField] private float spawnInterval = 1.1f;
    [SerializeField] private float hitWindowPx = 90f;
    [SerializeField] private float missExtraPx = 140f;

    [Header("Lane Note Sprites")]
    [SerializeField] private Sprite[] laneNoteSprites;

    [Header("Lane Colors (0=Blue, 1=Green, 2=Red)")]
    [SerializeField] private Color[] laneColors = new Color[] { 
        new Color(0.2f, 0.5f, 1f, 1f),  // Azul
        new Color(0.2f, 0.9f, 0.4f, 1f), // Verde  
        new Color(1f, 0.3f, 0.3f, 1f)    // Rojo
    };

    [Header("Feedback")]
    [SerializeField] private float hitPopScale = 1.15f;
    [SerializeField] private float hitPopTime = 0.08f;
    [SerializeField] private Color hitTint = new Color(0.7f, 1f, 0.7f);

    [Header("Vidas")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private Image[] lifeIcons;
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    private enum GameState { Idle, Playing, Paused, GameOver }
    private GameState currentState = GameState.Idle;

    private const string RECORD_KEY = "RHYTHM_RECORD";
    private const string BEST_STREAK_KEY = "RHYTHM_BEST_STREAK";

    private readonly List<NoteUI> activeNotes = new List<NoteUI>();
    private int score = 0;
    private int level = 1;
    private int lives;

    private int currentStreak = 0;
    private int bestStreak = 0;
    private int totalNotesHit = 0;
    private int totalNotesMissed = 0;
    private int consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 5;

    private float currentNoteSpeed;
    private float currentSpawnInterval;

    private float lastTapTime = -1f;
    private float tapCooldown = 0.15f;

    private Coroutine spawnRoutine;

    private void Start()
    {
        ValidateReferences();
        LoadStats();
        UpdateScore();
        UpdateLivesUI();

        for (int i = 0; i < laneButtons.Length; i++)
        {
            int idx = i;
            laneButtons[i].onClick.RemoveAllListeners();
            laneButtons[i].onClick.AddListener(() => OnLanePress(idx));

            Image btnImage = laneButtons[i].GetComponent<Image>();
            if (btnImage != null && laneColors != null && laneColors.Length > i)
            {
                btnImage.color = laneColors[i];
            }

            Image hitImage = hitPoints[i]?.GetComponent<Image>();
            if (hitImage != null && laneColors != null && laneColors.Length > i)
            {
                hitImage.color = laneColors[i];
            }
        }

        StartGame();
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];

            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            if (note.isResolving)
                continue;

            RectTransform rt = (RectTransform)note.transform;
            rt.anchoredPosition += Vector2.down * currentNoteSpeed * Time.deltaTime;

            float hitY = hitPoints[note.laneIndex].anchoredPosition.y;

            if (!note.wasHit && rt.anchoredPosition.y < (hitY - hitWindowPx - missExtraPx))
            {
                note.wasHit = true;
                totalNotesMissed++;
                consecutiveErrors++;
                Destroy(note.gameObject);
                activeNotes.RemoveAt(i);

                TriggerHapticError();

                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    GameOver("Demasiados errores");
                    return;
                }

                SetStatus("Casi...", new Color(1f, 0.85f, 0.2f), animate: false);
            }
        }
    }

    private void ValidateReferences()
    {
        if (laneButtons == null || laneButtons.Length < 3)
            Debug.LogError("RhythmGameManager: Faltan laneButtons");

        if (spawnPoints == null || hitPoints == null)
            Debug.LogError("RhythmGameManager: Faltan spawnPoints o hitPoints");
    }

    private void LoadStats()
    {
        bestStreak = PlayerPrefs.GetInt(BEST_STREAK_KEY, 0);
    }

    private void SaveStats()
    {
        if (currentStreak > bestStreak)
        {
            bestStreak = currentStreak;
            PlayerPrefs.SetInt(BEST_STREAK_KEY, bestStreak);
            PlayerPrefs.Save();
        }
    }

    public void StartGame()
    {
        if (currentState == GameState.Playing) return;

        currentState = GameState.Playing;
        score = 0;
        level = 1;
        lives = maxLives;
        currentStreak = 0;
        totalNotesHit = 0;
        totalNotesMissed = 0;
        consecutiveErrors = 0;

        currentNoteSpeed = noteSpeed;
        currentSpawnInterval = spawnInterval;

        UpdateScore();
        UpdateLivesUI();

        SetStatus("¡Sigue el ritmo!", new Color(0.2f, 0.9f, 0.4f));

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        SetStatus("Pausado", Color.yellow);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        spawnRoutine = StartCoroutine(SpawnLoop());

        SetStatus("¡Sigue el ritmo!", new Color(0.2f, 0.9f, 0.4f));
    }

    private IEnumerator SpawnLoop()
    {
        while (currentState == GameState.Playing)
        {
            SpawnNote(Random.Range(0, 3));

            IncreaseDifficulty();

            yield return new WaitForSeconds(currentSpawnInterval);
        }
    }

    private void IncreaseDifficulty()
    {
        int noteCount = totalNotesHit + totalNotesMissed;
        int newLevel = Mathf.Max(1, noteCount / 10 + 1);

        if (newLevel > level)
        {
            level = newLevel;
            float speedMultiplier = Mathf.Clamp(1f + (level - 1) * 0.05f, 1f, 2f);
            float intervalMultiplier = Mathf.Clamp(1f - (level - 1) * 0.03f, 0.5f, 1f);

            currentNoteSpeed = noteSpeed * speedMultiplier;
            currentSpawnInterval = spawnInterval * intervalMultiplier;
        }
    }

    private void SpawnNote(int lane)
    {
        NoteUI note = Instantiate(notePrefab, notesParent);
        note.laneIndex = lane;
        note.wasHit = false;
        note.isResolving = false;

        RectTransform rt = (RectTransform)note.transform;
        rt.anchoredPosition = spawnPoints[lane].anchoredPosition;

        if (laneNoteSprites != null && laneNoteSprites.Length > lane && note.img != null)
            note.img.sprite = laneNoteSprites[lane];

        if (note.img != null && laneColors != null && laneColors.Length > lane)
            note.img.color = laneColors[lane];

        activeNotes.Add(note);
    }

    private void OnLanePress(int lane)
    {
        if (currentState != GameState.Playing) return;

        float now = Time.time;
        if (now - lastTapTime < tapCooldown) return;
        lastTapTime = now;

        NoteUI best = null;
        float bestDist = float.MaxValue;
        float hitY = hitPoints[lane].anchoredPosition.y;

        foreach (var n in activeNotes)
        {
            if (n == null || n.wasHit) continue;
            if (n.laneIndex != lane) continue;

            float dist = Mathf.Abs(((RectTransform)n.transform).anchoredPosition.y - hitY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = n;
            }
        }

        if (best != null && bestDist <= hitWindowPx)
        {
            best.wasHit = true;
            best.isResolving = true;

            score += 10 * level;
            totalNotesHit++;
            currentStreak++;
            consecutiveErrors = 0;

            SaveStats();

            TriggerHapticSuccess();

            UpdateScore();
            SetStatus("¡Bien!", new Color(0.2f, 0.9f, 0.4f));

            activeNotes.Remove(best);
            StartCoroutine(HitAndDestroy(best));
        }
        else
        {
            currentStreak = 0;
            SetStatus("Intenta de nuevo", new Color(1f, 0.85f, 0.2f), animate: false);
        }
    }

    private void GameOver(string reason)
    {
        currentState = GameState.GameOver;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

       SaveStats();

        int record = PlayerPrefs.GetInt(RECORD_KEY, 0);
        if (score > record)
        {
            PlayerPrefs.SetInt(RECORD_KEY, score);
            PlayerPrefs.Save();
        }

        TriggerHapticError();

        SetStatus($"{reason}! Puntos: {score}", new Color(1f, 0.3f, 0.3f));
    }

    private void UpdateScore()
    {
        if (scoreText != null)
            scoreText.text = $"Puntos: {score}";
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

    private void SetStatus(string msg, Color color, bool animate = false)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = color;
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

    private IEnumerator HitAndDestroy(NoteUI note)
    {
        if (note == null) yield break;
        yield return StartCoroutine(HitFeedback(note));
        if (note != null) Destroy(note.gameObject);
    }

    private IEnumerator HitFeedback(NoteUI note)
    {
        if (note == null) yield break;

        var rt = note.transform as RectTransform;
        var img = note.img;

        if (rt == null || img == null) yield break;

        Vector3 originalScale = rt.localScale;
        Color originalColor = img.color;

        img.color = hitTint;
        rt.localScale = originalScale * hitPopScale;

        yield return new WaitForSeconds(hitPopTime);

        if (note == null || rt == null || img == null) yield break;

        rt.localScale = originalScale;
        img.color = originalColor;
    }

}
