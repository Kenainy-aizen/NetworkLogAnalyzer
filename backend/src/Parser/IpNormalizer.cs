namespace Parser;

public static class IpNormalizer
{
    private static readonly HashSet<string> LocalIps = new(StringComparer.OrdinalIgnoreCase)
    {
        "127.0.0.1",
        "::1",
        "0:0:0:0:0:0:0:1",
        "localhost",
        "",
    };

    public static string Normalize(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "localhost";
        if (LocalIps.Contains(ip)) return "localhost";
        return ip;
    }
}
