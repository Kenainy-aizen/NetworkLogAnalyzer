using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class PamParser : ILogParser
{
    // su[5748]: FAILED SU (to root) kenainy on pts/1
    private static readonly Regex SuFailedRegex = new(
        @"su\[\d+\]: FAILED SU \(to (?<target>\S+)\) (?<user>\S+) on (?<tty>\S+)",
        RegexOptions.Compiled
    );

    // su[5748]: Successful su for root by kenainy
    private static readonly Regex SuSuccessRegex = new(
        @"su\[\d+\]: Successful su for (?<target>\S+) by (?<user>\S+)",
        RegexOptions.Compiled
    );

    // pam_unix(su:auth): authentication failure; ... user=root
    private static readonly Regex PamAuthFailRegex = new(
        @"pam_unix\((?<service>[^:]+):auth\): authentication failure;.*user=(?<user>\S+)",
        RegexOptions.Compiled
    );

    // login[xxx]: FAILED LOGIN (1) on 'tty1' FOR 'fakeuser'
    private static readonly Regex LoginFailedRegex = new(
        @"login\[\d+\]: FAILED LOGIN.*FOR '(?<user>\S+)'",
        RegexOptions.Compiled
    );

    public bool CanParse(string line)
    {
        return line.Contains("FAILED SU") ||
               line.Contains("Successful su") ||
               line.Contains("pam_unix(su:auth): authentication failure") ||
               line.Contains("pam_unix(login:auth): authentication failure") ||
               line.Contains("FAILED LOGIN");
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var line = rawLine.Content;

        // su échoué
        var suFailed = SuFailedRegex.Match(line);
        if (suFailed.Success)
        {
            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = "localhost",
                Protocol  = "SU",
                Port      = null,
                Action    = "BLOCK",
                Severity  = "WARNING",
                RawData   = line,
                Source    = "pam",
            };
        }

        // su réussi
        var suSuccess = SuSuccessRegex.Match(line);
        if (suSuccess.Success)
        {
            // Su vers root → plus intéressant à surveiller
            var target = suSuccess.Groups["target"].Value;
            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = "localhost",
                Protocol  = "SU",
                Port      = null,
                Action    = "ALLOW",
                Severity  = target == "root" ? "WARNING" : "INFO",
                RawData   = line,
                Source    = "pam",
            };
        }

        // PAM auth failure générique (su ou login)
        var pamFail = PamAuthFailRegex.Match(line);
        if (pamFail.Success)
        {
            var service = pamFail.Groups["service"].Value;
            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = "localhost",
                Protocol  = service.ToUpper(),
                Port      = null,
                Action    = "BLOCK",
                Severity  = "WARNING",
                RawData   = line,
                Source    = "pam",
            };
        }

        // Login TTY échoué
        var loginFailed = LoginFailedRegex.Match(line);
        if (loginFailed.Success)
        {
            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = "localhost",
                Protocol  = "LOGIN",
                Port      = null,
                Action    = "BLOCK",
                Severity  = "WARNING",
                RawData   = line,
                Source    = "pam",
            };
        }

        return null;
    }
}
