using UnityEngine;
using UnityEngine.UI;

public class BeatGuideUI : MonoBehaviour
{
    public BeatController beat;
    public Image guideFill; // Image tipo Filled radial

    void Update()
    {
        if (beat == null || guideFill == null) return;

        double now = AudioSettings.dspTime;
        double next = beat.NextBeatDspTime;
        double interval = beat.IntervalSec;

        // Progreso hacia el próximo beat: 0..1
        double t = 1.0 - Mathf.Clamp01((float)((next - now) / interval));

        guideFill.fillAmount = (float)t;
    }
}