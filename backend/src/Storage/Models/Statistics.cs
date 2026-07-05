namespace Storage.Models;

public class Statistics
{
    public int TotalEvents { get; set; }
    public int TotalBlocked { get; set; }
    public int TotalAllowed { get; set; }
    public int TotalCritical { get; set; }
    public int TotalWarning { get; set; }
    public int TotalInfo { get; set; }

    public List<KeyValuePair<string, int>> TopSourceIps { get; set; } = [];
    public List<KeyValuePair<string, int>> TopPorts { get; set; } = [];
    public List<KeyValuePair<string, int>> EventsByProtocol { get; set; } = [];
    public List<KeyValuePair<string, int>> EventsBySource { get; set; } = [];
    public List<KeyValuePair<string, int>> EventsByHour { get; set; } = [];
    public List<KeyValuePair<string, int>> EventsByDay { get; set; } = [];
}
