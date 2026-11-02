using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class BandaLevelConfiguration
{
    public string label = "Nivel 1";
    [Tooltip("Tempo for this level's WAV loops; used as a fallback without clips.")]
    [Range(40, 160)] public int bpm = 80;
    [Min(4)] public int beats = 4;
    [Header("Audio del nivel (primera vuelta guiada, luego fondo en loop)")]
    public AudioClip guidedLoop;
    public AudioClip backgroundLoop;
    [Min(1)] public int beatsPerLoop = 16;
    [Header("Retos a los 15, 30 y 45 segundos")]
    [Min(1f)] public float levelDurationSeconds = 60f;
    [Min(0f)] public float challengeDurationSeconds = 5f;
    [Range(0f, 1f)] public float firstChallengeMusicFraction = 0.6f;
    [Range(0.08f, 0.4f)] public float toleranceSeconds = 0.25f;
    public bool visualHelp = true;
    [Range(0.25f, 1f)] public float successFraction = 0.5f;
}

public class BandaMusicalGameManager : MonoBehaviour
{
    public RhythmManager rhythm;
    public BandaSessionRecorder recorder;
    public DrumInputController drum;
    public MusicianController monkey;
    public MusicianController cat;
    public MusicianController bear;
    public TMP_Text message;
    public UnityEngine.UI.Image hand;
    public UnityEngine.UI.Image star;
    public UnityEngine.UI.Image ear;
    public UnityEngine.UI.Button replayButton;
    public UnityEngine.UI.Button backButton;
    public TopHudPanelView topHud;
    public AudioSource voiceSource;
    [Header("Optional narration: assign recordings of these exact phrases")]
    public AudioClip discoverVoice; // ¡Hola! ¡Toca el tambor!
    public AudioClip listenVoice;   // ¡Mira cómo lo hago!
    public AudioClip togetherVoice; // ¡Ahora toca conmigo!
    public AudioClip helpVoice;     // ¡Escuchemos otra vez!
    public AudioClip positiveSound;
    [Tooltip("Golpe breve al incorporarse un músico nuevo, entre secciones (sin tapar el loop).")]
    public AudioClip animalHit;
    [Header("Canciones: BPM e inicio del primer pulso por pista")]
    public BandaSongConfiguration[] songs = Array.Empty<BandaSongConfiguration>();
    [Tooltip("Index in Songs. 0 uses the 80 BPM drum loop; 1 uses the 120 BPM mix (provisional alignment).")]
    [Min(0)] public int selectedSongIndex;
    [Header("Niveles: sus WAV tienen prioridad sobre la canción del tutorial")]
    public BandaLevelConfiguration[] levels =
    {
        new BandaLevelConfiguration { label = "Nivel 1", bpm = 80, beats = 4, visualHelp = true },
        new BandaLevelConfiguration { label = "Nivel 2", bpm = 80, beats = 8, visualHelp = true },
        new BandaLevelConfiguration { label = "Nivel 3", bpm = 80, beats = 8, visualHelp = false }
    };
    public bool allowHelp = true;

    private bool discovering;
    private bool tappedDiscovery;
    private bool accepting;
    private bool assisted;
    private bool completed;
    private int currentLevel;
    private int sectionIndex;
    private int sectionHits;
    private Coroutine activity;
    private bool playingMainLevels;
    private int score;
    private BandaSongConfiguration selectedSong;

    private void Start()
    {
        if (rhythm == null || recorder == null || drum == null || monkey == null || cat == null || bear == null)
        {
            Debug.LogError("Banda Musical: faltan referencias en la escena.");
            return;
        }
        if (drum.hitClip == null)
            drum.hitClip = CreateSoftDrum();
        BackgroundMusicPlayer.SetBandaMusicFocus(true);
        rhythm.Beat += OnBeat;
        UnityEngine.UI.Button menu = topHud != null && topHud.MenuButton != null
            ? topHud.MenuButton : backButton;
        if (menu != null) menu.onClick.AddListener(ReturnToMenu);
        if (menu != backButton && backButton != null) backButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.onClick.AddListener(Restart);
        Restart();
    }

    private static AudioClip CreateSoftDrum()
    {
        const int sampleRate = 22050;
        float[] samples = new float[sampleRate / 5];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2f * Mathf.PI * (115f * t - 160f * t * t)) *
                         Mathf.Exp(-24f * t) * 0.65f;
        }
        AudioClip clip = AudioClip.Create("BandaSoftDrum", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void Restart()
    {
        if (activity != null) StopCoroutine(activity);
        if (rhythm != null) rhythm.StopSequence();
        if (drum != null) drum.EndBeatGuide();
        // Record an interrupted activity before starting a different one.
        if (recorder != null && sectionIndex > 0 && !completed)
            recorder.Save(Mathf.Max(1, currentLevel + 1), Mathf.RoundToInt(rhythm.bpm), false);
        currentLevel = 0;
        sectionIndex = 0;
        sectionHits = 0;
        assisted = false;
        completed = false;
        playingMainLevels = false;
        score = 0;
        UpdateHud("Tutorial");
        discovering = false;
        accepting = false;
        recorder.Begin(false);
        selectedSong = songs != null && selectedSongIndex >= 0 && selectedSongIndex < songs.Length
            ? songs[selectedSongIndex] : null;
        if (selectedSong != null && selectedSong.clip != null && selectedSong.bpm > 0f)
        {
            rhythm.music = selectedSong.clip;
            rhythm.bpm = selectedSong.bpm;
            rhythm.beatOffsetSeconds = selectedSong.firstBeatOffsetSeconds;
            recorder.Metrics.songId = selectedSong.id;
        }
        cat.gameObject.SetActive(false);
        bear.gameObject.SetActive(false);
        monkey.gameObject.SetActive(true);
        if (star != null) star.gameObject.SetActive(false);
        if (ear != null) ear.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        activity = StartCoroutine(RunActivity());
    }

    public void OnDrumTap(double dspTime)
    {
        if (discovering)
        {
            tappedDiscovery = true;
            return;
        }
        if (!accepting || !rhythm.Running) return;
        if (recorder.RecordTap(dspTime, rhythm, sectionIndex))
        {
            sectionHits++;
            if (playingMainLevels)
            {
                score += 10;
                recorder.SetScore(score);
                UpdateHud("Nivel " + (currentLevel + 1));
            }
        }
    }

    private void OnBeat(int beat, double dsp)
    {
        monkey.OnBeat(beat, dsp, rhythm.Interval);
        if (cat.gameObject.activeSelf) cat.OnBeat(beat, dsp, rhythm.Interval);
        if (bear.gameObject.activeSelf) bear.OnBeat(beat, dsp, rhythm.Interval);
        // Only the teaching demonstration uses a separate drum sound. The song
        // and its expected beats share one DSP start and do not layer a metronome.
        if (!rhythm.PlayingSong && drum.source != null && drum.hitClip != null)
            drum.source.PlayOneShot(drum.hitClip, 0.55f * GameSettings.EffectsVolume);
    }

    private void Say(string words, AudioClip voice)
    {
        if (message != null) message.text = words;
        if (voice != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = voice;
            voiceSource.volume = GameSettings.EffectsVolume;
            voiceSource.Play();
        }
    }

    private IEnumerator RunActivity()
    {
        discovering = true;
        tappedDiscovery = false;
        SetHand(true);
        Say("¡Hola! ¡Toca el tambor!", discoverVoice);
        yield return new WaitUntil(() => tappedDiscovery);
        discovering = false;
        yield return new WaitForSecondsRealtime(0.6f);

        Say("Mira: el tambor brilla con cada pulso", listenVoice);
        yield return PlaySection(4, false, false, true);
        Say("¡Toca cuando el tambor brille!", togetherVoice);
        yield return PlaySection(4, true, true, true);

        int independentTries = 0;
        do
        {
            Say("Ahora tú: toca cuando brille", null);
            yield return PlaySection(4, true, true, false);
            if (sectionHits >= 2) break;
            independentTries++;
            assisted = true;
            if (allowHelp && independentTries < 2) yield return ShowHelp();
        } while (independentTries < 2);

        for (int level = 0; level < levels.Length; level++)
        {
            currentLevel = level;
            UpdateHud("Nivel " + (level + 1));
            BandaLevelConfiguration config = levels[level];
            bool hasLevelLoops = config.guidedLoop != null && config.backgroundLoop != null;
            // Only change tempo when the corresponding music changes too.
            rhythm.bpm = hasLevelLoops ? config.bpm :
                selectedSong != null && selectedSong.clip != null ? selectedSong.bpm : config.bpm;
            rhythm.beatOffsetSeconds = hasLevelLoops ? 0f :
                selectedSong != null ? selectedSong.firstBeatOffsetSeconds : 0f;
            rhythm.music = hasLevelLoops ? config.guidedLoop :
                selectedSong != null ? selectedSong.clip : rhythm.music;
            rhythm.toleranceSeconds = config.toleranceSeconds;
            // Round to a complete beat: at 60/80/100 BPM, 60 s is exact.
            int expectedBeats = hasLevelLoops
                ? Mathf.RoundToInt(config.levelDurationSeconds * config.bpm / 60f) : config.beats;
            int tries = 0;
            do
            {
                Say(config.label + ": toca cuando brille", null);
                playingMainLevels = true;
                if (hasLevelLoops)
                    yield return PlayLoopedLevel(config, expectedBeats, config.visualHelp || assisted);
                else
                    yield return PlaySection(expectedBeats, true, true, config.visualHelp || assisted);
                playingMainLevels = false;
                recorder.AddSection(expectedBeats, sectionHits, assisted);
                // A looped level is one full minute, even if the player needs
                // more practice; do not silently repeat the entire minute.
                if (hasLevelLoops || sectionHits >= Mathf.CeilToInt(expectedBeats * config.successFraction)) break;
                tries++;
                assisted = true;
                if (allowHelp && tries < 2)
                {
                    yield return ShowHelp();
                    yield return PlaySection(4, false, false, true);
                }
            } while (tries < 2);

            bool accompanied = sectionHits >= Mathf.CeilToInt(expectedBeats * config.successFraction);
            if (star != null) star.gameObject.SetActive(accompanied);
            if (accompanied && positiveSound != null && voiceSource != null)
                voiceSource.PlayOneShot(positiveSound, GameSettings.EffectsVolume);
            Say(!accompanied ? "¡Seguimos juntos!" : level == 0 ? "¡Muy bien! ¡Llegó el gato!" :
                level == 1 ? "¡Genial! ¡Llegó el oso!" : "¡La banda toca junta!", null);
            if (level == 0) cat.gameObject.SetActive(true);
            if (level == 1) bear.gameObject.SetActive(true);
            if (level < 2 && animalHit != null && voiceSource != null)
                voiceSource.PlayOneShot(animalHit, 0.35f * GameSettings.EffectsVolume);
            yield return new WaitForSecondsRealtime(1.8f);
            if (star != null) star.gameObject.SetActive(false);
        }

        completed = true;
        playingMainLevels = false;
        SetHand(false);
        Say("¡Lo logramos! ¿Tocamos otra vez?", null);
        if (star != null) star.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
        recorder.Save(levels.Length, Mathf.RoundToInt(rhythm.bpm), true);
        activity = null;
    }

    private IEnumerator PlaySection(int beats, bool withInput, bool song, bool showHand)
    {
        sectionHits = 0;
        sectionIndex++;
        accepting = withInput;
        SetHand(showHand);
        rhythm.StartSequence(beats, song);
        drum.BeginBeatGuide(rhythm.FirstBeatDspTime, rhythm.Interval, beats);
        double end = rhythm.FirstBeatDspTime + (beats - 1) * rhythm.Interval + rhythm.toleranceSeconds;
        while (AudioSettings.dspTime < end) yield return null;
        accepting = false;
        rhythm.StopSequence();
        drum.EndBeatGuide();
        monkey.FinishPhrase();
        cat.FinishPhrase();
        bear.FinishPhrase();
        SetHand(false);
        yield return new WaitForSecondsRealtime(0.4f);
    }

    private IEnumerator PlayLoopedLevel(BandaLevelConfiguration config, int beats, bool showHand)
    {
        sectionHits = 0;
        sectionIndex++;
        accepting = true;
        SetHand(showHand);
        bool scheduled = rhythm.StartLoopedSequence(config.guidedLoop, config.backgroundLoop,
            config.beatsPerLoop, beats, config.challengeDurationSeconds,
            config.firstChallengeMusicFraction);
        if (!scheduled)
        {
            // Invalid/missing loops should not make the level impossible to play.
            sectionIndex--;
            yield return PlaySection(beats, true, true, showHand);
            yield break;
        }
        drum.BeginBeatGuide(rhythm.FirstBeatDspTime, rhythm.Interval, beats);
        double lastWindowEnd = rhythm.FirstBeatDspTime + (beats - 1) * rhythm.Interval + rhythm.toleranceSeconds;
        double audioEnd = rhythm.FirstBeatDspTime + beats * rhythm.Interval;
        int previousChallenge = 0;
        while (AudioSettings.dspTime < lastWindowEnd)
        {
            int challenge = rhythm.ChallengeIndexAt(AudioSettings.dspTime);
            if (challenge != previousChallenge)
            {
                if (challenge == 0)
                {
                    drum.SetBeatCueVisible(true);
                    SetHand(showHand);
                    Say(config.label + ": ¡la banda vuelve contigo!", null);
                }
                else
                {
                    SetHand(false);
                    drum.SetBeatCueVisible(challenge != 3);
                    Say(challenge == 1 ? "¡Tu momento! Toca cuando brille" :
                        challenge == 2 ? "¡Ahora tú! Toca cuando brille" :
                        "¡Ahora tú! Sigue el ritmo sin luz", null);
                }
                previousChallenge = challenge;
            }
            yield return null;
        }
        accepting = false;
        while (AudioSettings.dspTime < audioEnd) yield return null;
        rhythm.StopSequence();
        drum.EndBeatGuide();
        monkey.FinishPhrase();
        cat.FinishPhrase();
        bear.FinishPhrase();
        SetHand(false);
        yield return new WaitForSecondsRealtime(0.4f);
    }

    private IEnumerator ShowHelp()
    {
        recorder.Metrics.helpCount++;
        if (ear != null) ear.gameObject.SetActive(true);
        Say("¡Escuchemos otra vez!", helpVoice);
        yield return new WaitForSecondsRealtime(1.2f);
        if (ear != null) ear.gameObject.SetActive(false);
    }

    private void SetHand(bool visible)
    {
        if (hand != null) hand.gameObject.SetActive(visible && allowHelp);
    }

    private void UpdateHud(string level)
    {
        if (topHud == null) return;
        if (topHud.ScoreText != null) topHud.ScoreText.text = "Puntaje " + score;
        if (topHud.LevelText != null) topHud.LevelText.text = level;
    }

    private void Update()
    {
        if (hand != null && hand.gameObject.activeSelf)
        {
            float press = discovering
                ? 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * (2f * Mathf.PI / 0.75f))
                : Mathf.Clamp01(drum.CueStrength(AudioSettings.dspTime) * 2f);
            RectTransform handRect = hand.transform as RectTransform;
            if (handRect != null)
                handRect.anchoredPosition = Vector2.Lerp(new Vector2(228f, -155f),
                    new Vector2(126f, -222f), press);
            hand.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 0.84f, press);
        }
    }

    public void ReturnToMenu()
    {
        if (!completed && recorder != null)
            recorder.Save(Mathf.Max(1, currentLevel + 1), Mathf.RoundToInt(rhythm.bpm), false);
        if (rhythm != null) rhythm.StopSequence();
        if (drum != null) drum.EndBeatGuide();
        SceneTransitionController.LoadScene("MainScene");
    }

    private void OnDestroy()
    {
        if (rhythm != null)
        {
            rhythm.Beat -= OnBeat;
            rhythm.StopSequence();
        }
        BackgroundMusicPlayer.SetBandaMusicFocus(false);
    }

    // Can be connected to a future song-selection UI or changed in the Inspector.
    public void SelectSong(int index)
    {
        if (songs == null || index < 0 || index >= songs.Length || songs[index] == null ||
            songs[index].clip == null || songs[index].bpm <= 0f)
            return;
        selectedSongIndex = index;
        Restart();
    }
}
