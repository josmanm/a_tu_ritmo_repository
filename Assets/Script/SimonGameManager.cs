using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimonGameManager : MonoBehaviour
{
    [System.Serializable]
    public class SimonColorData
    {
        public string name;
        public string label;
        public Color normalColor;
        public Color litColor;
        public AudioClip clip;
    }

    [Header("Configuracion del tablero")]
    [SerializeField] private float pieceSize = 400f;
    [SerializeField] private RectTransform piecesRoot;
    [SerializeField] private SimonPieceUI piecePrefab;

    [Header("Sprites por cantidad de piezas")]
    [SerializeField] private Sprite pieceSprite4;
    [SerializeField] private Sprite pieceSprite5;
    [SerializeField] private Sprite pieceSprite7;

    [Header("Colores disponibles")]
    [SerializeField] private List<SimonColorData> colorPool = new List<SimonColorData>();

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Panel de estadisticas")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private TMP_Text bestStreakText;
    [SerializeField] private TMP_Text avgTimeText;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip roundTransitionClip;

    [Header("Efectos visuales")]
    [SerializeField] private SimonCelebrationEffect celebrationEffect;
    [SerializeField] private Image backgroundOverlay;
    [SerializeField] private Color bgSuccessColor = new Color(0.1f, 0.3f, 0.1f, 0.3f);
    [SerializeField] private Color bgErrorColor = new Color(0.3f, 0.1f, 0.1f, 0.3f);

    [Header("Dificultad suave")]
    [SerializeField] private float timePerInputStart = 4.5f;
    [SerializeField] private float timePerInputMin = 1.8f;
    [SerializeField] private float timeDecreasePerLevel = 0.12f;
    [SerializeField] private float flashStart = 0.5f;
    [SerializeField] private float gapStart = 0.25f;
    [SerializeField] private float flashMin = 0.25f;
    [SerializeField] private float gapMin = 0.1f;
    [SerializeField] private float flashDecreasePerLevel = 0.015f;
    [SerializeField] private float gapDecreasePerLevel = 0.008f;

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
    private const string STREAK_KEY = "SIMON_STREAK";
    private const string BEST_STREAK_KEY = "SIMON_BEST_STREAK";

    private readonly List<int> sequence = new List<int>();
    private int playerIndex = 0;

    private float flashTime;
    private float gapTime;
    private float currentInputTime;
    private float timeLimit;

    private Coroutine statusAnim;
    private Coroutine showSequenceRoutine;
    private Coroutine loseLifeRoutine;

    private int lives;
    private int score;

    private readonly List<SimonPieceUI> activePieces = new List<SimonPieceUI>();
    private int currentColorCount = 4;

    private float lastTapTime = -1f;
    private float tapCooldown = 0.2f;

    private int currentStreak = 0;
    private int bestStreak = 0;
    private readonly List<float> inputTimes = new List<float>();

    private void Start()
    {
        ValidateReferences();
        LoadStats();
        AutoCreateStatsPanel();
        BuildBoard(4);
        SetInput(false);
        ShowStatsPanel(false);

        startButton.interactable = true;
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(ReplaySequence);
        }
        score = 0;
        RefreshScoreUI();
        RefreshRecordUI();
        RefreshStatsUI();

        SetStatus("Presiona START", normalColor, animate: false);
        SetInfo("");
    }

    private void AutoCreateStatsPanel()
    {
        if (statsPanel == null)
        {
            GameObject canvas = FindObjectOfType<Canvas>()?.gameObject;
            if (canvas == null) return;

            GameObject panelObj = new GameObject("StatsPanel");
            panelObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = panelObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);
            rt.sizeDelta = new Vector2(180, 120);

            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);

            statsPanel = panelObj;

            CreateStatText(panelObj.transform, "LevelText", ref levelText, "Nivel: 0", 0);
            CreateStatText(panelObj.transform, "StreakText", ref streakText, "Racha: 0", 28);
            CreateStatText(panelObj.transform, "BestStreakText", ref bestStreakText, "Mejor: 0", 56);
            CreateStatText(panelObj.transform, "AvgTimeText", ref avgTimeText, "Promedio: -", 84);
        }
    }

    private void CreateStatText(Transform parent, string name, ref TMP_Text textRef, string defaultText, float yOffset)
    {
        GameObject txtObj = new GameObject(name);
        txtObj.transform.SetParent(parent, false);

        RectTransform rt = txtObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -yOffset);
        rt.sizeDelta = new Vector2(160, 22);

        textRef = txtObj.AddComponent<TMP_Text>();
        textRef.text = defaultText;
        textRef.fontSize = 16;
        textRef.color = Color.white;
        textRef.alignment = TextAlignmentOptions.Left;
    }

    private void Update()
    {
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
        if (piecesRoot == null) Debug.LogError("SimonGameManager: piecesRoot es null.");
        if (piecePrefab == null) Debug.LogError("SimonGameManager: piecePrefab es null.");
        if (colorPool == null || colorPool.Count == 0) Debug.LogError("SimonGameManager: colorPool esta vacio.");
    }

    private void LoadStats()
    {
        bestStreak = PlayerPrefs.GetInt(BEST_STREAK_KEY, 0);
    }

    private void SaveStats()
    {
        PlayerPrefs.SetInt(STREAK_KEY, currentStreak);
        if (currentStreak > bestStreak)
        {
            bestStreak = currentStreak;
            PlayerPrefs.SetInt(BEST_STREAK_KEY, bestStreak);
        }
        PlayerPrefs.Save();
    }

    private float GetAverageInputTime()
    {
        if (inputTimes.Count == 0) return 0;
        float sum = 0;
        foreach (float t in inputTimes) sum += t;
        return sum / inputTimes.Count;
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

        foreach (Transform child in piecesRoot)
            Destroy(child.gameObject);

        activePieces.Clear();

        Sprite spriteToUse = pieceCount switch
        {
            4 => pieceSprite4,
            5 => pieceSprite5,
            7 => pieceSprite7,
            _ => pieceSprite4
        };

        float angleStep = 360f / pieceCount;

        for (int i = 0; i < pieceCount; i++)
        {
            SimonPieceUI piece = Instantiate(piecePrefab, piecesRoot);
            RectTransform rt = piece.GetComponent<RectTransform>();
            piece.SetSprite(spriteToUse);

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(pieceSize, pieceSize);
            rt.localScale = Vector3.one;

            float rotationZ = -i * angleStep;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            piece.Setup(i, colorPool[i].normalColor, colorPool[i].litColor, colorPool[i].label, OnPlayerPress);
            piece.SetLabelRotation(-rotationZ);

            activePieces.Add(piece);
        }

        currentColorCount = pieceCount;
    }

    public void StartGame()
    {
        StopAllCoroutines();

        sequence.Clear();
        playerIndex = 0;
        score = 0;
        currentStreak = 0;
        inputTimes.Clear();
        RefreshScoreUI();

        BuildBoard(4);
        AddStep();
        UpdateBoardForLevel();
        UpdateDifficultyForLevel();
        ResetPlayerTimeLimit();

        ShowStatsPanel(true);
        RefreshStatsUI();

        SetStatus("Memoriza la secuencia", warningColor);
        SetInfo("");

        if (replayButton != null) replayButton.gameObject.SetActive(true);

        showSequenceRoutine = StartCoroutine(ShowSequence());
        lives = maxLives;
        UpdateLivesUI();
    }

    public void ReplaySequence()
    {
        if (currentState != GameState.PlayerTurn && currentState != GameState.Idle) return;

        StopAllCoroutines();
        currentStreak = 0;
        inputTimes.Clear();
        RefreshStatsUI();

        SetStatus("Escucha de nuevo", warningColor);
        showSequenceRoutine = StartCoroutine(ShowSequence());
    }

    private void AddStep()
    {
        sequence.Add(Random.Range(0, activePieces.Count));
    }

    private IEnumerator ShowSequence()
    {
        currentState = GameState.ShowingSequence;

        if (timeBarRoot != null) timeBarRoot.SetActive(true);
        SetInput(false);
        startButton.interactable = false;
        if (replayButton != null) replayButton.interactable = false;
        SetInfo("");

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < sequence.Count; i++)
        {
            int step = sequence[i];

            activePieces[step].ShowLit();
            PlayColorSound(step);

            yield return new WaitForSeconds(flashTime);

            activePieces[step].SetOff();
            yield return new WaitForSeconds(gapTime);
        }

        currentState = GameState.PlayerTurn;
        playerIndex = 0;

        ResetPlayerTimeLimit();

        SetStatus("Tu turno", normalColor, animate: false);
        SetInput(true);
        startButton.interactable = true;
        if (replayButton != null) replayButton.interactable = true;

        if (timeBarFill != null)
            timeBarFill.transform.parent.gameObject.SetActive(true);
    }

    private void OnPlayerPress(int idx)
    {
        if (currentState != GameState.PlayerTurn) return;

        float now = Time.time;
        if (now - lastTapTime < tapCooldown) return;
        lastTapTime = now;

        activePieces[idx].FlashOn();

        if (idx != sequence[playerIndex])
        {
            TriggerHapticError();
            StartLoseLife("Fallaste");
            return;
        }

        PlayColorSound(idx);
        TriggerHapticSuccess();

        float usedTime = timeLimit - currentInputTime;
        inputTimes.Add(usedTime);

        Vector3 piecePos = activePieces[idx].transform.position;
        celebrationEffect.PlayPerfectEffect(piecePos);

        currentStreak++;
        SaveStats();

        playerIndex++;
        ResetPlayerTimeLimit();
        RefreshStatsUI();

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

        SetStatus("Excelente!", successColor);
        SetInfo("");
        RefreshStatsUI();

        celebrationEffect.PlayLevelUpEffect();

        yield return new WaitForSeconds(0.8f);

        AddStep();
        UpdateBoardForLevel();
        UpdateDifficultyForLevel();
        ResetPlayerTimeLimit();

        SetStatus("Memoriza la secuencia", warningColor, animate: false);
        yield return StartCoroutine(ShowSequence());
    }

    private void UpdateDifficultyForLevel()
    {
        int level = Mathf.Max(1, sequence.Count);

        float progress = (level - 1) / 20f;
        progress = Mathf.Clamp01(progress);

        flashTime = Mathf.Lerp(flashStart, flashMin, progress);
        gapTime = Mathf.Lerp(gapStart, gapMin, progress);
    }

    private void ResetPlayerTimeLimit()
    {
        int level = Mathf.Max(1, sequence.Count);

        float progress = (level - 1) / 25f;
        progress = Mathf.Clamp01(progress);

        currentInputTime = Mathf.Lerp(timePerInputStart, timePerInputMin, progress);
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

        if (gameOverClip != null && sfxSource != null)
            sfxSource.PlayOneShot(gameOverClip);

        TriggerHapticError();
        celebrationEffect.PlayErrorEffect(Vector3.zero);

        SaveStats();
        SaveRecordIfNeeded(sequence.Count);
        RefreshRecordUI();
        RefreshStatsUI();

        SetStatus($"{reason}. Nivel: {sequence.Count}", errorColor);
        SetInfo("Presiona START");

        sequence.Clear();
        playerIndex = 0;
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

    private void RefreshStatsUI()
    {
        if (levelText != null) levelText.text = $"Nivel: {sequence.Count}";
        if (streakText != null) streakText.text = $"Racha: {currentStreak}";
        if (bestStreakText != null) bestStreakText.text = $"Mejor racha: {bestStreak}";

        float avgTime = GetAverageInputTime();
        if (avgTimeText != null)
        {
            if (avgTime > 0)
                avgTimeText.text = $"Tiempo promedio: {avgTime:0.0}s";
            else
                avgTimeText.text = "Tiempo promedio: -";
        }
    }

    private void ShowStatsPanel(bool show)
    {
        if (statsPanel != null)
            statsPanel.SetActive(show);
    }

    private void SetInput(bool value)
    {
        for (int i = 0; i < activePieces.Count; i++)
            activePieces[i].SetInteractable(value);
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
        if (idx < 0 || idx >= activePieces.Count) return;
        activePieces[idx].ShowLit();
    }

    private void ShowAllOff()
    {
        for (int i = 0; i < activePieces.Count; i++)
            activePieces[i].SetOff();
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

        SetStatus($"{reason}. Te quedan {lives}", errorColor);
        SetInfo("Repite la secuencia");
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
            scoreText.text = $"Puntos: {score}";
    }

    private void UpdateBoardForLevel()
    {
        int level = Mathf.Max(1, sequence.Count);
        int neededCount = GetColorCountForLevel(level);

        if (neededCount != currentColorCount)
            BuildBoard(neededCount);
    }
}
