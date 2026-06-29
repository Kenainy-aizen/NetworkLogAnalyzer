using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public class LogHub : Hub
{
    // Le hub reçoit les connexions React
    // Les événements sont envoyés depuis les services via IHubContext
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
