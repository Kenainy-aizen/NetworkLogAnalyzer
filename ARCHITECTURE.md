# NetworkLogAnalyzer — Carte du projet

## Stack
- Backend  : ASP.NET Core 9 (C#)
- Frontend : React + Vite
- BDD      : SQLite (dev) → PostgreSQL (prod)
- Temps réel : SignalR

## Structure backend/src/

### Api/
Point d'entrée du backend.
- Program.cs         → Configuration CORS, SignalR, DI
- Controllers/       → Endpoints REST (GET /api/events, etc.)
- Hubs/LogHub.cs     → Hub SignalR (push temps réel vers React)
STATUS: [ ] À faire

### Collector/
Lit les sources de logs.
- FileLogCollector.cs  → Surveille /var/log via FileSystemWatcher
- NetworkCollector.cs  → Capture paquets via SharpPcap
STATUS: [ ] À faire

### Parser/
Traduit le texte brut en objets C#.
- JournalParser.cs   → Parse les lignes journalctl
- IptablesParser.cs  → Parse les lignes iptables/firewall
- Models/RawLogLine.cs
STATUS: [ ] À faire

### Storage/
Base de données.
- AppDbContext.cs     → Contexte Entity Framework
- Models/NetworkEvent.cs → Modèle principal
- Repositories/      → Accès aux données
- Migrations/        → Générées automatiquement par EF
STATUS: [ ] À faire

### Analyzer/
Détection d'anomalies.
- Rules/IDetectionRule.cs    → Interface commune
- Rules/PortScanRule.cs      → Détecte port scan
- Rules/FloodRule.cs         → Détecte flood/DDoS
- AnalyzerService.cs         → Applique toutes les règles
STATUS: [ ] À faire

## Structure frontend/src/

### components/
- EventTable.jsx     → Tableau des événements
- AlertBanner.jsx    → Alerte temps réel
- Charts/            → Graphes (Recharts)

### hooks/
- useSignalR.js      → Connexion temps réel
- useEvents.js       → Chargement historique

### services/
- api.js             → Appels REST vers le backend

STATUS: [ ] À faire

## Ordre de développement
[x] 1. Créer la solution et les projets
[ ] 2. Storage  → NetworkEvent + AppDbContext + SQLite
[ ] 3. Parser   → JournalParser (lire journalctl)
[ ] 4. Collector → FileLogCollector
[ ] 5. Api      → Program.cs + GET /api/events
[ ] 6. Frontend → Tableau basique
[ ] 7. SignalR  → Temps réel
[ ] 8. Analyzer → Règles de détection
[ ] 9. Frontend → Graphes + alertes

## Ports en développement
- Backend  : http://localhost:5000
- Frontend : http://localhost:5173

## Commandes utiles
# Lancer le backend
cd ~/NetworkLogAnalyzer/backend && dotnet run --project src/Api

# Lancer le frontend
cd ~/NetworkLogAnalyzer/frontend && npm run dev

# Ajouter une migration EF
cd ~/NetworkLogAnalyzer/backend
dotnet ef migrations add NomDeLaMigration --project src/Storage --startup-project src/Api

# Appliquer la migration
dotnet ef database update --project src/Storage --startup-project src/Api
