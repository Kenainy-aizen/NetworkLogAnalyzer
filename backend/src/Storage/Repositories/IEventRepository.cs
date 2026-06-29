using Storage.Models;

namespace Storage.Repositories;

public interface IEventRepository
{
    Task AddAsync(NetworkEvent networkEvent);

    Task<IEnumerable<NetworkEvent>> GetAllAsync(
        string? severity = null,
        string? sourceIp = null,
        int limit = 100
    );

    Task<NetworkEvent?> GetByIdAsync(int id);

    Task<Dictionary<string, int>> GetStatsAsync();
}
