using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parser;
using Storage.Repositories;

namespace Collector;

public class CollectorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CollectorBackgroundService> _logger;

    public CollectorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CollectorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Collector démarré : écoute de journalctl en direct...");

        using var scope = _scopeFactory.CreateScope();
        var parsers = scope.ServiceProvider.GetServices<ILogParser>();
        var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        var collector = new JournalctlCollector(parsers, repository);

        try
        {
            await collector.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans le collector");
        }
    }
}
