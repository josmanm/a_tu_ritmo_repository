using UnityEngine;
using System;

public class BeatController : MonoBehaviour
{
    [Header("Tempo")]
    [Range(40, 160)] public float bpm = 80f;
    public bool startOnPlay = true;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip metronomeClick;

    public event Action<double> OnBeat;

    public double NextBeatDspTime => nextBeatTime;
    public double IntervalSec => interval;
    public double LastBeatDspTime { get; private set; }

    double interval;
    double nextBeatTime;
    bool running;

    void Start()
    {
        interval = 60.0 / bpm;

        if (startOnPlay)
            StartBeats();
    }

    public void StartBeats()
    {
        interval = 60.0 / bpm;
        running = true;

        nextBeatTime = AudioSettings.dspTime + 0.2;
        LastBeatDspTime = nextBeatTime - interval;
    }

    public void StopBeats()
    {
        running = false;
    }

    void Update()
    {
        if (!running) return;

        double dsp = AudioSettings.dspTime;

        if (dsp >= nextBeatTime)
        {
            LastBeatDspTime = nextBeatTime;

            if (audioSource && metronomeClick)
                audioSource.PlayOneShot(metronomeClick);

            OnBeat?.Invoke(LastBeatDspTime);

            nextBeatTime += interval;
        }
    }
}