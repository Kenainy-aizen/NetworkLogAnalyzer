using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class ApacheParser : ILogParser
{
    // Format identique à Nginx
    private static readonly Regex ApacheRegex = new(
        @"^(?<ip>[\da-fA-F:.]+) \S+ \S+ \[(?<time>[^\]]+)\] ""(?<method>\w+) (?<path>\S+) HTTP/[\d.]+"" (?<status>\d+) (?<size>\d+)",
        RegexOptions.Compiled
    );

    private static readonly HashSet<string> SuspiciousPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin", "/administrator", "/wp-login.php", "/wp-admin",
        "/.env", "/.git", "/config.php", "/phpinfo.php",
        "/shell.php", "/cmd.php", "/backdoor.php",
        "/etc/passwd", "/proc/self/environ",
        "/.aws/credentials", "/.ssh/id_rsa",
        "/manager/html", "/phpmyadmin",
    };

    private static readonly HashSet<string> SuspiciousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".php", ".asp", ".aspx", ".jsp", ".cgi", ".sh", ".bash"
    };

    public bool CanParse(string line)
    {
        return ApacheRegex.IsMatch(line);
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = ApacheRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var ip     = match.Groups["ip"].Value;
        var method = match.Groups["method"].Value;
        var path   = match.Groups["path"].Value;
        var status = int.TryParse(match.Groups["status"].Value, out var s) ? s : 0;

        var severity = DetermineSeverity(path, status);
        if (severity is null) return null;

        var action = status >= 400 ? "BLOCK" : "ALLOW";

        return new NetworkEvent
        {
            Timestamp = rawLine.ReceivedAt,
            SourceIp  = ip,
            Protocol  = $"HTTP {method}",
            Port      = 80,
            Action    = action,
            Severity  = severity,
            RawData   = rawLine.Content,
            Source    = "apache",
        };
    }

    private static string? DetermineSeverity(string path, int status)
    {
        if (SuspiciousPaths.Contains(path))
            return "WARNING";

        var ext = System.IO.Path.GetExtension(path);
        if (!string.IsNullOrEmpty(ext) && SuspiciousExtensions.Contains(ext))
            return "WARNING";

        if (path.Contains("../") || path.Contains("..\\"))
            return "CRITICAL";

        if (status >= 500)
            return "WARNING";

        return null;
    }
}
