using System;
using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    private const string ActivePlayerIdKey = "ACTIVE_PLAYER_ID";
    private const string ActivePlayerNameKey = "ACTIVE_PLAYER_NAME";
    private const string ActivePlayerAvatarKey = "ACTIVE_PLAYER_AVATAR";

    public static PlayerProfileManager Instance { get; private set; }
    public PlayerProfileData ActiveProfile { get; private set; }
    public bool HasActiveProfile => ActiveProfile != null && !string.IsNullOrEmpty(ActiveProfile.playerId);
    public event Action<PlayerProfileData> ActiveProfileChanged;

    [SerializeField] private string[] avatarOptions = { "conejo_azul", "zorro_naranja", "oso_verde" };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadActiveProfileFromPrefs();
    }

    public string[] GetAvatarOptions()
    {
        return avatarOptions;
    }

    public void LoadProfiles(Action<List<PlayerProfileData>> onSuccess, Action<string> onError)
    {
        FirebaseManager manager = FirebaseManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            onError?.Invoke("Firebase no esta listo.");
            return;
        }

        manager.Db.Collection("players").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke(task.Exception != null ? task.Exception.GetBaseException().Message : "No se pudieron cargar perfiles.");
                return;
            }

            List<PlayerProfileData> profiles = new List<PlayerProfileData>();
            QuerySnapshot snapshot = task.Result;
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> data = document.ToDictionary();
                PlayerProfileData profile = new PlayerProfileData
                {
                    playerId = GetString(data, "playerId", document.Id),
                    name = GetString(data, "name", document.Id),
                    avatar = GetString(data, "avatar", avatarOptions.Length > 0 ? avatarOptions[0] : "avatar_default"),
                    createdAt = GetString(data, "createdAt", ReportUtility.UtcNowString()),
                };
                profiles.Add(profile);
            }

            profiles.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            onSuccess?.Invoke(profiles);
        });
    }

    public void CreateProfile(string playerName, string avatar, Action<PlayerProfileData> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            onError?.Invoke("Debes escribir un nombre.");
            return;
        }

        FirebaseManager manager = FirebaseManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            onError?.Invoke("Firebase no esta listo.");
            return;
        }

        PlayerProfileData profile = new PlayerProfileData
        {
            playerId = ReportUtility.GenerateId("player"),
            name = playerName.Trim(),
            avatar = string.IsNullOrWhiteSpace(avatar) ? (avatarOptions.Length > 0 ? avatarOptions[0] : "avatar_default") : avatar,
            createdAt = ReportUtility.UtcNowString(),
        };

        manager.Db.Collection("players").Document(profile.playerId).SetAsync(ReportUtility.ToDictionary(profile)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke(task.Exception != null ? task.Exception.GetBaseException().Message : "No se pudo crear el perfil.");
                return;
            }

            SetActiveProfile(profile);
            onSuccess?.Invoke(profile);
        });
    }

    public void SetActiveProfile(PlayerProfileData profile)
    {
        if (profile == null)
            return;

        ActiveProfile = profile;
        PlayerPrefs.SetString(ActivePlayerIdKey, profile.playerId ?? string.Empty);
        PlayerPrefs.SetString(ActivePlayerNameKey, profile.name ?? string.Empty);
        PlayerPrefs.SetString(ActivePlayerAvatarKey, profile.avatar ?? string.Empty);
        PlayerPrefs.Save();
        ActiveProfileChanged?.Invoke(ActiveProfile);
    }

    public void ClearActiveProfile()
    {
        ActiveProfile = null;
        PlayerPrefs.DeleteKey(ActivePlayerIdKey);
        PlayerPrefs.DeleteKey(ActivePlayerNameKey);
        PlayerPrefs.DeleteKey(ActivePlayerAvatarKey);
        PlayerPrefs.Save();
        ActiveProfileChanged?.Invoke(null);
    }

    private void LoadActiveProfileFromPrefs()
    {
        string playerId = PlayerPrefs.GetString(ActivePlayerIdKey, string.Empty);
        if (string.IsNullOrEmpty(playerId))
            return;

        ActiveProfile = new PlayerProfileData
        {
            playerId = playerId,
            name = PlayerPrefs.GetString(ActivePlayerNameKey, string.Empty),
            avatar = PlayerPrefs.GetString(ActivePlayerAvatarKey, avatarOptions.Length > 0 ? avatarOptions[0] : "avatar_default"),
            createdAt = string.Empty,
        };
    }

    private static string GetString(Dictionary<string, object> data, string key, string fallback)
    {
        return data != null && data.TryGetValue(key, out object value) && value != null ? value.ToString() : fallback;
    }
}