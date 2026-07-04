using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class Fail2banParser : ILogParser
{
    // Format : 2026-07-03 20:35:08,400 fail2ban.actions [22093]: NOTICE  [sshd] Ban ::1
    private static readonly Regex BanRegex = new(
        @"(?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}),\d+ fail2ban\.actions\s+\[\d+\]: NOTICE\s+\[(?<jail>[^\]]+)\] (?<action>Ban|Unban) (?<ip>[\da-fA-F:.]+)",
        RegexOptions.Compiled
    );

    // Format : 2026-07-03 20:35:04,093 fail2ban.filter [22093]: INFO    [sshd] Found ::1 - 2026-07-03 20:35:03
    private static readonly Regex FoundRegex = new(
        @"(?<date>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}),\d+ fail2ban\.filter\s+\[\d+\]: INFO\s+\[(?<jail>[^\]]+)\] Found (?<ip>[\da-fA-F:.]+)",
        RegexOptions.Compiled
    );

    public bool CanParse(string line)
    {
        return line.Contains("fail2ban.actions") && (line.Contains("] Ban ") || line.Contains("] Unban ")) ||
               line.Contains("fail2ban.filter") && line.Contains("] Found ");
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        // Ban ou Unban
        var banMatch = BanRegex.Match(rawLine.Content);
        if (banMatch.Success)
        {
            var action = banMatch.Groups["action"].Value;
            var ip     = IpNormalizer.Normalize(banMatch.Groups["ip"].Value);
            var jail   = banMatch.Groups["jail"].Value;

            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FAIL2BAN",
                Port      = null,
                Action    = action == "Ban" ? "BLOCK" : "ALLOW",
                Severity  = action == "Ban" ? "CRITICAL" : "INFO",
                RawData   = rawLine.Content,
                Source    = "fail2ban",
            };
        }

        // Found (tentative détectée)
        var foundMatch = FoundRegex.Match(rawLine.Content);
        if (foundMatch.Success)
        {
            var ip   = IpNormalizer.Normalize(foundMatch.Groups["ip"].Value);
            var jail = foundMatch.Groups["jail"].Value;

            return new NetworkEvent
            {
                Timestamp = rawLine.ReceivedAt,
                SourceIp  = ip,
                Protocol  = "FAIL2BAN",
                Port      = null,
                Action    = "BLOCK",
                Severity  = "WARNING",
                RawData   = rawLine.Content,
                Source    = "fail2ban",
            };
        }

        return null;
    }
}
