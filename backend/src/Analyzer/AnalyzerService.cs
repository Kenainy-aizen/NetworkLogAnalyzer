using Analyzer.Rules;
using Microsoft.EntityFrameworkCore;
using Storage;
using Storage.Models;

namespace Analyzer;

public class AnalyzerService
{
    private readonly IEnumerable<IDetectionRule> _rules;
    private readonly AppDbContext _db;
    private readonly INotifier? _notifier;

    public AnalyzerService(IEnumerable<IDetectionRule> rules, AppDbContext db, INotifier? notifier = null)
    {
        _rules = rules;
        _db = db;
        _notifier = notifier;
    }

    // Appelé après chaque nouvel événement sauvegardé
    public async Task AnalyzeAsync(NetworkEvent newEvent)
    {
        // Lecture directe en base, sans passer par le repository
        // (évite la dépendance circulaire avec IAnalysisTrigger)
        var recentEvents = await _db.NetworkEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(200)
            .ToListAsync();

        foreach (var rule in _rules)
        {
            var alert = await rule.EvaluateAsync(newEvent, recentEvents);
            if (alert is not null)
            {
                _db.NetworkEvents.Add(alert);
                await _db.SaveChangesAsync();

                if (_notifier is not null)
                {
                    await _notifier.NotifyNewEventAsync(alert);
                }
            }
        }
    }
}
