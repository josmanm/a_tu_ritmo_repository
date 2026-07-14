using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompassGuideView : MonoBehaviour
{
    [SerializeField] private Image[] beatSlots;
    [SerializeField] private TMP_Text[] beatTexts;
    [SerializeField] private TMP_Text measureText;
    [SerializeField] [Range(0f, 1f)] private float minimumSlotAlpha = 0.68f;
    [SerializeField] private float activeBeatScale = 1.14f;
    [SerializeField] private float pulseSpeed = 10f;
    [SerializeField] private float ringSize = 74f;
    [SerializeField] private float beatSpacing = 132f;
    [SerializeField] private float silenceSquareSize = 42f;
    [SerializeField] private float glowFadeSpeed = 8f;

    private Sprite[] defaultSprites;
    private CompassBeatRingGraphic[] ringSlots;
    private Vector3[] baseScales;
    private float[] pulseTargets;
    private float[] glowTargets;

    private void Awake()
    {
        CacheDefaults();
        EnsureRingSlots();
        ArrangeBeatSlots();
        DisableDecorativeRaycasts();
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
        EnsureRingSlots();
        ArrangeBeatSlots();
        DisableDecorativeRaycasts();
    }

    private void Update()
    {
        if (beatSlots == null || baseScales == null || pulseTargets == null || glowTargets == null)
            return;

        for (int i = 0; i < beatSlots.Length; i++)
        {
            if (beatSlots[i] == null)
                continue;

            float current = beatSlots[i].rectTransform.localScale.x;
            float next = Mathf.Lerp(current, pulseTargets[i], Time.unscaledDeltaTime * pulseSpeed);
            beatSlots[i].rectTransform.localScale = baseScales[i] * next;
            pulseTargets[i] = Mathf.Lerp(pulseTargets[i], 1f, Time.unscaledDeltaTime * pulseSpeed);

            if (ringSlots != null && i < ringSlots.Length && ringSlots[i] != null)
            {
                glowTargets[i] = Mathf.Lerp(glowTargets[i], 0f, Time.unscaledDeltaTime * glowFadeSpeed);
                ringSlots[i].SetGlow(glowTargets[i]);
            }
        }
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
                beatSlots[i].enabled = step != null && step.isSilence && silenceSprite != null;
                beatSlots[i].color = new Color(1f, 1f, 1f, step != null && step.isSilence && silenceSprite != null ? 1f : 0f);
            }

            if (ringSlots != null && i < ringSlots.Length && ringSlots[i] != null)
            {
                Color ringColor = step != null && step.isSilence ? EnsureReadableAlpha(silenceColor) : WithReadableAlpha(step != null ? step.color : idleColor, idleColor.a);
                ringSlots[i].SetColor(ringColor);
                pulseTargets[i] = 1f;
                glowTargets[i] = 0f;
                ringSlots[i].gameObject.SetActive(!(step != null && step.isSilence && silenceSprite != null));
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
        if (beatSlots == null || beatIndex < 0 || beatIndex >= beatSlots.Length)
            return;

        if (ringSlots != null && beatIndex < ringSlots.Length && ringSlots[beatIndex] != null)
        {
            ringSlots[beatIndex].SetColor(step != null && !step.isSilence ? WithReadableAlpha(step.color, color.a) : EnsureReadableAlpha(color));
            pulseTargets[beatIndex] = activeBeatScale;
            glowTargets[beatIndex] = 1f;
        }

        if (beatSlots[beatIndex] != null && step != null && step.isSilence)
            beatSlots[beatIndex].color = EnsureReadableAlpha(color);
    }

    private void CacheDefaults()
    {
        if (beatSlots == null)
            return;

        defaultSprites = new Sprite[beatSlots.Length];
        baseScales = new Vector3[beatSlots.Length];
        pulseTargets = new float[beatSlots.Length];
        glowTargets = new float[beatSlots.Length];
        for (int i = 0; i < beatSlots.Length; i++)
        {
            defaultSprites[i] = beatSlots[i] != null ? beatSlots[i].sprite : null;
            baseScales[i] = beatSlots[i] != null ? beatSlots[i].rectTransform.localScale : Vector3.one;
            pulseTargets[i] = 1f;
            glowTargets[i] = 0f;
        }
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

    private void DisableDecorativeRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void EnsureRingSlots()
    {
        if (beatSlots == null)
            return;

        ringSlots = new CompassBeatRingGraphic[beatSlots.Length];
        for (int i = 0; i < beatSlots.Length; i++)
        {
            if (beatSlots[i] == null)
                continue;

            Transform ringTransform = beatSlots[i].transform.Find("Ring");
            CompassBeatRingGraphic ring = ringTransform != null ? ringTransform.GetComponent<CompassBeatRingGraphic>() : null;
            if (ring == null)
            {
                GameObject ringObject = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(CompassBeatRingGraphic));
                RectTransform ringRect = ringObject.GetComponent<RectTransform>();
                ringRect.SetParent(beatSlots[i].transform, false);
                ringRect.anchorMin = Vector2.zero;
                ringRect.anchorMax = Vector2.one;
                ringRect.offsetMin = Vector2.zero;
                ringRect.offsetMax = Vector2.zero;
                ring = ringObject.GetComponent<CompassBeatRingGraphic>();
            }

            beatSlots[i].raycastTarget = false;
            beatSlots[i].enabled = false;
            ringSlots[i] = ring;
        }
    }

    private void ArrangeBeatSlots()
    {
        if (beatSlots == null || beatSlots.Length == 0)
            return;

        float totalWidth = (beatSlots.Length - 1) * beatSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < beatSlots.Length; i++)
        {
            if (beatSlots[i] == null)
                continue;

            RectTransform beatRect = beatSlots[i].rectTransform;
            beatRect.anchorMin = new Vector2(0.5f, 0.5f);
            beatRect.anchorMax = new Vector2(0.5f, 0.5f);
            beatRect.pivot = new Vector2(0.5f, 0.5f);
            beatRect.anchoredPosition = new Vector2(startX + (i * beatSpacing), 0f);
            beatRect.sizeDelta = new Vector2(ringSize, ringSize);

            if (beatTexts != null && i < beatTexts.Length && beatTexts[i] != null)
            {
                RectTransform textRect = beatTexts[i].rectTransform;
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(ringSize + 12f, ringSize + 12f);
            }

        }

        if (measureText != null)
        {
            RectTransform measureRect = measureText.rectTransform;
            measureRect.anchorMin = new Vector2(0.5f, 0f);
            measureRect.anchorMax = new Vector2(0.5f, 0f);
            measureRect.pivot = new Vector2(0.5f, 0.5f);
            measureRect.anchoredPosition = new Vector2(0f, -44f);
        }
    }
}

public class CompassBeatDisplay
{
    public bool isSilence;
    public string label;
    public Color color;
}
