using Microsoft.AspNetCore.SignalR;
using Api.Hubs;
using Storage;
using Storage.Models;

namespace Api;

public class SignalRNotifier : INotifier
{
    private readonly IHubContext<LogHub> _hubContext;

    public SignalRNotifier(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewEventAsync(NetworkEvent networkEvent)
    {
        await _hubContext.Clients.All.SendAsync("NewEvent", networkEvent);
    }
}
