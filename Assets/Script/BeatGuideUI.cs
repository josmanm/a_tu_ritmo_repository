using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeatGuideUI : MonoBehaviour
{
    public BeatController beat;
    public Image guideFill; // Image tipo Filled radial
    public RadialBeatIndicatorGraphic proceduralGuide;
    public TempoTapGameManager game;
    [Range(0f, 1f)] public float idleFillAmount = 0f;

    float lastProgress;
    bool wasRunning;
    TMP_Text actionLabel;

    void Start()
    {
        if (game == null)
            game = FindFirstObjectByType<TempoTapGameManager>();
        Transform background = transform.Find("ActionBackground");
        if (background == null)
        {
            GameObject surface = new GameObject("ActionBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = surface.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(168f, 92f);
            surface.GetComponent<Image>().color = new Color(0.04f, 0.15f, 0.34f, 0.94f);
            surface.GetComponent<Image>().raycastTarget = false;
            rect.SetAsFirstSibling();
        }
        Transform existing = transform.Find("ActionLabel");
        if (existing == null)
        {
            GameObject label = new GameObject("ActionLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.sizeDelta = new Vector2(164f, 80f);
            existing = rect;
        }
        actionLabel = existing.GetComponent<TMP_Text>();
        actionLabel.fontSize = 29f;
        actionLabel.fontStyle = FontStyles.Bold;
        actionLabel.alignment = TextAlignmentOptions.Center;
        actionLabel.color = new Color(1f, 0.94f, 0.82f);
        actionLabel.raycastTarget = false;
        actionLabel.text = "ESCUCHA";
        SetFill(idleFillAmount);
        lastProgress = idleFillAmount;
    }

    void Update()
    {
        if (beat == null || (guideFill == null && proceduralGuide == null))
            return;

        if (!beat.IsRunning || beat.IntervalSec <= 0.0)
        {
            if (actionLabel != null) actionLabel.text = "ESCUCHA";
            SetFill(idleFillAmount);
            lastProgress = idleFillAmount;
            wasRunning = false;
            return;
        }

        if (proceduralGuide != null && game != null)
            proceduralGuide.SetTimingWindows(game.CurrentPerfectMs, game.CurrentAssistMs, beat.IntervalSec);

        if (!wasRunning)
        {
            lastProgress = 0f;
            wasRunning = true;
        }

        double now = AudioSettings.dspTime;
        double next = beat.NextBeatDspTime;
        if (actionLabel != null && game != null)
        {
            string prompt = game.GetPromptForBeat(next);
            if (actionLabel.text != prompt) actionLabel.text = prompt;
        }
        double previous = next - beat.IntervalSec;

        if (now <= previous)
        {
            SetFill(0f);
            return;
        }

        double progress = (now - previous) / beat.IntervalSec;
        float normalizedProgress = Mathf.Clamp01((float)progress);

        // Cuando el ciclo reinicia tras completar un beat, hacemos un flash breve.
        if (lastProgress > 0.95f && normalizedProgress < 0.2f && proceduralGuide != null)
            proceduralGuide.TriggerFlash();

        SetFill(normalizedProgress);
        lastProgress = normalizedProgress;
    }

    void SetFill(float amount)
    {
        if (guideFill != null)
            guideFill.fillAmount = amount;

        if (proceduralGuide != null)
            proceduralGuide.SetProgress(amount);
    }
}
