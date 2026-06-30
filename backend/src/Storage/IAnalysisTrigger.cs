using Storage.Models;

namespace Storage;

public interface IAnalysisTrigger
{
    Task TriggerAnalysisAsync(NetworkEvent networkEvent);
}
