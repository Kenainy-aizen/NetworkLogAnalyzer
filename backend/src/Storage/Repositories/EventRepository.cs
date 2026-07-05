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
            await _notifier.NotifyNewEventAsync(networkEvent);

        if (_analysisTrigger is not null && networkEvent.Source != "analyzer")
            await _analysisTrigger.TriggerAnalysisAsync(networkEvent);
    }

    public async Task<PagedResult<NetworkEvent>> GetAllAsync(
        string? severity = null,
        string? sourceIp = null,
        string? search   = null,
        int page         = 1,
        int pageSize     = 20)
    {
        var query = _db.NetworkEvents.AsQueryable();

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(e => e.Severity == severity);

        if (!string.IsNullOrEmpty(sourceIp))
        {
            if (sourceIp == "localhost")
                query = query.Where(e =>
                    e.SourceIp == "" ||
                    e.SourceIp == "localhost" ||
                    e.SourceIp == "127.0.0.1" ||
                    e.SourceIp == "::1");
            else
                query = query.Where(e => e.SourceIp == sourceIp);
        }

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(e =>
                e.RawData.ToLower().Contains(s)     ||
                e.SourceIp.ToLower().Contains(s)    ||
                e.Protocol.ToLower().Contains(s)    ||
                e.Action.ToLower().Contains(s)      ||
                e.Source.ToLower().Contains(s)
            );
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<NetworkEvent>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<NetworkEvent?> GetByIdAsync(int id)
    {
        return await _db.NetworkEvents.FindAsync(id);
    }

    public async Task<Dictionary<string, int>> GetStatsAsync()
    {
        var events = await _db.NetworkEvents.ToListAsync();
        return events
            .GroupBy(e => e.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
