using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class JournalParser : ILogParser
{
    // Exemple : Jun 29 20:15:32 hostname sshd[1234]: message
    private static readonly Regex LineRegex = new(
        @"^(?<month>\w+)\s+(?<day>\d+)\s+(?<time>\d+:\d+:\d+)\s+(?<host>\S+)\s+(?<service>\S+?)(?:\[(?<pid>\d+)\])?:\s+(?<message>.+)$",
        RegexOptions.Compiled
    );

    // Détecte une tentative SSH échouée
    private static readonly Regex SshFailedRegex = new(
        @"Failed password for (?:invalid user )?(?<user>\S+) from (?<ip>[\d.]+) port (?<port>\d+)",
        RegexOptions.Compiled
    );

    // Détecte une connexion SSH réussie
    private static readonly Regex SshAcceptedRegex = new(
        @"Accepted (?:password|publickey) for (?<user>\S+) from (?<ip>[\d.]+) port (?<port>\d+)",
        RegexOptions.Compiled
    );

    // Détecte une commande sudo
    private static readonly Regex SudoRegex = new(
        @"(?<user>\S+)\s+:.*COMMAND=(?<command>.+)$",
        RegexOptions.Compiled
    );

    public bool CanParse(string line)
    {
        return LineRegex.IsMatch(line);
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = LineRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var service = match.Groups["service"].Value.ToLower();
        var message = match.Groups["message"].Value;
        var host    = match.Groups["host"].Value;

        // Construire la date depuis les champs du log
        var dateStr = $"{match.Groups["month"].Value} {match.Groups["day"].Value} {match.Groups["time"].Value} {DateTime.UtcNow.Year}";
        DateTime.TryParse(dateStr, out var timestamp);

        var networkEvent = new NetworkEvent
        {
            Timestamp  = timestamp == default ? DateTime.UtcNow : timestamp,
            RawData    = rawLine.Content,
            Source     = rawLine.Source,
            Protocol   = "TCP",
            Action     = "INFO",
            Severity   = "INFO",
            SourceIp   = host,
        };

        // SSH échoué
        var sshFailed = SshFailedRegex.Match(message);
        if (sshFailed.Success && service.Contains("sshd"))
        {
            networkEvent.SourceIp = sshFailed.Groups["ip"].Value;
            networkEvent.Port     = int.TryParse(sshFailed.Groups["port"].Value, out var p) ? p : 22;
            networkEvent.Protocol = "SSH";
            networkEvent.Action   = "BLOCK";
            networkEvent.Severity = "WARNING";
            return networkEvent;
        }

        // SSH accepté
        var sshAccepted = SshAcceptedRegex.Match(message);
        if (sshAccepted.Success && service.Contains("sshd"))
        {
            networkEvent.SourceIp = sshAccepted.Groups["ip"].Value;
            networkEvent.Port     = int.TryParse(sshAccepted.Groups["port"].Value, out var p) ? p : 22;
            networkEvent.Protocol = "SSH";
            networkEvent.Action   = "ALLOW";
            networkEvent.Severity = "INFO";
            return networkEvent;
        }

        // Sudo
        var sudo = SudoRegex.Match(message);
        if (sudo.Success && service.Contains("sudo"))
        {
            networkEvent.Protocol = "SUDO";
            networkEvent.Action   = "ALLOW";
            networkEvent.Severity = "WARNING";
            return networkEvent;
        }

        // Ligne reconnue mais pas catégorisée → on ignore
        return null;
    }
}
