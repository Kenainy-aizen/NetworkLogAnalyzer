using System.Collections.Concurrent;
using Storage.Models;

namespace Analyzer.Rules;

public class HttpBruteForceRule : IDetectionRule
{
    public string Name => "HTTP Brute Force";

    private const int Threshold = 10;
    private static readonly TimeSpan Window   = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    private static readonly ConcurrentDictionary<string, DateTime> _lastAlertByIp = new();

    // Chemins typiques de pages de login
    private static readonly HashSet<string> LoginPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/login", "/signin", "/admin/login", "/wp-login.php",
        "/administrator", "/user/login", "/auth/login", "/api/login",
        "/api/auth", "/account/login",
    };

    public Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents)
    {
        // On s'intéresse aux requêtes HTTP bloquées (401, 403 → action BLOCK)
        if (newEvent.Action != "BLOCK") return Task.FromResult<NetworkEvent?>(null);
        if (newEvent.Source != "nginx" && newEvent.Source != "apache")
            return Task.FromResult<NetworkEvent?>(null);

        // Vérifier si la requête cible un chemin de login
        var isLoginPath = LoginPaths.Any(p =>
            newEvent.RawData.Contains(p, StringComparison.OrdinalIgnoreCase));

        if (!isLoginPath) return Task.FromResult<NetworkEvent?>(null);

        var cutoff = DateTime.UtcNow - Window;

        var count = recentEvents.Count(e =>
            e.Action    == "BLOCK" &&
            e.SourceIp  == newEvent.SourceIp &&
            (e.Source   == "nginx" || e.Source == "apache") &&
            e.Timestamp >= cutoff &&
            LoginPaths.Any(p => e.RawData.Contains(p, StringComparison.OrdinalIgnoreCase))
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
            RawData   = $"Brute force HTTP détecté : {count} tentatives sur page de login depuis {newEvent.SourceIp} en moins d'une minute.",
        });
    }
}
