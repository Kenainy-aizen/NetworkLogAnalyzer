using Storage.Models;

namespace Analyzer.Rules;

public class SshBruteForceRule : IDetectionRule
{
    public string Name => "SSH Brute Force";

    private const int Threshold = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents)
    {
        // On ne s'intéresse qu'aux tentatives SSH bloquées
        if (newEvent.Protocol != "SSH" || newEvent.Action != "BLOCK")
            return Task.FromResult<NetworkEvent?>(null);

        var cutoff = DateTime.UtcNow - Window;

        // Compter les tentatives échouées de la même IP dans la fenêtre de temps
        var attemptsCount = recentEvents.Count(e =>
            e.Protocol == "SSH" &&
            e.Action == "BLOCK" &&
            e.SourceIp == newEvent.SourceIp &&
            e.Timestamp >= cutoff
        );

        if (attemptsCount >= Threshold)
        {
            var alert = new NetworkEvent
            {
                Timestamp     = DateTime.UtcNow,
                SourceIp      = newEvent.SourceIp,
                Protocol      = "ALERT",
                Action        = "DETECTED",
                Severity      = "CRITICAL",
                Source        = "analyzer",
                RawData       = $"Brute force SSH détecté : {attemptsCount} tentatives depuis {newEvent.SourceIp} en moins d'une minute.",
            };

            return Task.FromResult<NetworkEvent?>(alert);
        }

        return Task.FromResult<NetworkEvent?>(null);
    }
}
