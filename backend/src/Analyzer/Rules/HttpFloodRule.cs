using System.Collections.Concurrent;
using Storage.Models;

namespace Analyzer.Rules;

public class HttpFloodRule : IDetectionRule
{
    public string Name => "HTTP Flood";

    private const int Threshold = 100; // requêtes en 30 secondes
    private static readonly TimeSpan Window   = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    private static readonly ConcurrentDictionary<string, DateTime> _lastAlertByIp = new();

    public Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents)
    {
        // Uniquement les événements HTTP
        if (newEvent.Source != "nginx" && newEvent.Source != "apache")
            return Task.FromResult<NetworkEvent?>(null);

        if (string.IsNullOrEmpty(newEvent.SourceIp))
            return Task.FromResult<NetworkEvent?>(null);

        var cutoff = DateTime.UtcNow - Window;

        // Compter toutes les requêtes (pas juste les bloquées) de cette IP
        var count = recentEvents.Count(e =>
            e.SourceIp  == newEvent.SourceIp &&
            (e.Source   == "nginx" || e.Source == "apache") &&
            e.Timestamp >= cutoff
        );

        if (count < Threshold) return Task.FromResult<NetworkEvent?>(null);

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
            RawData   = $"HTTP Flood détecté : {count} requêtes depuis {newEvent.SourceIp} en moins de 30 secondes.",
        });
    }
}
