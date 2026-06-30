using System.Diagnostics;
using Parser;
using Parser.Models;
using Storage.Repositories;

namespace Collector;

public class JournalctlCollector
{
    private readonly IEnumerable<ILogParser> _parsers;
    private readonly IEventRepository _repository;
    private Process? _process;

    public JournalctlCollector(IEnumerable<ILogParser> parsers, IEventRepository repository)
    {
        _parsers = parsers;
        _repository = repository;
    }

    // Démarre l'écoute en continu de journalctl
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "journalctl",
            // -f = suivre en direct, -n 0 = ne pas relire l'historique
            Arguments = "-f -n 0 --no-pager",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = Process.Start(startInfo);
        if (_process is null)
        {
            throw new InvalidOperationException("Impossible de démarrer journalctl");
        }

        // Lire chaque ligne au fur et à mesure qu'elle arrive
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            await ProcessLineAsync(line);
        }
    }

    // Traite une ligne : trouve le bon parser, sauvegarde le résultat
    private async Task ProcessLineAsync(string line)
    {
        var rawLine = new RawLogLine
        {
            Content = line,
            Source = "journalctl",
            ReceivedAt = DateTime.UtcNow
        };

        foreach (var parser in _parsers)
        {
            if (!parser.CanParse(line)) continue;

            var networkEvent = parser.Parse(rawLine);
            if (networkEvent is not null)
            {
                await _repository.AddAsync(networkEvent);
            }
            break; // Une ligne n'est traitée que par un seul parser
        }
    }

    public void Stop()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
        }
    }
}
