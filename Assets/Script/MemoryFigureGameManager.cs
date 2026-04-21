using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryFigureGameManager : MonoBehaviour
{
    [System.Serializable]
    private class FigureData
    {
        public string displayName;
        public string description;
        public Sprite sprite;
    }

    [Header("Figuras")]
    [SerializeField] private Sprite redondaSprite;
    [SerializeField] private Sprite blancaSprite;
    [SerializeField] private Sprite negraSprite;
    [SerializeField] private Sprite corcheaSprite;
    [SerializeField] private Sprite semicorcheaSprite;

    [Header("UI")]
    [SerializeField] private TMP_Text textFind;
    [SerializeField] private Image figureImage;
    [SerializeField] private TMP_Text textDescription;
    [SerializeField] private TMP_Text textQuestion;
    [SerializeField] private TMP_Text pointText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Image[] optionImages;
    [SerializeField] private Button noneOptionButton;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    [Header("Juego")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int normalPoints = 10;
    [SerializeField] private int fastPoints = 20;
    [SerializeField] private float fastAnswerSeconds = 2f;
    [SerializeField] private float previewSeconds = 3f;
    [SerializeField] private float phaseTransitionSeconds = 0.5f;
    [SerializeField] private float nextRoundDelay = 0.8f;
    [SerializeField] private float hideBeforeObserveSeconds = 0.2f;
    [SerializeField] private float missingCorrectChance = 0.35f;
    [SerializeField] private string observePhaseMessage = "Observa la figura";
    [SerializeField] private string choosePhaseMessage = "Ahora elige";
    [SerializeField] private string transitionMessage = "Preparate...";

    [Header("Feedback Visual")]
    [SerializeField] private Color correctColor = new Color(0.45f, 0.9f, 0.45f, 1f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float correctScaleMultiplier = 1.12f;
    [SerializeField] private float correctScaleDuration = 0.2f;

    private readonly List<FigureData> availableFigures = new List<FigureData>();
    private readonly List<FigureData> currentOptions = new List<FigureData>();

    private FigureData currentFigure;
    private float roundStartTime;
    private int score;
    private int lives;
    private bool answeringEnabled;
    private bool correctOptionIncluded;
    private Coroutine nextRoundRoutine;
    private Coroutine correctPulseRoutine;
    private Color[] originalOptionColors;
    private Color originalNoneButtonColor = Color.white;
    private Vector3[] originalOptionScales;
    private Vector3 originalNoneButtonScale = Vector3.one;

    private void Start()
    {
        ResolveSceneReferences();

        if (!ValidateReferences())
            return;

        CacheOriginalColors();

        BuildFigureList();

        score = 0;
        lives = maxLives;
        UpdateHud();

        if (availableFigures.Count < 3)
        {
            textFind.text = "Faltan figuras musicales";
            textQuestion.text = "Agrega al menos 3 imagenes de figuras para jugar.";
            textDescription.text = "Ya encontre blanca, corchea y semicorchea. Cuando agregues redonda y negra, apareceran automaticamente.";
            SetOptionsInteractable(false);
            return;
        }

        StartRound();
    }

    private void BuildFigureList()
    {
        availableFigures.Clear();
        AddFigure("redonda", "La redonda dura 4 tiempos y se representa con una figura sin plica.", redondaSprite);
        AddFigure("blanca", "La blanca dura 2 tiempos y tiene cabeza vacia con plica.", blancaSprite);
        AddFigure("negra", "La negra dura 1 tiempo y tiene cabeza rellena con plica.", negraSprite);
        AddFigure("corchea", "La corchea dura medio tiempo y lleva una bandera o se agrupa en barras.", corcheaSprite);
        AddFigure("semicorchea", "La semicorchea dura un cuarto de tiempo y lleva dos banderas o dos barras.", semicorcheaSprite);
    }

    private void AddFigure(string displayName, string description, Sprite sprite)
    {
        if (sprite == null)
            return;

        availableFigures.Add(new FigureData
        {
            displayName = displayName,
            description = description,
            sprite = sprite
        });
    }

    private void StartRound()
    {
        if (availableFigures.Count < 3)
            return;

        currentFigure = availableFigures[Random.Range(0, availableFigures.Count)];
        currentOptions.Clear();

        List<FigureData> wrongOptions = new List<FigureData>(availableFigures);
        wrongOptions.Remove(currentFigure);

        correctOptionIncluded = noneOptionButton == null || Random.value > missingCorrectChance;

        if (correctOptionIncluded)
            currentOptions.Add(currentFigure);

        while (currentOptions.Count < 3 && wrongOptions.Count > 0)
        {
            int index = Random.Range(0, wrongOptions.Count);
            currentOptions.Add(wrongOptions[index]);
            wrongOptions.RemoveAt(index);
        }

        Shuffle(currentOptions);

        HideQuestionPhase();

        textFind.text = observePhaseMessage;
        textDescription.text = currentFigure.description;

        if (figureImage != null)
        {
            figureImage.sprite = currentFigure.sprite;
            figureImage.gameObject.SetActive(true);
        }

        if (textDescription != null)
            textDescription.gameObject.SetActive(true);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int optionIndex = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));

            if (i < currentOptions.Count && optionImages[i] != null)
            {
                optionImages[i].sprite = currentOptions[i].sprite;
                optionImages[i].preserveAspect = true;
            }
        }

        if (noneOptionButton != null)
        {
            noneOptionButton.onClick.RemoveAllListeners();
            noneOptionButton.onClick.AddListener(OnNoneOptionSelected);
            noneOptionButton.gameObject.SetActive(false);
        }

        ResetOptionColors();
        ResetOptionScales();

        answeringEnabled = false;
        SetOptionButtonsVisible(false);
        SetOptionsInteractable(false);

        if (nextRoundRoutine != null)
            StopCoroutine(nextRoundRoutine);
        nextRoundRoutine = StartCoroutine(ShowQuestionPhaseAfterPreview());
    }

    private IEnumerator ShowQuestionPhaseAfterPreview()
    {
        yield return new WaitForSeconds(previewSeconds);

        textFind.text = transitionMessage;
        HideObservePhase();

        yield return new WaitForSeconds(phaseTransitionSeconds);

        if (textQuestion != null)
        {
            textQuestion.gameObject.SetActive(true);
            textQuestion.text = $"¿Cual de estas figuras es la {currentFigure.displayName}?";
            if (noneOptionButton != null)
                textQuestion.text += " Si no aparece, toca 'No esta'.";
        }

        textFind.text = choosePhaseMessage;
        SetOptionButtonsVisible(true);
        SetOptionsInteractable(true);

        if (noneOptionButton != null)
            noneOptionButton.gameObject.SetActive(true);

        answeringEnabled = true;
        roundStartTime = Time.time;
        nextRoundRoutine = null;
    }

    private void OnOptionSelected(int index)
    {
        if (!answeringEnabled || index < 0 || index >= currentOptions.Count)
            return;

        answeringEnabled = false;
        SetOptionsInteractable(false);
        SetOptionButtonsVisible(true);

        if (noneOptionButton != null)
            noneOptionButton.interactable = false;

        bool isCorrect = correctOptionIncluded && currentOptions[index] == currentFigure;
        ApplyOptionFeedback(index, selectedNone: false);
        ResolveAnswer(isCorrect, isCorrect ? string.Empty : $"Incorrecto. Era la {currentFigure.displayName}");
    }

    private void OnNoneOptionSelected()
    {
        if (!answeringEnabled)
            return;

        answeringEnabled = false;
        SetOptionsInteractable(false);

        if (noneOptionButton != null)
            noneOptionButton.interactable = false;

        bool isCorrect = !correctOptionIncluded;
        ApplyOptionFeedback(-1, selectedNone: true);
        ResolveAnswer(isCorrect, isCorrect ? string.Empty : $"Si estaba. Era la {currentFigure.displayName}");
    }

    private void ResolveAnswer(bool isCorrect, string wrongMessage)
    {
        if (isCorrect)
        {
            float answerTime = Time.time - roundStartTime;
            int awardedPoints = answerTime <= fastAnswerSeconds ? fastPoints : normalPoints;
            score += awardedPoints;
            PlaySfx(correctClip);
            textFind.text = awardedPoints == fastPoints
                ? $"Correcto! +{awardedPoints} por responder rapido"
                : $"Correcto! +{awardedPoints} puntos";
        }
        else
        {
            lives--;
            PlaySfx(wrongClip);
            textFind.text = wrongMessage;
        }

        UpdateHud();

        if (lives <= 0)
        {
            ShowResultPhase();
            ShowGameOver();
            return;
        }

        if (nextRoundRoutine != null)
            StopCoroutine(nextRoundRoutine);
        nextRoundRoutine = StartCoroutine(StartNextRoundAfterDelay());
    }

    private IEnumerator StartNextRoundAfterDelay()
    {
        yield return new WaitForSeconds(nextRoundDelay);

        HideQuestionPhase();
        HideObservePhase();

        textFind.text = string.Empty;

        yield return null;
        yield return new WaitForSeconds(hideBeforeObserveSeconds);

        textFind.text = transitionMessage;

        yield return new WaitForSeconds(phaseTransitionSeconds);

        StartRound();
        nextRoundRoutine = null;
    }

    private void ShowGameOver()
    {
        if (figureImage != null)
        {
            figureImage.sprite = currentFigure != null ? currentFigure.sprite : null;
            figureImage.gameObject.SetActive(true);
        }

        if (textDescription != null)
            textDescription.gameObject.SetActive(true);

        textQuestion.text = $"Juego terminado. Puntaje final: {score}";
        textDescription.text = "Recarga la escena o vuelve a entrar para jugar otra vez.";
        SetOptionsInteractable(false);

        if (noneOptionButton != null)
        {
            noneOptionButton.interactable = false;
            noneOptionButton.gameObject.SetActive(false);
        }
    }

    private void UpdateHud()
    {
        if (pointText != null)
            pointText.text = $"Puntos\n{score}";

        if (livesText != null)
            livesText.text = $"{lives}";
    }

    private void SetOptionsInteractable(bool value)
    {
        if (optionButtons == null)
            return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
                optionButtons[i].interactable = value;
        }

        if (noneOptionButton != null)
            noneOptionButton.interactable = value;
    }

    private void SetOptionButtonsVisible(bool value)
    {
        if (optionButtons == null)
            return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
                optionButtons[i].gameObject.SetActive(value);
        }
    }

    private void HideObservePhase()
    {
        if (figureImage != null)
            figureImage.gameObject.SetActive(false);

        if (textDescription != null)
            textDescription.gameObject.SetActive(false);
    }

    private void HideQuestionPhase()
    {
        SetOptionButtonsVisible(false);

        if (textQuestion != null)
        {
            textQuestion.gameObject.SetActive(false);
            textQuestion.text = string.Empty;
        }

        if (noneOptionButton != null)
            noneOptionButton.gameObject.SetActive(false);
    }

    private void ShowResultPhase()
    {
        if (figureImage != null)
        {
            figureImage.sprite = currentFigure.sprite;
            figureImage.gameObject.SetActive(true);
        }

        if (textDescription != null)
            textDescription.gameObject.SetActive(true);

        if (textQuestion != null)
            textQuestion.gameObject.SetActive(true);

        if (noneOptionButton != null)
            noneOptionButton.gameObject.SetActive(true);
    }

    private void CacheOriginalColors()
    {
        originalOptionColors = new Color[optionButtons.Length];
        originalOptionScales = new Vector3[optionButtons.Length];

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null && optionButtons[i].image != null)
                originalOptionColors[i] = optionButtons[i].image.color;
            else
                originalOptionColors[i] = Color.white;

            if (optionButtons[i] != null)
                originalOptionScales[i] = optionButtons[i].transform.localScale;
            else
                originalOptionScales[i] = Vector3.one;
        }

        if (noneOptionButton != null && noneOptionButton.image != null)
            originalNoneButtonColor = noneOptionButton.image.color;

        if (noneOptionButton != null)
            originalNoneButtonScale = noneOptionButton.transform.localScale;
    }

    private void ResetOptionColors()
    {
        if (optionButtons != null && originalOptionColors != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null && optionButtons[i].image != null && i < originalOptionColors.Length)
                    optionButtons[i].image.color = originalOptionColors[i];
            }
        }

        if (noneOptionButton != null && noneOptionButton.image != null)
            noneOptionButton.image.color = originalNoneButtonColor;
    }

    private void ResetOptionScales()
    {
        if (correctPulseRoutine != null)
        {
            StopCoroutine(correctPulseRoutine);
            correctPulseRoutine = null;
        }

        if (optionButtons != null && originalOptionScales != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null && i < originalOptionScales.Length)
                    optionButtons[i].transform.localScale = originalOptionScales[i];
            }
        }

        if (noneOptionButton != null)
            noneOptionButton.transform.localScale = originalNoneButtonScale;
    }

    private void ApplyOptionFeedback(int selectedIndex, bool selectedNone)
    {
        ResetOptionColors();
        ResetOptionScales();

        int correctIndex = -1;
        if (correctOptionIncluded)
        {
            for (int i = 0; i < currentOptions.Count; i++)
            {
                if (currentOptions[i] == currentFigure)
                {
                    correctIndex = i;
                    break;
                }
            }
        }

        if (correctIndex >= 0 && correctIndex < optionButtons.Length)
            SetButtonColor(optionButtons[correctIndex], correctColor);

        if (selectedNone && noneOptionButton != null)
        {
            SetButtonColor(noneOptionButton, correctOptionIncluded ? wrongColor : correctColor);
        }
        else if (selectedIndex >= 0 && selectedIndex < optionButtons.Length)
        {
            if (selectedIndex != correctIndex)
                SetButtonColor(optionButtons[selectedIndex], wrongColor);
        }

        if (!correctOptionIncluded && noneOptionButton != null)
            SetButtonColor(noneOptionButton, selectedNone ? correctColor : originalNoneButtonColor);

        Button correctButton = null;
        if (correctOptionIncluded && correctIndex >= 0 && correctIndex < optionButtons.Length)
            correctButton = optionButtons[correctIndex];
        else if (!correctOptionIncluded)
            correctButton = noneOptionButton;

        if (correctButton != null)
            correctPulseRoutine = StartCoroutine(PulseCorrectButton(correctButton));
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.image != null)
            button.image.color = color;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private IEnumerator PulseCorrectButton(Button button)
    {
        if (button == null)
            yield break;

        Transform target = button.transform;
        Vector3 baseScale = target.localScale;
        Vector3 targetScale = baseScale * correctScaleMultiplier;

        float elapsed = 0f;
        while (elapsed < correctScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / correctScaleDuration);
            target.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < correctScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / correctScaleDuration);
            target.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }

        target.localScale = baseScale;
        correctPulseRoutine = null;
    }

    private void Shuffle(List<FigureData> figures)
    {
        for (int i = figures.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            FigureData temp = figures[i];
            figures[i] = figures[swapIndex];
            figures[swapIndex] = temp;
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (textFind == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar TextFind.");
            valid = false;
        }

        if (figureImage == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar FigureImage.");
            valid = false;
        }

        if (textDescription == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar TextDescription.");
            valid = false;
        }

        if (textQuestion == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar TextQuestion.");
            valid = false;
        }

        if (pointText == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar PointText.");
            valid = false;
        }

        if (livesText == null)
        {
            Debug.LogError("MemoryFigureGameManager: Falta asignar LivesText.");
            valid = false;
        }

        if (optionButtons == null || optionButtons.Length != 3)
        {
            Debug.LogError("MemoryFigureGameManager: Debes asignar 3 botones en optionButtons.");
            valid = false;
        }

        if (optionImages == null || optionImages.Length != 3)
        {
            Debug.LogError("MemoryFigureGameManager: Debes asignar 3 imagenes en optionImages.");
            valid = false;
        }

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] == null)
                {
                    Debug.LogError($"MemoryFigureGameManager: Falta asignar optionButtons[{i}].");
                    valid = false;
                }
            }
        }

        if (optionImages != null)
        {
            for (int i = 0; i < optionImages.Length; i++)
            {
                if (optionImages[i] == null)
                {
                    Debug.LogError($"MemoryFigureGameManager: Falta asignar optionImages[{i}].");
                    valid = false;
                }
            }
        }

        return valid;
    }

    private void ResolveSceneReferences()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            return;

        if (textFind == null)
            textFind = FindText(canvas, "TextFind") ?? FindText(canvas, "TextAtuRitmo");

        if (figureImage == null)
            figureImage = FindImage(canvas, "FigureImage");

        if (textDescription == null)
            textDescription = FindText(canvas, "TextDescription");

        if (textQuestion == null)
            textQuestion = FindText(canvas, "TextQuestion");

        if (pointText == null)
            pointText = FindText(canvas, "PointText");

        if (livesText == null)
            livesText = FindText(canvas, "LivesText");

        if (optionButtons == null || optionButtons.Length != 3)
            optionButtons = new Button[3];

        if (optionImages == null || optionImages.Length != 3)
            optionImages = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            int number = i + 1;

            if (optionButtons[i] == null)
                optionButtons[i] = FindButton(canvas, $"Figure{number}");

            if (optionImages[i] == null && optionButtons[i] != null)
                optionImages[i] = FindButtonImage(optionButtons[i]);
        }

        if (noneOptionButton == null)
            noneOptionButton = FindButton(canvas, "NoEstaButton") ?? FindButton(canvas, "NoneOptionButton");

        if (sfxSource == null)
            sfxSource = FindObjectOfType<AudioSource>();
    }

    private TMP_Text FindText(Canvas canvas, string objectName)
    {
        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == objectName)
                return texts[i];
        }

        return null;
    }

    private Image FindImage(Canvas canvas, string objectName)
    {
        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].name == objectName)
                return images[i];
        }

        return null;
    }

    private Button FindButton(Canvas canvas, string objectName)
    {
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName)
                return buttons[i];
        }

        return null;
    }

    private Image FindButtonImage(Button button)
    {
        Image[] images = button.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].gameObject != button.gameObject)
                return images[i];
        }

        return button.GetComponent<Image>();
    }
}
