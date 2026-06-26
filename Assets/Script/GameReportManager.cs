using System;
using Firebase.Extensions;
using UnityEngine;

public class GameReportManager : MonoBehaviour
{
    public static GameReportManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ReportAttempt(AttemptReportData data, Action onSuccess, Action<string> onError)
    {
        FirebaseManager manager = FirebaseManager.Instance;
        PlayerProfileManager profileManager = PlayerProfileManager.Instance;
        SessionManager sessionManager = SessionManager.Instance;

        if (manager == null || !manager.IsReady)
        {
            onError?.Invoke("Firebase no esta listo.");
            return;
        }

        if (profileManager == null || !profileManager.HasActiveProfile)
        {
            onError?.Invoke("No hay perfil activo.");
            return;
        }

        if (sessionManager == null || !sessionManager.HasActiveSession)
        {
            onError?.Invoke("No hay sesion activa.");
            return;
        }

        data.attemptId = string.IsNullOrEmpty(data.attemptId) ? ReportUtility.GenerateId("attempt") : data.attemptId;
        data.playerId = profileManager.ActiveProfile.playerId;
        data.playerName = profileManager.ActiveProfile.name;
        data.avatar = profileManager.ActiveProfile.avatar;
        data.sessionId = sessionManager.CurrentSession.sessionId;
        data.playedAt = string.IsNullOrEmpty(data.playedAt) ? ReportUtility.UtcNowString() : data.playedAt;

        manager.Db.Collection("attempts").Document(data.attemptId).SetAsync(ReportUtility.ToDictionary(data)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke(task.Exception != null ? task.Exception.GetBaseException().Message : "No se pudo guardar el intento.");
                return;
            }

            onSuccess?.Invoke();
        });
    }
}