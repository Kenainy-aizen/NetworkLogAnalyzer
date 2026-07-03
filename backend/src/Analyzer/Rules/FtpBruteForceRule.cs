using System.Collections.Concurrent;
using Storage.Models;

namespace Analyzer.Rules;

public class FtpBruteForceRule : IDetectionRule
{
    public string Name => "FTP Brute Force";

    private const int Threshold = 5;
    private static readonly TimeSpan Window   = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    private static readonly ConcurrentDictionary<string, DateTime> _lastAlertByIp = new();

    public Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents)
    {
        if (newEvent.Protocol != "FTP" || newEvent.Action != "BLOCK")
            return Task.FromResult<NetworkEvent?>(null);

        var cutoff = DateTime.UtcNow - Window;

        var count = recentEvents.Count(e =>
            e.Protocol == "FTP" &&
            e.Action   == "BLOCK" &&
            e.SourceIp == newEvent.SourceIp &&
            e.Timestamp >= cutoff
        );

        if (count < Threshold)
            return Task.FromResult<NetworkEvent?>(null);

        var now = DateTime.UtcNow;
        if (_lastAlertByIp.TryGetValue(newEvent.SourceIp, out var last) && now - last < Cooldown)
            return Task.FromResult<NetworkEvent?>(null);

        _lastAlertByIp[newEvent.SourceIp] = now;

        return Task.FromResult<NetworkEvent?>(new NetworkEvent
        {
            Timestamp = now,
            SourceIp  = newEvent.SourceIp,
            Protocol  = "ALERT",
            Action    = "DETECTED",
            Severity  = "CRITICAL",
            Source    = "analyzer",
            RawData   = $"Brute force FTP détecté : {count} tentatives depuis {newEvent.SourceIp} en moins d'une minute.",
        });
    }
}
