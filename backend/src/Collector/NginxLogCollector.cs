using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Parser;
using Parser.Models;
using Storage.Repositories;

namespace Collector;

public class NginxLogCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NginxLogCollector> _logger;

    private const string LogPath = "/var/log/nginx/access.log";

    public NginxLogCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<NginxLogCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!File.Exists(LogPath))
        {
            _logger.LogWarning("Nginx log non trouvé : {Path}. Collector Nginx désactivé.", LogPath);
            return;
        }

        _logger.LogInformation("Collector Nginx démarré : surveillance de {Path}", LogPath);

        using var scope = _scopeFactory.CreateScope();
        var parsers    = scope.ServiceProvider.GetServices<ILogParser>();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var nginxParser = new NginxParser();

        // Se positionner à la fin du fichier pour ne lire que les nouvelles lignes
        using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(0, SeekOrigin.End);
        using var reader = new StreamReader(stream);

        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(stoppingToken);

            if (line is null)
            {
                // Pas de nouvelle ligne — attendre 500ms avant de re-vérifier
                await Task.Delay(500, stoppingToken);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            var rawLine = new RawLogLine
            {
                Content    = line,
                Source     = "nginx",
                ReceivedAt = DateTime.UtcNow,
            };

            if (nginxParser.CanParse(line))
            {
                var networkEvent = nginxParser.Parse(rawLine);
                if (networkEvent is not null)
                {
                    await repository.AddAsync(networkEvent);
                }
            }
        }
    }
}
