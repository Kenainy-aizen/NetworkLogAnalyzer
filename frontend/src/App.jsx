import { useCallback, useEffect, useState } from 'react';
import { getEvents } from './services/api';
import { useSignalR } from './hooks/useSignalR';
import EventTable from './components/EventTable';
import StatsCards from './components/StatsCards';

function App() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('ALL');

  // Charger l'historique une seule fois au démarrage
  const loadEvents = async () => {
    try {
      const data = await getEvents({ limit: 100 });
      setEvents(data);
      setError(null);
    } catch (err) {
      setError("Impossible de contacter le backend. Vérifie qu'il tourne sur le port 5000.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadEvents();
  }, []);

  // Ajouter un nouvel événement reçu en temps réel, en tête de liste
  const handleNewEvent = useCallback((newEvent) => {
    setEvents((prev) => [newEvent, ...prev].slice(0, 200));
  }, []);

  const connected = useSignalR(handleNewEvent);

  const filteredEvents = filter === 'ALL'
    ? events
    : events.filter(e => e.severity === filter);

  return (
    <div className="min-h-screen bg-zinc-950 px-7 py-5 text-zinc-300">
      <div className="mb-5 flex items-center justify-between">
        <h1 className="text-lg font-medium text-white">Network log analyzer</h1>
        <div className="flex items-center rounded border border-zinc-800 px-2.5 py-1 text-xs text-zinc-400">
          <span className={`mr-1.5 inline-block h-2 w-2 rounded-full ${connected ? 'animate-pulse bg-green-500' : 'bg-red-500'}`}></span>
          {connected ? 'En direct' : 'Déconnecté'}
        </div>
      </div>

      {error && (
        <div className="mb-4 rounded border border-red-500 bg-red-500/10 px-4 py-2.5 text-sm text-red-400">
          {error}
        </div>
      )}

      <StatsCards events={events} />

      <div className="overflow-hidden rounded border border-zinc-800 bg-zinc-900">
        <div className="flex items-center justify-between border-b border-zinc-800 px-4 py-3 text-sm font-medium">
          <span>Événements réseau</span>
          <div className="flex gap-2">
            {['ALL', 'INFO', 'WARNING', 'CRITICAL'].map((f) => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={`rounded border px-2.5 py-1 text-xs transition-colors ${
                  filter === f
                    ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                    : 'border-zinc-800 text-zinc-500 hover:border-zinc-700 hover:text-zinc-300'
                }`}
              >
                {f}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="py-16 text-center text-sm text-zinc-500">Chargement...</div>
        ) : (
          <EventTable events={filteredEvents} />
        )}
      </div>
    </div>
  );
}

export default App;
