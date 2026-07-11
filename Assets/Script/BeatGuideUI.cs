using UnityEngine;
using UnityEngine.UI;

public class BeatGuideUI : MonoBehaviour
{
    public BeatController beat;
    public Image guideFill; // Image tipo Filled radial
    public RadialBeatIndicatorGraphic proceduralGuide;
    [Range(0f, 1f)] public float idleFillAmount = 0f;

    float lastProgress;
    bool wasRunning;

    void Start()
    {
        SetFill(idleFillAmount);
        lastProgress = idleFillAmount;
    }

    void Update()
    {
        if (beat == null || (guideFill == null && proceduralGuide == null))
            return;

        if (!beat.IsRunning || beat.IntervalSec <= 0.0)
        {
            SetFill(idleFillAmount);
            lastProgress = idleFillAmount;
            wasRunning = false;
            return;
        }

        if (!wasRunning)
        {
            lastProgress = 0f;
            wasRunning = true;
        }

        double now = AudioSettings.dspTime;
        double next = beat.NextBeatDspTime;
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
