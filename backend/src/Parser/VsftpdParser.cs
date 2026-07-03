using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class VsftpdParser : ILogParser
{
    // Format : Fri Jul  3 19:31:53 2026 [pid 1] [user] FAIL LOGIN: Client "127.0.0.1"
    private static readonly Regex LineRegex = new(
        @"(?<month>\w+)\s+(?<day>\w+)\s+(?<time>[\d:]+)\s+(?<year>\d{4})\s+\[pid \d+\]\s+(?:\[(?<user>[^\]]+)\]\s+)?(?<event>FAIL LOGIN|OK LOGIN|OK DOWNLOAD|OK UPLOAD|CONNECT):\s+Client ""(?<ip>[^""]+)""(?:,\s+""(?<file>[^""]+)"")?",
        RegexOptions.Compiled
    );

    // Extensions suspectes uploadées
    private static readonly HashSet<string> SuspiciousUploadExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".php", ".sh", ".bash", ".py", ".pl", ".asp", ".aspx", ".jsp"
    };

    public bool CanParse(string line)
    {
        return (line.Contains("FAIL LOGIN") ||
                line.Contains("OK LOGIN") ||
                line.Contains("OK DOWNLOAD") ||
                line.Contains("OK UPLOAD") ||
                line.Contains("CONNECT")) &&
               line.Contains("Client");
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = LineRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var ip    = IpNormalizer.Normalize(match.Groups["ip"].Value);
        var event_ = match.Groups["event"].Value;
        var user  = match.Groups["user"].Value;
        var file  = match.Groups["file"].Value;

        return event_ switch
        {
            "FAIL LOGIN" => new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FTP",
                Port      = 21,
                Action    = "BLOCK",
                Severity  = "WARNING",
                RawData   = rawLine.Content,
                Source    = "vsftpd",
            },

            "OK LOGIN" => new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FTP",
                Port      = 21,
                Action    = "ALLOW",
                Severity  = "INFO",
                RawData   = rawLine.Content,
                Source    = "vsftpd",
            },

            "OK UPLOAD" => new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FTP UPLOAD",
                Port      = 21,
                Action    = "ALLOW",
                // Upload d'un fichier suspect → CRITICAL
                Severity  = IsSuspiciousFile(file) ? "CRITICAL" : "INFO",
                RawData   = rawLine.Content,
                Source    = "vsftpd",
            },

            "OK DOWNLOAD" => new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FTP DOWNLOAD",
                Port      = 21,
                Action    = "ALLOW",
                Severity  = "INFO",
                RawData   = rawLine.Content,
                Source    = "vsftpd",
            },

            // CONNECT seul sans login → pas intéressant
            _ => null
        };
    }

    private static bool IsSuspiciousFile(string file)
    {
        if (string.IsNullOrEmpty(file)) return false;
        var ext = System.IO.Path.GetExtension(file);
        return !string.IsNullOrEmpty(ext) && SuspiciousUploadExtensions.Contains(ext);
    }
}
