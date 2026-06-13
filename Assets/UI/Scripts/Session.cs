public static class Session
{
    public static bool IsLoggedIn { get; set; }
    public static string Username { get; set; }
    public static string Token { get; set; }
    public static string AccountId { get; set; }
    public static float Volume { get; set; } = 1f;
    public static Skin Skin { get; set; }
}
