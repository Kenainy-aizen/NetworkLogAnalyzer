using Analyzer;
using Storage;
using Storage.Models;

namespace Api;

public class AnalysisTrigger : IAnalysisTrigger
{
    private readonly AnalyzerService _analyzerService;

    public AnalysisTrigger(AnalyzerService analyzerService)
    {
        _analyzerService = analyzerService;
    }

    public async Task TriggerAnalysisAsync(NetworkEvent networkEvent)
    {
        await _analyzerService.AnalyzeAsync(networkEvent);
    }
}
