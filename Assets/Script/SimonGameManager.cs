using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimonGameManager : MonoBehaviour
{
    // -------------------- CONFIG --------------------
    [Header("Main Image (Circle)")]
    [SerializeField] private Image simonImage;

    [Header("Sprites")]
    [SerializeField] private Sprite baseSprite;
    [SerializeField] private Sprite redOn;
    [SerializeField] private Sprite yellowOn;
    [SerializeField] private Sprite greenOn;
    [SerializeField] private Sprite blueOn;

    [Header("UI")]
    [SerializeField] private Button startButton;

    // Un texto grande para estados ("Tu turno", "Fallaste", etc.)
    [SerializeField] private TMP_Text statusText;
    // Un texto pequeno para tiempo u otra info (opcional)
    [SerializeField] private TMP_Text infoText;
    // Record fijo (arriba izq)
    [SerializeField] private TMP_Text recordText;

    [Header("Input Buttons (Transparent zones)")]
    [SerializeField] private Button btnRed;
    [SerializeField] private Button btnYellow;
    [SerializeField] private Button btnGreen;
    [SerializeField] private Button btnBlue;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip redClip;
    [SerializeField] private AudioClip yellowClip;
    [SerializeField] private AudioClip greenClip;
    [SerializeField] private AudioClip blueClip;
    [SerializeField] private AudioClip failClip; // el audio que se usara para cuando se pierda uan vida 
    [SerializeField] private AudioClip gameOverClip; // el audio que se usara para cuando se pierda el juego (vidas = 0)

    [Header("Difficulty - Sequence Speed")]
    [SerializeField] private float flashStart = 0.45f;
    [SerializeField] private float gapStart = 0.20f;
    [SerializeField] private float flashMin = 0.18f;
    [SerializeField] private float gapMin = 0.06f;
    [SerializeField] private float flashDecreasePerLevel = 0.02f;
    [SerializeField] private float gapDecreasePerLevel = 0.01f;

    [Header("Difficulty - Player Time Limit")]
    [SerializeField] private float timePerInputStart = 3.0f;
    [SerializeField] private float timePerInputMin = 1.2f;
    [SerializeField] private float timeDecreasePerLevel = 0.1f;

    [Header("Status Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color successColor = new Color(0.2f, 0.9f, 0.4f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f);

    [Header("Status Animation")]
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.15f;


    [Header("Time Bar UI")]
    [SerializeField] private Image timeBarFill;

    [SerializeField] private Color timeOkColor = Color.white;
    [SerializeField] private Color timeWarnColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color timeLowColor = new Color(1f, 0.3f, 0.3f);

    [SerializeField] private float warnThreshold = 0.35f; // 35%
    [SerializeField] private float lowThreshold = 0.15f;  // 15%

    [Header("Lives UI")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private Image[] lifeIcons;   // arrastra Life1, Life2, Life3
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    [SerializeField] private GameObject timeBarRoot; // el objeto que contiene la barra completa


    // -------------------- STATE --------------------

    private const string RECORD_KEY = "SIMON_RECORD";

    // 0=Red,1=Yellow,2=Green,3=Blue
    private readonly List<int> sequence = new List<int>();
    private int playerIndex = 0;

    private bool isShowing = false;
    private bool isPlayerTurn = false;

    private float flashTime;
    private float gapTime;

    private float currentInputTime;

    private Coroutine statusAnim;

    private int lives;

    private float timeLimit; // tiempo maximo del turno actual

    private Coroutine showSequenceRoutine;
    private bool isLosingLife;

    // -------------------- UNITY --------------------

    private void Start()
    {
        // Conectar clicks
        btnRed.onClick.AddListener(() => OnPlayerPress(0));
        btnYellow.onClick.AddListener(() => OnPlayerPress(1));
        btnGreen.onClick.AddListener(() => OnPlayerPress(2));
        btnBlue.onClick.AddListener(() => OnPlayerPress(3));

        // Estado inicial
        simonImage.sprite = baseSprite;
        SetInput(false);

        startButton.interactable = true;

        RefreshRecordUI();
        SetStatus("Presiona START", normalColor, animate: false);
        SetInfo(""); // sin tiempo al inicio
    }

    private void Update()
    {
        // Temporizador solo durante el turno del jugador
        if (!isPlayerTurn) return;

        currentInputTime -= Time.deltaTime;
        SetInfo($"Tiempo: {currentInputTime:0.0}s");

        if (currentInputTime <= 0f)
        {
            StartCoroutine(LoseLife("Tiempo agotado"));
        }

        if (timeBarFill != null && timeLimit > 0f)
        {
            float pct = Mathf.Clamp01(currentInputTime / timeLimit);
            timeBarFill.fillAmount = pct;

            if (pct <= lowThreshold) timeBarFill.color = timeLowColor;
            else if (pct <= warnThreshold) timeBarFill.color = timeWarnColor;
            else timeBarFill.color = timeOkColor;
        }
    }

    // -------------------- PUBLIC UI --------------------

    public void StartGame()
    {
        StopAllCoroutines();

        sequence.Clear();
        playerIndex = 0;

        simonImage.sprite = baseSprite;

        AddStep();

        UpdateDifficultyForLevel();
        ResetPlayerTimeLimit();

        SetStatus("Memoriza la secuencia", warningColor);
        SetInfo("");

        showSequenceRoutine = StartCoroutine(ShowSequence());
        lives = maxLives;
        UpdateLivesUI();

    }

    // -------------------- GAME FLOW --------------------

    private void AddStep()
    {
        sequence.Add(Random.Range(0, 4));
    }

    private IEnumerator ShowSequence()
    {
        if (timeBarRoot != null) timeBarRoot.SetActive(true);
        isShowing = true;
        isPlayerTurn = false;

        SetInput(false);
        startButton.interactable = false;

        SetInfo("");

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < sequence.Count; i++)
        {
            int step = sequence[i];

            ShowOn(step);
            PlayColorSound(step);

            yield return new WaitForSeconds(flashTime);

            simonImage.sprite = baseSprite;
            yield return new WaitForSeconds(gapTime);
        }

        // Turno del jugador
        isShowing = false;
        isPlayerTurn = true;
        playerIndex = 0;

        ResetPlayerTimeLimit();

        SetStatus("Tu turno", normalColor, animate: false);
        SetInput(true);
        startButton.interactable = true;
        if (timeBarFill != null) timeBarFill.transform.parent.gameObject.SetActive(true);

    }

    private void OnPlayerPress(int idx)
    {
        if (isShowing || !isPlayerTurn) return;

        // Feedback visual inmediato (esto sí lo puedes dejar)
        ShowOn(idx);
        StartCoroutine(BackToBaseAfter(0.12f));

        // Validar primero
        bool isWrong = (sequence.Count == 0 || idx != sequence[playerIndex]);

        if (isWrong)
        {
            // Opcional: si quieres que al fallar NO se vea el color, comenta el ShowOn de arriba.
            StartCoroutine(LoseLife("Fallaste"));
            return;
        }

        // Si acertó, ahora sí suena el color
        PlayColorSound(idx);

        playerIndex++;
        ResetPlayerTimeLimit();

        if (playerIndex >= sequence.Count)
        {
            isPlayerTurn = false;
            SetInput(false);
            StartCoroutine(NextRound());
        }
    }

    private IEnumerator NextRound()
    {
        SetStatus("¡Ronda completada!", successColor);
        SetInfo("");
        yield return new WaitForSeconds(0.8f);

        AddStep();

        UpdateDifficultyForLevel();
        ResetPlayerTimeLimit();

        SetStatus("Memoriza la secuencia", warningColor, animate: false);
        yield return StartCoroutine(ShowSequence());
    }

    private IEnumerator BackToBaseAfter(float t)
    {
        yield return new WaitForSeconds(t);
        simonImage.sprite = baseSprite;
    }

    // -------------------- DIFFICULTY --------------------

    private void UpdateDifficultyForLevel()
    {
        int level = Mathf.Max(1, sequence.Count);

        flashTime = Mathf.Max(flashMin, flashStart - (level - 1) * flashDecreasePerLevel);
        gapTime = Mathf.Max(gapMin, gapStart - (level - 1) * gapDecreasePerLevel);
    }

    private void ResetPlayerTimeLimit()
    {
        int level = Mathf.Max(1, sequence.Count);

        currentInputTime = Mathf.Max(
            timePerInputMin,
            timePerInputStart - (level - 1) * timeDecreasePerLevel
        );

        timeLimit = currentInputTime;  // guarda el maximo para calcular porcentaje

        if (timeBarFill != null)
        {
            timeBarFill.fillAmount = 1f;
            timeBarFill.color = timeOkColor;
        }
    }

    // -------------------- GAME OVER + RECORD --------------------

    private void GameOver(string reason)
    {
        StopAllCoroutines();

        isShowing = false;
        isPlayerTurn = false;

        SetInput(false);
        startButton.interactable = true;

        if (gameOverClip && sfxSource) sfxSource.PlayOneShot(gameOverClip);

        int currentLevel = sequence.Count;

        SaveRecordIfNeeded(currentLevel);
        RefreshRecordUI();

        simonImage.sprite = baseSprite;

        SetStatus($"{reason}. Nivel: {currentLevel}", errorColor);
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
        if (recordText != null) recordText.text = $"Récord: {record}";
    }

    // -------------------- UI HELPERS --------------------

    private void SetInput(bool value)
    {
        btnRed.interactable = value;
        btnYellow.interactable = value;
        btnGreen.interactable = value;
        btnBlue.interactable = value;
    }

    private void SetStatus(string msg, Color color, bool animate = true)
    {
        if (statusText == null) return;

        statusText.text = msg;
        statusText.color = color;

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
        Vector3 original = Vector3.one;

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

    // -------------------- VISUALS + AUDIO --------------------

    private void ShowOn(int idx)
    {
        switch (idx)
        {
            case 0: simonImage.sprite = redOn; break;
            case 1: simonImage.sprite = yellowOn; break;
            case 2: simonImage.sprite = greenOn; break;
            case 3: simonImage.sprite = blueOn; break;
        }
    }

    private void PlayColorSound(int idx)
    {
        if (sfxSource == null) return;

        AudioClip clipToPlay = idx switch
        {
            0 => redClip,
            1 => yellowClip,
            2 => greenClip,
            3 => blueClip,
            _ => null
        };

        if (clipToPlay == null) return;

        sfxSource.PlayOneShot(clipToPlay);
    }
    IEnumerator LoseLife(string reason)
    {
        if (isLosingLife) yield break; // evita doble llamada
        isLosingLife = true;

        // Detén SOLO la secuencia si estaba corriendo
        if (showSequenceRoutine != null)
        {
            StopCoroutine(showSequenceRoutine);
            showSequenceRoutine = null;
        }

        isShowing = false;
        isPlayerTurn = false;
        SetInput(false);

        lives--;
        UpdateLivesUI();

        if (lives <= 0)
        {
            GameOver(reason);
            isLosingLife = false;
            yield break;
        }

        if (sfxSource) sfxSource.Stop();
        if (failClip && sfxSource) sfxSource.PlayOneShot(failClip);

        simonImage.sprite = baseSprite;
        SetStatus($"{reason}. Te quedan {lives}", errorColor);
        SetInfo("Repite la secuencia");

        playerIndex = 0;

        // ✅ pausa
        yield return new WaitForSeconds(1f);

        showSequenceRoutine = StartCoroutine(ShowSequence());
        isLosingLife = false;
    }

    void UpdateLivesUI()
    {
        if (lifeIcons == null) return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null) continue;
            lifeIcons[i].sprite = (i < lives) ? heartFull : heartEmpty;
        }
    }
}
