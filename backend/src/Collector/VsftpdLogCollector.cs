using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parser;
using Parser.Models;
using Storage.Repositories;

namespace Collector;

public class VsftpdLogCollector : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VsftpdLogCollector> _logger;

    private const string LogPath = "/var/log/vsftpd.log";

    public VsftpdLogCollector(
        IServiceScopeFactory scopeFactory,
        ILogger<VsftpdLogCollector> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!File.Exists(LogPath))
        {
            _logger.LogWarning("vsftpd log non trouvé : {Path}. Collector FTP désactivé.", LogPath);
            return;
        }

        try
        {
            using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.End);
            using var reader = new StreamReader(stream);

            _logger.LogInformation("Collector FTP démarré : surveillance de {Path}", LogPath);

            using var scope  = _scopeFactory.CreateScope();
            var repository   = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var parser       = new VsftpdParser();

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
                    Source     = "vsftpd",
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
                "Permission refusée pour lire {Path}. " +
                "Lance : sudo chmod 644 {Path}",
                LogPath, LogPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur dans le Collector vsftpd");
        }
    }
}
