using System.Collections.Concurrent;
using Storage.Models;

namespace Analyzer.Rules;

public class PortScanRule : IDetectionRule
{
    public string Name => "Port Scan";

    private const int DistinctPortThreshold = 10;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

    private static readonly ConcurrentDictionary<string, DateTime> _lastAlertByIp = new();

    public Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents)
    {
        // On s'intéresse à toute tentative bloquée ayant un port défini
        if (newEvent.Action != "BLOCK" || newEvent.Port is null)
            return Task.FromResult<NetworkEvent?>(null);

        if (string.IsNullOrEmpty(newEvent.SourceIp))
            return Task.FromResult<NetworkEvent?>(null);

        var cutoff = DateTime.UtcNow - Window;

        // Compter les ports DISTINCTS touchés par cette IP dans la fenêtre
        var distinctPorts = recentEvents
            .Where(e =>
                e.Action == "BLOCK" &&
                e.SourceIp == newEvent.SourceIp &&
                e.Port is not null &&
                e.Timestamp >= cutoff)
            .Select(e => e.Port!.Value)
            .Distinct()
            .Count();

        if (distinctPorts < DistinctPortThreshold)
            return Task.FromResult<NetworkEvent?>(null);

        var now = DateTime.UtcNow;
        if (_lastAlertByIp.TryGetValue(newEvent.SourceIp, out var lastAlert))
        {
            if (now - lastAlert < Cooldown)
                return Task.FromResult<NetworkEvent?>(null);
        }

        _lastAlertByIp[newEvent.SourceIp] = now;

        var alert = new NetworkEvent
        {
            Timestamp = now,
            SourceIp  = newEvent.SourceIp,
            Protocol  = "ALERT",
            Action    = "DETECTED",
            Severity  = "CRITICAL",
            Source    = "analyzer",
            RawData   = $"Port scan détecté : {distinctPorts} ports différents touchés par {newEvent.SourceIp} en moins de 30 secondes.",
        };

        return Task.FromResult<NetworkEvent?>(alert);
    }
}
