public static class StatFormat
{
    public static string Time(long seconds)
    {
        long h = seconds / 3600;
        long m = (seconds % 3600) / 60;
        if (h > 0) return $"{h}h {m}m";
        long s = seconds % 60;
        return $"{m}m {s}s";
    }

    public static string Date(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "";
        return iso.Length >= 10 ? iso.Substring(0, 10) : iso;
    }
}
