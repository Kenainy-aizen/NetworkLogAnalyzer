using Storage.Models;

namespace Storage.Repositories;

public interface IEventRepository
{
    Task AddAsync(NetworkEvent networkEvent);

    Task<PagedResult<NetworkEvent>> GetAllAsync(
        string? severity = null,
        string? sourceIp = null,
        int page = 1,
        int pageSize = 20
    );

    Task<NetworkEvent?> GetByIdAsync(int id);

    Task<Dictionary<string, int>> GetStatsAsync();
}
