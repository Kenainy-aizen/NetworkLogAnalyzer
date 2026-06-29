namespace Storage.Models;

public class NetworkEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SourceIp { get; set; } = string.Empty;
    public string? DestinationIp { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Severity { get; set; } = "INFO";
    public string RawData { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
