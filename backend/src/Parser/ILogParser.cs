using Parser.Models;
using Storage.Models;

namespace Parser;

public interface ILogParser
{
    // Vérifie si ce parser sait traiter cette ligne
    bool CanParse(string line);

    // Transforme la ligne brute en NetworkEvent
    NetworkEvent? Parse(RawLogLine rawLine);
}
