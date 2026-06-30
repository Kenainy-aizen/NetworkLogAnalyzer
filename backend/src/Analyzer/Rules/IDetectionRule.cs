using Storage.Models;

namespace Analyzer.Rules;

public interface IDetectionRule
{
    // Nom de la règle, pour les logs et l'affichage
    string Name { get; }

    // Examine un nouvel événement et l'historique récent,
    // retourne une alerte si une anomalie est détectée
    Task<NetworkEvent?> EvaluateAsync(NetworkEvent newEvent, IEnumerable<NetworkEvent> recentEvents);
}
