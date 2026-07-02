using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class NginxParser : ILogParser
{
    // Format : 127.0.0.1 - - [02/Jul/2026:11:37:55 +0300] "GET /path HTTP/1.1" 200 896 "-" "curl/8.21.0"
    private static readonly Regex NginxRegex = new(
        @"^(?<ip>[\da-fA-F:.]+) \S+ \S+ \[(?<time>[^\]]+)\] ""(?<method>\w+) (?<path>\S+) HTTP/[\d.]+"" (?<status>\d+) (?<size>\d+)",
        RegexOptions.Compiled
    );

    // Chemins suspects souvent scannés par des bots et attaquants
    private static readonly HashSet<string> SuspiciousPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin", "/administrator", "/wp-login.php", "/wp-admin",
        "/.env", "/.git", "/config.php", "/phpinfo.php",
        "/shell.php", "/cmd.php", "/backdoor.php",
        "/etc/passwd", "/proc/self/environ",
        "/.aws/credentials", "/.ssh/id_rsa",
        "/manager/html", "/phpmyadmin",
    };

    // Extensions suspectes
    private static readonly HashSet<string> SuspiciousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".php", ".asp", ".aspx", ".jsp", ".cgi", ".sh", ".bash"
    };

    public bool CanParse(string line)
    {
        return NginxRegex.IsMatch(line);
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = NginxRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var ip     = match.Groups["ip"].Value;
        var method = match.Groups["method"].Value;
        var path   = match.Groups["path"].Value;
        var status = int.TryParse(match.Groups["status"].Value, out var s) ? s : 0;

        // Déterminer la sévérité selon le chemin et le code HTTP
        var severity = DetermineSeverity(path, status);
        if (severity is null) return null; // Requête normale → on ignore

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
            Source    = "nginx",
            DestinationIp = null,
        };
    }

    private static string? DetermineSeverity(string path, int status)
    {
        // Chemin explicitement suspect
        if (SuspiciousPaths.Contains(path))
            return "WARNING";

        // Extension suspecte dans le chemin
        var ext = System.IO.Path.GetExtension(path);
        if (!string.IsNullOrEmpty(ext) && SuspiciousExtensions.Contains(ext))
            return "WARNING";

        // Tentative de path traversal
        if (path.Contains("../") || path.Contains("..\\"))
            return "CRITICAL";

        // Erreur serveur (500+) → toujours intéressant
        if (status >= 500)
            return "WARNING";

        // Requête normale (200, 301, etc.) → on ignore
        return null;
    }
}
