# NetworkLogAnalyzer — Guide complet du projet

## Contexte
Analyseur de logs réseau avec visualisation en temps réel.
Surveille les fichiers de logs Linux (/var/log, journalctl)
et affiche les événements dans un dashboard React.

## Environnement de développement
- OS          : Arch Linux (EndeavourOS) + KDE Plasma
- Backend     : .NET 9 SDK (global.json force la version 9)
- Frontend    : Node.js + Vite + React
- Éditeur     : Neovim (NvChad) ou Zed
- Shell       : Zsh + Konsole

## Stack technique
- Backend     : ASP.NET Core 9 (C#)
- Frontend    : React 18 + Vite
- BDD         : SQLite (dev) → PostgreSQL (prod)
- ORM         : Entity Framework Core 9
- Temps réel  : SignalR (WebSocket)
- HTTP client : Axios (côté React)
- Graphes     : Recharts

## Ports en développement
- Backend  : http://localhost:5000
- Frontend : http://localhost:5173
- Swagger  : http://localhost:5000/swagger

---

## Structure complète des fichiers

### backend/
backend/
├── global.json                         ← Force .NET 9
├── NetworkLogAnalyzer.sln
└── src/
├── Api/                            ← Projet WebAPI (point d'entrée)
│   ├── Api.csproj
│   ├── Program.cs                  ← DI, CORS, SignalR, Swagger
│   ├── Controllers/
│   │   └── EventsController.cs     ← GET /api/events, GET /api/events/{id}
│   └── Hubs/
│       └── LogHub.cs               ← Hub SignalR → push vers React
│
├── Collector/                      ← Lit les sources de logs
│   ├── Collector.csproj
│   ├── FileLogCollector.cs         ← FileSystemWatcher sur /var/log
│   └── NetworkCollector.cs         ← SharpPcap (paquets réseau live)
│
├── Parser/                         ← Traduit texte brut → objets C#
│   ├── Parser.csproj
│   ├── ILogParser.cs               ← Interface commune à tous les parsers
│   ├── JournalParser.cs            ← Parse lignes journalctl/syslog
│   ├── IptablesParser.cs           ← Parse lignes firewall iptables
│   └── Models/
│       └── RawLogLine.cs           ← Ligne brute avant parsing
│
├── Storage/                        ← Base de données
│   ├── Storage.csproj
│   ├── AppDbContext.cs             ← DbContext Entity Framework
│   ├── Models/
│   │   └── NetworkEvent.cs         ← Modèle principal (voir détail ci-dessous)
│   ├── Repositories/
│   │   ├── IEventRepository.cs     ← Interface
│   │   └── EventRepository.cs      ← Implémentation SQLite
│   └── Migrations/                 ← Générées par "dotnet ef migrations add"
│
└── Analyzer/                       ← Détecte les anomalies
├── Analyzer.csproj
├── AnalyzerService.cs          ← Applique toutes les règles
└── Rules/
├── IDetectionRule.cs       ← Interface commune
├── PortScanRule.cs         ← Même IP, N ports différents en < Xs
└── FloodRule.cs            ← Trop de paquets par seconde

### frontend/
frontend/
├── package.json
├── vite.config.js
└── src/
├── main.jsx
├── App.jsx
├── services/
│   └── api.js                      ← Axios → appels REST backend
├── hooks/
│   ├── useSignalR.js               ← Connexion SignalR temps réel
│   └── useEvents.js                ← Chargement historique événements
└── components/
├── EventTable.jsx              ← Tableau paginé des événements
├── AlertBanner.jsx             ← Bandeau alerte temps réel
└── Charts/
├── TimelineChart.jsx       ← Événements dans le temps
├── TopIpsChart.jsx         ← Bar chart top IPs
└── ProtocolPieChart.jsx    ← Répartition des protocoles
---

## Modèle principal : NetworkEvent

```csharp
public class NetworkEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string SourceIp { get; set; }
    public string? DestinationIp { get; set; }
    public string Protocol { get; set; }    // TCP, UDP, SSH...
    public int? Port { get; set; }
    public string Action { get; set; }      // ALLOW, BLOCK, CONNECT
    public string Severity { get; set; }    // INFO, WARNING, CRITICAL
    public string RawData { get; set; }     // Ligne brute originale
    public string Source { get; set; }      // "journalctl", "iptables", "pcap"
}
```

---

## Dépendances entre projets
Api → Collector, Parser, Storage, Analyzer
Collector → Parser, Storage
Analyzer → Storage
Parser → (aucune dépendance interne)
Storage → (aucune dépendance interne)
---

## Packages NuGet installés

| Projet   | Package                                      | Rôle                  |
|----------|----------------------------------------------|-----------------------|
| Storage  | Microsoft.EntityFrameworkCore                | ORM                   |
| Storage  | Microsoft.EntityFrameworkCore.Sqlite         | Driver SQLite         |
| Storage  | Microsoft.EntityFrameworkCore.Design         | Migrations CLI        |
| Api      | Microsoft.AspNetCore.SignalR                 | Temps réel WebSocket  |
| Api      | Swashbuckle.AspNetCore                       | Swagger UI            |
| Collector| SharpPcap                                    | Capture paquets réseau|

---

## Packages NPM installés (frontend)

| Package                  | Rôle                            |
|--------------------------|---------------------------------|
| @microsoft/signalr       | Connexion temps réel SignalR    |
| axios                    | Appels REST vers le backend     |
| recharts                 | Graphes (timeline, bar, pie)    |
| react-router-dom         | Navigation entre pages          |

---

## Ordre de développement

- [x] 1. Créer la solution et les 5 projets backend
- [x] 2. Relier les dépendances entre projets (dotnet add reference)
- [x] 3. Storage  → NetworkEvent.cs + AppDbContext.cs + SQLite
- [x] 4. Parser   → ILogParser.cs + JournalParser.cs
- [x] 5. Collector → FileLogCollector.cs
- [x] 6. Api      → Program.cs + EventsController.cs
- [x] 7. Frontend → Vite + React + tableau basique
- [x] 8. SignalR  → LogHub.cs + useSignalR.js
- [ ] 9. Analyzer → IDetectionRule + PortScanRule + FloodRule
- [ ] 10. Frontend → Graphes + alertes

---

## Commandes utiles

```bash
# Lancer le backend
cd ~/NetworkLogAnalyzer/backend
dotnet run --project src/Api

# Lancer le frontend
cd ~/NetworkLogAnalyzer/frontend
npm run dev

# Ajouter une migration EF Core
dotnet ef migrations add NomMigration \
  --project src/Storage \
  --startup-project src/Api

# Appliquer la migration (crée logs.db)
dotnet ef database update \
  --project src/Storage \
  --startup-project src/Api

# Builder tout le backend
dotnet build

# Voir les logs en direct sur Linux
journalctl -f
```

---

## Comment reprendre ce projet avec une IA

Colle ce fichier en entier au début de ta conversation, puis dis :
"On en est à l'étape X, on continue."
