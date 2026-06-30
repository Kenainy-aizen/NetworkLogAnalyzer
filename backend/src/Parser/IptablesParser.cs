using System.Text.RegularExpressions;
using Parser.Models;
using Storage.Models;

namespace Parser;

public class IptablesParser : ILogParser
{
    private static readonly Regex IptablesRegex = new(
        @"(?:UFW|iptables|BLOCK|ALLOW).*SRC=(?<src>[\d.]+).*DST=(?<dst>[\d.]+).*PROTO=(?<proto>\w+)(?:.*DPT=(?<dpt>\d+))?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public bool CanParse(string line)
    {
        return line.Contains("UFW") || line.Contains("iptables") ||
               (line.Contains("SRC=") && line.Contains("DST="));
    }

    public NetworkEvent? Parse(RawLogLine rawLine)
    {
        var match = IptablesRegex.Match(rawLine.Content);
        if (!match.Success) return null;

        var isBlock = rawLine.Content.Contains("BLOCK", StringComparison.OrdinalIgnoreCase);

        return new NetworkEvent
        {
            Timestamp      = rawLine.ReceivedAt,
            SourceIp       = match.Groups["src"].Value,
            DestinationIp  = match.Groups["dst"].Value,
            Protocol       = match.Groups["proto"].Value.ToUpper(),
            Port           = int.TryParse(match.Groups["dpt"].Value, out var port) ? port : null,
            Action         = isBlock ? "BLOCK" : "ALLOW",
            Severity       = isBlock ? "WARNING" : "INFO",
            RawData        = rawLine.Content,
            Source         = rawLine.Source,
        };
    }
}
