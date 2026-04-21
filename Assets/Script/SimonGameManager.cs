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

    [Header("Posicion de letras")]
    [SerializeField] private Vector2 labelPositionFor4Pieces = new Vector2(-140f, 140f);
    [SerializeField] private Vector2 labelPositionFor5Pieces = new Vector2(-165f, 70f);
    [SerializeField] private Vector2 labelPositionFor7Pieces = new Vector2(-150f, 55f);
    [SerializeField] private Vector2[] labelPositionsFor5ByIndex = new Vector2[5];
    [SerializeField] private Vector2[] labelPositionsFor7ByIndex = new Vector2[7];
    [SerializeField] private float labelFontSizeFor4Pieces = 30f;
    [SerializeField] private float labelFontSizeFor5Pieces = 24f;
    [SerializeField] private float labelFontSizeFor7Pieces = 20f;
    [SerializeField] private float[] labelXPatternFallback = new float[] { -140f, 7f, 140f };

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private TMP_Text scoreText;

    // Panel de estadisticas eliminado - UI mas limpia

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
    [SerializeField] private float flashStart = 0.5f;
    [SerializeField] private float gapStart = 0.25f;
    [SerializeField] private float flashMin = 0.25f;
    [SerializeField] private float gapMin = 0.1f;

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

    private void Start()
    {
        ValidateReferences();
        BuildBoard(4);
        SetInput(false);

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

        SetStatus("Presiona JUGAR para comenzar", normalColor, animate: false);
        SetInfo("");
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

            if (spriteToUse != null)
            {
                rt.sizeDelta = new Vector2(spriteToUse.texture.width, spriteToUse.texture.height);
            }
            else
            {
                rt.sizeDelta = new Vector2(pieceSize, pieceSize);
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            float rotationZ = -i * angleStep;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            piece.Setup(i, colorPool[i].normalColor, colorPool[i].litColor, colorPool[i].label, OnPlayerPress);
            piece.SetLabelRotation(-rotationZ);
            ApplyLabelPosition(piece, i, pieceCount);

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
        currentLevel = 1;
        RefreshScoreUI();

        BuildBoard(4);
        AddStep();
        UpdateBoardForLevel();
        UpdateDifficultyForLevel();
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
        if (currentState != GameState.PlayerTurn && currentState != GameState.Idle) return;

        StopAllCoroutines();

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
        SetInfo($"Nivel {currentLevel}");

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

        Vector3 piecePos = activePieces[idx].transform.position;
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

        yield return new WaitForSeconds(0.8f);

        AddStep();
        UpdateBoardForLevel();
        UpdateDifficultyForLevel();
        ResetPlayerTimeLimit();

        SetStatus("Mira la secuencia y repitela tocando los colores", warningColor, animate: false);
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
            scoreText.text = $"Puntos\n {score}";
    }

    private void UpdateBoardForLevel()
    {
        int level = Mathf.Max(1, sequence.Count);
        int neededCount = GetColorCountForLevel(level);

        if (neededCount != currentColorCount)
            BuildBoard(neededCount);
    }

    private void ApplyLabelPosition(SimonPieceUI piece, int index, int pieceCount)
    {
        if (piece == null) return;

        if (pieceCount == 4)
        {
            piece.SetLabelAnchoredPosition(labelPositionFor4Pieces.x, labelPositionFor4Pieces.y);
            piece.SetLabelFontSize(labelFontSizeFor4Pieces);
            return;
        }

        if (pieceCount == 5)
        {
            Vector2 p = GetPerIndexLabelPosition(labelPositionsFor5ByIndex, index, labelPositionFor5Pieces);
            piece.SetLabelAnchoredPosition(p.x, p.y);
            piece.SetLabelFontSize(labelFontSizeFor5Pieces);
            return;
        }

        if (pieceCount == 7)
        {
            Vector2 p = GetPerIndexLabelPosition(labelPositionsFor7ByIndex, index, labelPositionFor7Pieces);
            piece.SetLabelAnchoredPosition(p.x, p.y);
            piece.SetLabelFontSize(labelFontSizeFor7Pieces);
            return;
        }

        if (labelXPatternFallback == null || labelXPatternFallback.Length == 0) return;
        piece.SetLabelAnchoredX(labelXPatternFallback[index % labelXPatternFallback.Length]);
    }

    private Vector2 GetPerIndexLabelPosition(Vector2[] byIndex, int index, Vector2 fallback)
    {
        if (byIndex == null || byIndex.Length == 0) return fallback;
        if (index < 0 || index >= byIndex.Length) return fallback;

        Vector2 p = byIndex[index];
        if (Mathf.Approximately(p.x, 0f) && Mathf.Approximately(p.y, 0f))
            return fallback;

        return p;
    }
}
