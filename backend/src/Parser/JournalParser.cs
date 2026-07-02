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

    // IP ou hostname (gère IPv4, IPv6 type ::1, et les deux-points)
    private const string IpPattern = @"(?<ip>[\da-fA-F:.]+)";

    // Détecte une tentative SSH échouée (mot de passe OU utilisateur invalide)
    private static readonly Regex SshFailedRegex = new(
        $@"Failed password for (?:invalid user )?(?<user>\S+) from {IpPattern} port (?<port>\d+)",
        RegexOptions.Compiled
    );

    // Détecte un utilisateur invalide (avant même le mot de passe)
    private static readonly Regex SshInvalidUserRegex = new(
        $@"Invalid user (?<user>\S+) from {IpPattern} port (?<port>\d+)",
        RegexOptions.Compiled
    );

    // Détecte une connexion SSH réussie
    private static readonly Regex SshAcceptedRegex = new(
        $@"Accepted (?:password|publickey) for (?<user>\S+) from {IpPattern} port (?<port>\d+)",
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

        // Le nom du service SSH peut être "sshd" ou "sshd-session" selon la version
        var service = match.Groups["service"].Value.ToLower();
        var message = match.Groups["message"].Value;

        var dateStr = $"{match.Groups["month"].Value} {match.Groups["day"].Value} {match.Groups["time"].Value} {DateTime.UtcNow.Year}";
        DateTime.TryParse(dateStr, out var timestamp);

        var networkEvent = new NetworkEvent
        {
            Timestamp = timestamp == default ? DateTime.UtcNow : timestamp,
            RawData   = rawLine.Content,
            Source    = rawLine.Source,
            Protocol  = "INFO",
            Action    = "INFO",
            Severity  = "INFO",
            SourceIp  = string.Empty, // jamais le hostname par défaut
        };

        bool isSsh = service.Contains("sshd");

        // SSH : mot de passe échoué
        var sshFailed = SshFailedRegex.Match(message);
        if (isSsh && sshFailed.Success)
        {
            networkEvent.SourceIp = sshFailed.Groups["ip"].Value;
            networkEvent.Port     = int.TryParse(sshFailed.Groups["port"].Value, out var p) ? p : 22;
            networkEvent.Protocol = "SSH";
            networkEvent.Action   = "BLOCK";
            networkEvent.Severity = "WARNING";
            return networkEvent;
        }

        // SSH : utilisateur invalide (première étape avant échec mot de passe)
        var sshInvalid = SshInvalidUserRegex.Match(message);
        if (isSsh && sshInvalid.Success)
        {
            networkEvent.SourceIp = sshInvalid.Groups["ip"].Value;
            networkEvent.Port     = int.TryParse(sshInvalid.Groups["port"].Value, out var p) ? p : 22;
            networkEvent.Protocol = "SSH";
            networkEvent.Action   = "BLOCK";
            networkEvent.Severity = "WARNING";
            return networkEvent;
        }

        // SSH : connexion acceptée
        var sshAccepted = SshAcceptedRegex.Match(message);
        if (isSsh && sshAccepted.Success)
        {
            networkEvent.SourceIp = sshAccepted.Groups["ip"].Value;
            networkEvent.Port     = int.TryParse(sshAccepted.Groups["port"].Value, out var p) ? p : 22;
            networkEvent.Protocol = "SSH";
            networkEvent.Action   = "ALLOW";
            networkEvent.Severity = "INFO";
            return networkEvent;
        }

        // Sudo : on ne met PAS le hostname dans SourceIp (ça n'a pas de sens ici)
        var sudo = SudoRegex.Match(message);
        if (service.Contains("sudo") && sudo.Success)
        {
            networkEvent.SourceIp  = "localhost";
            networkEvent.Protocol = "SUDO";
            networkEvent.Action   = "ALLOW";
            networkEvent.Severity = "WARNING";
            return networkEvent;
        }

        // Ligne reconnue par le format général mais pas catégorisée → on ignore
        return null;
    }
}
