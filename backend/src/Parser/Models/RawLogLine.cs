namespace Parser.Models;

public class RawLogLine
{
    // La ligne brute complète
    public string Content { get; set; } = string.Empty;

    // D'où elle vient : "journalctl", "iptables", "pcap"
    public string Source { get; set; } = string.Empty;

    // Quand elle a été reçue par le collecteur
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
