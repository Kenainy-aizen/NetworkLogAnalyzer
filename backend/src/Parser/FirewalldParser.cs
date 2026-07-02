using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class FirewalldParser : ILogParser
{
    // Reconnaît les lignes firewalld : filter_IN_public_REJECT ou filter_IN_public_DROP
    private static readonly Regex FirewalldRegex = new(
        @"filter_IN_\w+_(?<action>REJECT|DROP|ACCEPT):\s+IN=(?<iface>\S*)\s+.*SRC=(?<src>[\da-fA-F:.]+)\s+DST=(?<dst>[\da-fA-F:.]+)\s+.*PROTO=(?<proto>\w+)(?:.*SPT=(?<spt>\d+))?(?:.*DPT=(?<dpt>\d+))?",
        RegexOptions.Compiled
    );

    public bool CanParse(string line)
    {
        return line.Contains("filter_IN_") &&
               (line.Contains("_REJECT") || line.Contains("_DROP") || line.Contains("_ACCEPT"));
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = FirewalldRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var action = match.Groups["action"].Value switch
        {
            "REJECT" => "BLOCK",
            "DROP"   => "BLOCK",
            "ACCEPT" => "ALLOW",
            _        => "INFO"
        };

        var severity = action == "BLOCK" ? "WARNING" : "INFO";

        // Ignorer le trafic multicast/broadcast local (bruit réseau normal)
        var dst = match.Groups["dst"].Value;
        if (dst.StartsWith("224.") || dst.StartsWith("239.") ||
            dst.StartsWith("ff0") || dst.StartsWith("ff02"))
        {
            return null;
        }

        return new NetworkEvent
        {
            Timestamp     = rawLine.ReceivedAt,
            SourceIp      = match.Groups["src"].Value,
            DestinationIp = dst,
            Protocol      = match.Groups["proto"].Value.ToUpper(),
            Port          = int.TryParse(match.Groups["dpt"].Value, out var port) ? port : null,
            Action        = action,
            Severity      = severity,
            RawData       = rawLine.Content,
            Source        = "firewalld",
        };
    }
}
