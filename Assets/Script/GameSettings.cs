using System;
using UnityEngine;

public static class GameSettings
{
    public const float DefaultMusicVolume = 1f;
    public const float DefaultEffectsVolume = 1f;
    public const float DefaultGameplaySpeed = 1f;

    private const string MusicVolumeKey = "GAME_SETTINGS_MUSIC_VOLUME";
    private const string EffectsVolumeKey = "GAME_SETTINGS_EFFECTS_VOLUME";
    private const string GameplaySpeedKey = "GAME_SETTINGS_GAMEPLAY_SPEED";

    private static bool initialized;
    private static float musicVolume = 1f;
    private static float effectsVolume = 1f;
    private static float gameplaySpeed = 1f;

    public static event Action SettingsChanged;

    public static float MusicVolume
    {
        get
        {
            EnsureInitialized();
            return musicVolume;
        }
    }

    public static float EffectsVolume
    {
        get
        {
            EnsureInitialized();
            return effectsVolume;
        }
    }

    public static float GameplaySpeed
    {
        get
        {
            EnsureInitialized();
            return gameplaySpeed;
        }
    }

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, DefaultEffectsVolume);
        gameplaySpeed = PlayerPrefs.GetFloat(GameplaySpeedKey, DefaultGameplaySpeed);
        initialized = true;
    }

    public static void SetMusicVolume(float value)
    {
        EnsureInitialized();
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        SettingsChanged?.Invoke();
    }

    public static void SetEffectsVolume(float value)
    {
        EnsureInitialized();
        effectsVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(EffectsVolumeKey, effectsVolume);
        PlayerPrefs.Save();
        SettingsChanged?.Invoke();
    }

    public static void SetGameplaySpeed(float value)
    {
        EnsureInitialized();
        gameplaySpeed = Mathf.Clamp(value, 0.75f, 1.5f);
        PlayerPrefs.SetFloat(GameplaySpeedKey, gameplaySpeed);
        PlayerPrefs.Save();
        SettingsChanged?.Invoke();
    }

    public static void ResetDefaults()
    {
        EnsureInitialized();
        musicVolume = DefaultMusicVolume;
        effectsVolume = DefaultEffectsVolume;
        gameplaySpeed = DefaultGameplaySpeed;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(EffectsVolumeKey, effectsVolume);
        PlayerPrefs.SetFloat(GameplaySpeedKey, gameplaySpeed);
        PlayerPrefs.Save();
        SettingsChanged?.Invoke();
    }
}
