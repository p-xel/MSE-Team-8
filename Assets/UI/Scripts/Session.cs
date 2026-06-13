using UnityEngine;

public static class Session
{
    public static bool IsLoggedIn { get; set; }
    public static string Username { get; set; }
    public static string Token { get; set; }
    public static string AccountId { get; set; }

    private static float _volume = 1f;
    public static float Volume
    {
        get => _volume;
        set
        {
            _volume = value;
            AudioListener.volume = value;
        }
    }

    public static Skin Skin { get; set; }
    public static int PendingBotCount { get; set; }
    public static string MatchId { get; set; }
}
