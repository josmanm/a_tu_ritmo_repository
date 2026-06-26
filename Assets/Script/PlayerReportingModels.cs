using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerProfileData
{
    public string playerId;
    public string name;
    public string avatar;
    public string createdAt;
}

[Serializable]
public class SessionReportData
{
    public string sessionId;
    public string playerId;
    public string playerName;
    public string avatar;
    public string startedAt;
    public string endedAt;
    public int totalTimeSeconds;
}

[Serializable]
public class AttemptReportData
{
    public string attemptId;
    public string sessionId;
    public string playerId;
    public string playerName;
    public string avatar;
    public string miniGame;
    public int level;
    public string difficulty;
    public int bpm;
    public int errors;
    public int correctAnswers;
    public int levelRepetitions;
    public bool completed;
    public int timeSeconds;
    public int scoreFinal;
    public bool wasTutorial;
    public string exitReason;
    public string playedAt;
}

public static class ReportUtility
{
    public static string UtcNowString()
    {
        return DateTime.UtcNow.ToString("o");
    }

    public static string GenerateId(string prefix)
    {
        return prefix + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }

    public static Dictionary<string, object> ToDictionary(PlayerProfileData data)
    {
        return new Dictionary<string, object>
        {
            { "playerId", data.playerId },
            { "name", data.name },
            { "avatar", data.avatar },
            { "createdAt", data.createdAt },
        };
    }

    public static Dictionary<string, object> ToDictionary(SessionReportData data)
    {
        return new Dictionary<string, object>
        {
            { "sessionId", data.sessionId },
            { "playerId", data.playerId },
            { "playerName", data.playerName },
            { "avatar", data.avatar },
            { "startedAt", data.startedAt },
            { "endedAt", data.endedAt },
            { "totalTimeSeconds", data.totalTimeSeconds },
        };
    }

    public static Dictionary<string, object> ToDictionary(AttemptReportData data)
    {
        return new Dictionary<string, object>
        {
            { "attemptId", data.attemptId },
            { "sessionId", data.sessionId },
            { "playerId", data.playerId },
            { "playerName", data.playerName },
            { "avatar", data.avatar },
            { "miniGame", data.miniGame },
            { "level", data.level },
            { "difficulty", data.difficulty },
            { "bpm", data.bpm },
            { "errors", data.errors },
            { "correctAnswers", data.correctAnswers },
            { "levelRepetitions", data.levelRepetitions },
            { "completed", data.completed },
            { "timeSeconds", data.timeSeconds },
            { "scoreFinal", data.scoreFinal },
            { "wasTutorial", data.wasTutorial },
            { "exitReason", data.exitReason },
            { "playedAt", data.playedAt },
        };
    }
}