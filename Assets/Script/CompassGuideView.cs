using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompassGuideView : MonoBehaviour
{
    [SerializeField] private Image[] beatSlots;
    [SerializeField] private TMP_Text[] beatTexts;
    [SerializeField] private TMP_Text measureText;
    [SerializeField] [Range(0f, 1f)] private float minimumSlotAlpha = 0.55f;

    private Sprite[] defaultSprites;

    private void Awake()
    {
        CacheDefaults();
    }

    public void InitializeIfNeeded(int beatsPerMeasure)
    {
        if ((beatSlots == null || beatSlots.Length == 0) && transform.childCount > 0)
        {
            beatSlots = new Image[beatsPerMeasure];
            beatTexts = new TMP_Text[beatsPerMeasure];
            for (int i = 0; i < beatsPerMeasure; i++)
            {
                Transform beatTransform = transform.Find("Beat_" + (i + 1));
                if (beatTransform == null)
                    beatTransform = transform.Find("Time_" + (i + 1));

                if (beatTransform != null)
                {
                    beatSlots[i] = beatTransform.GetComponent<Image>();
                    beatTexts[i] = beatTransform.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (measureText == null)
                measureText = transform.Find("MeasureText")?.GetComponent<TMP_Text>();
        }

        CacheDefaults();
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void RefreshMeasure(CompassBeatDisplay[] steps, int measureNumber, int totalMeasures, Color idleColor, Color silenceColor, Sprite silenceSprite)
    {
        if (beatSlots == null || beatTexts == null)
            return;

        if (measureText != null)
            measureText.text = $"Compas {measureNumber}/{totalMeasures}";

        for (int i = 0; i < beatSlots.Length && i < beatTexts.Length; i++)
        {
            CompassBeatDisplay step = i < steps.Length ? steps[i] : null;
            if (beatSlots[i] != null)
            {
                beatSlots[i].sprite = step != null && step.isSilence && silenceSprite != null ? silenceSprite : defaultSprites[i];
                beatSlots[i].preserveAspect = step != null && step.isSilence && silenceSprite != null;
                beatSlots[i].color = step != null && step.isSilence ? EnsureReadableAlpha(silenceColor) : WithReadableAlpha(step != null ? step.color : Color.white, idleColor.a);
            }

            if (beatTexts[i] != null)
            {
                beatTexts[i].text = step == null ? "-" : step.isSilence ? string.Empty : step.label;
                beatTexts[i].color = step != null && !step.isSilence ? step.color : Color.white;
            }
        }
    }

    public void UpdateBeatState(int beatIndex, Color color, CompassBeatDisplay step)
    {
        if (beatSlots == null || beatIndex < 0 || beatIndex >= beatSlots.Length || beatSlots[beatIndex] == null)
            return;

        beatSlots[beatIndex].color = step != null && !step.isSilence ? WithReadableAlpha(step.color, color.a) : EnsureReadableAlpha(color);
    }

    private void CacheDefaults()
    {
        if (beatSlots == null)
            return;

        defaultSprites = new Sprite[beatSlots.Length];
        for (int i = 0; i < beatSlots.Length; i++)
            defaultSprites[i] = beatSlots[i] != null ? beatSlots[i].sprite : null;
    }

    private Color WithReadableAlpha(Color color, float alpha)
    {
        color.a = Mathf.Max(minimumSlotAlpha, alpha);
        return color;
    }

    private Color EnsureReadableAlpha(Color color)
    {
        color.a = Mathf.Max(minimumSlotAlpha, color.a);
        return color;
    }
}

public class CompassBeatDisplay
{
    public bool isSilence;
    public string label;
    public Color color;
}
