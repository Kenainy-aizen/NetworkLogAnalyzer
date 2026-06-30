using Storage.Models;

namespace Storage;

public interface INotifier
{
    Task NotifyNewEventAsync(NetworkEvent networkEvent);
}
