using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parser;
using Parser.Models;
using Storage.Repositories;

namespace Collector;

public class Fail2banLogCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Fail2banLogCollector> _logger;

    private const string LogPath = "/var/log/fail2ban.log";

    public Fail2banLogCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<Fail2banLogCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!File.Exists(LogPath))
        {
            _logger.LogWarning("fail2ban log non trouvé : {Path}. Collector désactivé.", LogPath);
            return;
        }

        try
        {
            using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.End);
            using var reader = new StreamReader(stream);

            _logger.LogInformation("Collector Fail2ban démarré : surveillance de {Path}", LogPath);

            using var scope = _scopeFactory.CreateScope();
            var repository  = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var parser      = new Fail2banParser();

            while (!stoppingToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(stoppingToken);

                if (line is null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                var rawLine = new RawLogLine
                {
                    Content    = line,
                    Source     = "fail2ban",
                    ReceivedAt = DateTime.UtcNow,
                };

                if (parser.CanParse(line))
                {
                    var networkEvent = parser.Parse(rawLine);
                    if (networkEvent is not null)
                        await repository.AddAsync(networkEvent);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning(
                "Permission refusée pour lire {Path}. Lance : sudo chmod 644 {Path}",
                LogPath, LogPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans le Collector Fail2ban");
        }
    }
}
