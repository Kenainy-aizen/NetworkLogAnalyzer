using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public class LogHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
