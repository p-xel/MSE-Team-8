using System;
using System.Collections.Generic;

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[Serializable]
public class AuthResponse
{
    public string token;
    public string accountId;
}

[Serializable]
public class AccountResponse
{
    public string id;
    public string username;
    public int level;
    public long exp;
    public string selectedCharacter;
    public string createdAt;
    public string updatedAt;
}

[Serializable]
public class AccountStatsResponse
{
    public long kills;
    public long deflects;
    public long roundsPlayed;
    public long gamesPlayed;
    public long totalPlaytimeSeconds;
    public string updatedAt;
    public int level;
    public long exp;
}

[Serializable]
public class LeaderboardEntryResponse
{
    public int rank;
    public string id;
    public string username;
    public int level;
    public long exp;
}

[Serializable]
public class MatchRecordResponse
{
    public string id;
    public string matchId;
    public long kills;
    public long deflects;
    public int roundsPlayed;
    public long playtimeSeconds;
    public string result;
    public string playedAt;
}

[Serializable]
public class MatchSubmissionRequest
{
    public string matchId;
    public long kills;
    public long deflects;
    public int roundsPlayed;
    public long playtimeSeconds;
    public string result;
}

[Serializable]
public class UpdateCharacterRequest
{
    public string selectedCharacter;
}

[Serializable]
public class ListWrapper<T>
{
    public List<T> items;
}
