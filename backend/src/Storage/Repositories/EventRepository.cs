using Microsoft.EntityFrameworkCore;
using Storage.Models;

namespace Storage.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    private readonly INotifier? _notifier;

    public EventRepository(AppDbContext db, INotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task AddAsync(NetworkEvent networkEvent)
    {
        _db.NetworkEvents.Add(networkEvent);
        await _db.SaveChangesAsync();

        if (_notifier is not null)
        {
            await _notifier.NotifyNewEventAsync(networkEvent);
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
