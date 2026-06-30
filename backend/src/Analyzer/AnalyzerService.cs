using Analyzer.Rules;
using Storage.Models;
using Storage.Repositories;

namespace Analyzer;

public class AnalyzerService
{
    private readonly IEnumerable<IDetectionRule> _rules;
    private readonly IEventRepository _repository;

    public AnalyzerService(IEnumerable<IDetectionRule> rules, IEventRepository repository)
    {
        _rules = rules;
        _repository = repository;
    }

    // Appelé après chaque nouvel événement sauvegardé
    public async Task AnalyzeAsync(NetworkEvent newEvent)
    {
        // On récupère les 200 derniers événements pour avoir du contexte
        var recentEvents = await _repository.GetAllAsync(limit: 200);

        foreach (var rule in _rules)
        {
            var alert = await rule.EvaluateAsync(newEvent, recentEvents);
            if (alert is not null)
            {
                await _repository.AddAsync(alert);
            }
        }
    }
}
