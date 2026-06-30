using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    private readonly INotifier? _notifier;
    private readonly IAnalysisTrigger? _analysisTrigger;

    public EventRepository(
        AppDbContext db,
        INotifier? notifier = null,
        IAnalysisTrigger? analysisTrigger = null)
    {
        _db = db;
        _notifier = notifier;
        _analysisTrigger = analysisTrigger;
    }

    public async Task AddAsync(NetworkEvent networkEvent)
    {
        _db.NetworkEvents.Add(networkEvent);
        await _db.SaveChangesAsync();

        if (_notifier is not null)
        {
            await _notifier.NotifyNewEventAsync(networkEvent);
        }

        // On n'analyse pas les alertes elles-mêmes, pour éviter une boucle infinie
        if (_analysisTrigger is not null && networkEvent.Source != "analyzer")
        {
            await _analysisTrigger.TriggerAnalysisAsync(networkEvent);
        }
    }

    public async Task<IEnumerable<NetworkEvent>> GetAllAsync(
        string? severity = null,
        string? sourceIp = null,
        int limit = 100)
    {
        var query = _db.NetworkEvents.AsQueryable();

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(e => e.Severity == severity);

        if (!string.IsNullOrEmpty(sourceIp))
            query = query.Where(e => e.SourceIp == sourceIp);

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<NetworkEvent?> GetByIdAsync(int id)
    {
        return await _db.NetworkEvents.FindAsync(id);
    }

    public async Task<Dictionary<string, int>> GetStatsAsync()
    {
        return await _db.NetworkEvents
            .GroupBy(e => e.Severity)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count()
            );
    }
}
