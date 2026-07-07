using Api.Hubs;
using Api.Services;
using Microsoft.AspNetCore.SignalR;
using Storage;
using Storage.Models;

namespace Api;

public class SignalRNotifier : INotifier
{
    private readonly IHubContext<LogHub> _hubContext;
    private readonly IEmailService _emailService;

    public SignalRNotifier(IHubContext<LogHub> hubContext, IEmailService emailService)
    {
        _hubContext   = hubContext;
        _emailService = emailService;
    }

    public async Task NotifyNewEventAsync(NetworkEvent networkEvent)
    {
        // Push SignalR vers le frontend
        await _hubContext.Clients.All.SendAsync("NewEvent", networkEvent);

        // Email si alerte CRITICAL
        if (networkEvent.Severity == "CRITICAL" && networkEvent.Source == "analyzer")
        {
            await _emailService.SendAlertAsync(
                subject: $"Alerte détectée depuis {networkEvent.SourceIp}",
                body: networkEvent.RawData
            );
        }
    }
}
