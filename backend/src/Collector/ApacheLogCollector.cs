using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Parser;
using Parser.Models;
using Storage.Repositories;

namespace Collector;

public class ApacheLogCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApacheLogCollector> _logger;

    private const string LogPath = "/var/log/httpd/access_log";

    public ApacheLogCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<ApacheLogCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!File.Exists(LogPath))
        {
            _logger.LogWarning("Apache log non trouvé : {Path}. Collector Apache désactivé.", LogPath);
            return;
        }

        _logger.LogInformation("Collector Apache démarré : surveillance de {Path}", LogPath);

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var apacheParser = new ApacheParser();

        using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(0, SeekOrigin.End);
        using var reader = new StreamReader(stream);

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
                Source     = "apache",
                ReceivedAt = DateTime.UtcNow,
            };

            if (apacheParser.CanParse(line))
            {
                var networkEvent = apacheParser.Parse(rawLine);
                if (networkEvent is not null)
                    await repository.AddAsync(networkEvent);
            }
        }
    }
}
