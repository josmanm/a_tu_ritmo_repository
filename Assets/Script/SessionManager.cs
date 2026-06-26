using System;
using Firebase.Extensions;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public SessionReportData CurrentSession { get; private set; }
    public bool HasActiveSession => CurrentSession != null && !string.IsNullOrEmpty(CurrentSession.sessionId);

    private DateTime sessionStartUtc;

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

    private void OnApplicationQuit()
    {
        EndCurrentSession(null, null);
    }

    public void StartSessionForPlayer(PlayerProfileData profile, Action<SessionReportData> onSuccess, Action<string> onError)
    {
        if (profile == null || string.IsNullOrEmpty(profile.playerId))
        {
            onError?.Invoke("No hay perfil activo.");
            return;
        }

        if (HasActiveSession)
            EndCurrentSession(null, null);

        FirebaseManager manager = FirebaseManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            onError?.Invoke("Firebase no esta listo.");
            return;
        }

        sessionStartUtc = DateTime.UtcNow;
        CurrentSession = new SessionReportData
        {
            sessionId = ReportUtility.GenerateId("session"),
            playerId = profile.playerId,
            playerName = profile.name,
            avatar = profile.avatar,
            startedAt = ReportUtility.UtcNowString(),
            endedAt = string.Empty,
            totalTimeSeconds = 0,
        };

        manager.Db.Collection("sessions").Document(CurrentSession.sessionId).SetAsync(ReportUtility.ToDictionary(CurrentSession)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                CurrentSession = null;
                onError?.Invoke(task.Exception != null ? task.Exception.GetBaseException().Message : "No se pudo iniciar la sesion.");
                return;
            }

            onSuccess?.Invoke(CurrentSession);
        });
    }

    public void EndCurrentSession(Action onSuccess, Action<string> onError)
    {
        if (!HasActiveSession)
        {
            onSuccess?.Invoke();
            return;
        }

        FirebaseManager manager = FirebaseManager.Instance;
        if (manager == null || !manager.IsReady)
        {
            CurrentSession = null;
            onError?.Invoke("Firebase no esta listo.");
            return;
        }

        CurrentSession.endedAt = ReportUtility.UtcNowString();
        CurrentSession.totalTimeSeconds = Mathf.Max(0, Mathf.RoundToInt((float)(DateTime.UtcNow - sessionStartUtc).TotalSeconds));

        SessionReportData sessionToClose = CurrentSession;
        CurrentSession = null;

        manager.Db.Collection("sessions").Document(sessionToClose.sessionId).SetAsync(ReportUtility.ToDictionary(sessionToClose)).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                onError?.Invoke(task.Exception != null ? task.Exception.GetBaseException().Message : "No se pudo cerrar la sesion.");
                return;
            }

            onSuccess?.Invoke();
        });
    }
}