using UnityEngine;
using UnityEngine.UI;

public class SimonMetronomeController : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image metronomeImage;
    [SerializeField] private RectTransform pendulum;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float swingAngle = 28f;
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color pulseColor = new Color(1f, 0.9f, 0.35f, 1f);

    [Header("Audio Opcional")]
    [SerializeField] private bool useMetronomeClick;
    [SerializeField] private AudioSource metronomeAudioSource;
    [SerializeField] private AudioClip metronomeClickClip;
    [SerializeField] [Range(0f, 1f)] private float metronomeClickVolume = 0.2f;

    private float bpm = 60f;
    private bool isRunning;
    private bool pulseTriggered;
    private float elapsedInBeat;
    private Vector3 baseScale = Vector3.one;
    private int currentFrameIndex = -1;
    private float baseClickVolume;

    private void Awake()
    {
        if (pendulum != null)
            baseScale = pendulum.localScale;

        if (metronomeAudioSource != null)
        {
            metronomeAudioSource.playOnAwake = false;
            baseClickVolume = metronomeClickVolume;
        }

        GameSettings.SettingsChanged += ApplyGameSettings;
        ApplyGameSettings();

        SetIdleState();
    }

    private void Update()
    {
        if (!isRunning || bpm <= 0f)
            return;

        float beatDuration = 60f / bpm;
        if (beatDuration <= 0f)
            return;

        elapsedInBeat += Time.deltaTime;
        if (elapsedInBeat >= beatDuration)
        {
            elapsedInBeat -= beatDuration;
            pulseTriggered = false;
        }

        float normalizedTime = Mathf.Clamp01(elapsedInBeat / beatDuration);
        float swing = Mathf.Sin(normalizedTime * Mathf.PI * 2f - Mathf.PI * 0.5f);

        if (HasFrameAnimation())
        {
            UpdateFrameAnimation(normalizedTime);
        }
        else if (pendulum != null)
        {
            pendulum.localRotation = Quaternion.Euler(0f, 0f, swing * swingAngle);

            float pulseAmount = Mathf.Clamp01(1f - normalizedTime * 4f);
            pendulum.localScale = Vector3.Lerp(baseScale, baseScale * pulseScale, pulseAmount);
        }

        if (!pulseTriggered && normalizedTime <= 0.08f)
        {
            pulseTriggered = true;
            ApplyPulseFeedback();
        }

        if (metronomeImage != null && normalizedTime > 0.12f)
            metronomeImage.color = idleColor;
    }

    private void OnDestroy()
    {
        GameSettings.SettingsChanged -= ApplyGameSettings;
    }

    public void SetBpm(float newBpm)
    {
        bpm = Mathf.Max(1f, newBpm);
    }

    public void StartMetronome(float newBpm)
    {
        SetBpm(newBpm);
        elapsedInBeat = 0f;
        pulseTriggered = false;
        isRunning = true;
        currentFrameIndex = -1;
        ApplyPulseFeedback();
    }

    public void StopMetronome()
    {
        isRunning = false;
        elapsedInBeat = 0f;
        pulseTriggered = false;
        currentFrameIndex = -1;
        SetIdleState();
    }

    public void PauseMetronome()
    {
        isRunning = false;
    }

    public void ResumeMetronome()
    {
        if (bpm > 0f)
            isRunning = true;
    }

    private void ApplyPulseFeedback()
    {
        if (metronomeImage != null)
            metronomeImage.color = pulseColor;

        if (useMetronomeClick && metronomeAudioSource != null && metronomeClickClip != null)
        {
            metronomeAudioSource.volume = metronomeClickVolume;
            metronomeAudioSource.PlayOneShot(metronomeClickClip);
        }
    }

    private void SetIdleState()
    {
        if (HasFrameAnimation())
        {
            SetFrame(0);
        }
        else if (pendulum != null)
        {
            pendulum.localRotation = Quaternion.identity;
            pendulum.localScale = baseScale;
        }

        if (metronomeImage != null)
            metronomeImage.color = idleColor;
    }

    private bool HasFrameAnimation()
    {
        return metronomeImage != null && animationFrames != null && animationFrames.Length > 0;
    }

    private void UpdateFrameAnimation(float normalizedTime)
    {
        if (!HasFrameAnimation())
            return;

        int frameCount = animationFrames.Length;
        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * frameCount), 0, frameCount - 1);
        SetFrame(frameIndex);
    }

    private void SetFrame(int frameIndex)
    {
        if (!HasFrameAnimation())
            return;

        frameIndex = Mathf.Clamp(frameIndex, 0, animationFrames.Length - 1);
        if (currentFrameIndex == frameIndex)
            return;

        currentFrameIndex = frameIndex;
        metronomeImage.sprite = animationFrames[frameIndex];
        metronomeImage.preserveAspect = true;
    }

    private void ApplyGameSettings()
    {
        metronomeClickVolume = baseClickVolume * GameSettings.EffectsVolume;
    }
}
