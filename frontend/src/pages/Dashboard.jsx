import { useCallback, useEffect, useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { getEvents } from '../services/api';
import { useSignalR } from '../hooks/useSignalR';
import { useToast } from '../hooks/useToast';
import { exportToCsv } from '../utils/exportCsv';
import EventTable from '../components/EventTable';
import StatsCards from '../components/StatsCards';
import TimelineChart from '../components/Charts/TimelineChart';
import TopIpsChart from '../components/Charts/TopIpsChart';
import ProtocolPieChart from '../components/Charts/ProtocolPieChart';
import HttpStatusChart from '../components/Charts/HttpStatusChart';
import SourceBarChart from '../components/Charts/SourceBarChart';
import DateRangeFilter from '../components/DateRangeFilter';
import Pagination from '../components/Pagination';
import ToastNotification from '../components/ToastNotification';

const PAGE_SIZE = 20;

export default function Dashboard() {
  const navigate = useNavigate();
  const [pagedResult, setPagedResult] = useState(null);
  const [allEvents, setAllEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('ALL');
  const [page, setPage] = useState(1);
  const [dateRange, setDateRange] = useState(null);
  const { toasts, addToast, dismissToast } = useToast();

  const loadEvents = useCallback(async (currentPage, currentFilter) => {
    try {
      const params = { page: currentPage, pageSize: PAGE_SIZE };
      if (currentFilter !== 'ALL') params.severity = currentFilter;

      const data = await getEvents(params);
      setPagedResult(data);
      setError(null);
    } catch (err) {
      setError("Impossible de contacter le backend. Vérifie qu'il tourne sur le port 5000.");
    } finally {
      setLoading(false);
    }
  }, []);

  // Charger tous les événements pour les graphes (sans pagination)
  const loadAllForCharts = useCallback(async () => {
    try {
      const data = await getEvents({ page: 1, pageSize: 500 });
      setAllEvents(data.items || []);
    } catch {}
  }, []);

  useEffect(() => {
    loadEvents(page, filter);
    loadAllForCharts();
  }, [page, filter]);

  const handleNewEvent = useCallback((newEvent) => {
    setAllEvents(prev => [newEvent, ...prev].slice(0, 500));
    if (newEvent.severity === 'CRITICAL') addToast(newEvent);
    // Recharger la page courante pour voir le nouvel événement
    loadEvents(page, filter);
  }, [page, filter, addToast, loadEvents]);

  const connected = useSignalR(handleNewEvent);

  const handleFilterChange = (f) => {
    setFilter(f);
    setPage(1);
  };

  const handlePageChange = (newPage) => {
    setPage(newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Filtre date côté client (sur les graphes uniquement)
  const dateFilteredEvents = useMemo(() => {
    if (!dateRange) return allEvents;
    const cutoff = new Date(Date.now() - dateRange * 60 * 1000);
    return allEvents.filter(e => new Date(e.timestamp) >= cutoff);
  }, [allEvents, dateRange]);

  const events = pagedResult?.items ?? [];
  const totalPages = pagedResult?.totalPages ?? 1;
  const totalCount = pagedResult?.totalCount ?? 0;

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

      <div className="mb-4 flex items-center rounded border border-zinc-800 bg-zinc-900 px-4 py-2.5">
        <DateRangeFilter value={dateRange} onChange={setDateRange} />
      </div>

      <StatsCards events={dateFilteredEvents} />

      <div className="mb-3 grid grid-cols-2 gap-3">
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-sm font-medium">Timeline des événements</div>
          <TimelineChart events={dateFilteredEvents} />
        </div>
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-sm font-medium">Top IPs sources</div>
          <TopIpsChart events={dateFilteredEvents} onIpClick={(ip) => navigate(`/ip/${ip}`)} />
        </div>
      </div>

      <div className="mb-3 grid grid-cols-3 gap-3">
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-sm font-medium">Répartition par protocole</div>
          <ProtocolPieChart events={dateFilteredEvents} />
        </div>
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-sm font-medium">Statuts HTTP</div>
          <HttpStatusChart events={dateFilteredEvents} />
        </div>
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-sm font-medium">Événements par source</div>
          <SourceBarChart events={dateFilteredEvents} />
        </div>
      </div>

      <div className="overflow-hidden rounded border border-zinc-800 bg-zinc-900">
        <div className="flex items-center justify-between border-b border-zinc-800 px-4 py-3 text-sm font-medium">
          <span>
            Événements réseau
            <span className="ml-2 text-xs text-zinc-500">
              ({totalCount} au total)
            </span>
          </span>
          <div className="flex items-center gap-2">
            {['ALL', 'INFO', 'WARNING', 'CRITICAL'].map((f) => (
              <button
                key={f}
                onClick={() => handleFilterChange(f)}
                className={`rounded border px-2.5 py-1 text-xs transition-colors ${
                  filter === f
                    ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                    : 'border-zinc-800 text-zinc-500 hover:border-zinc-700 hover:text-zinc-300'
                }`}
              >
                {f}
              </button>
            ))}
            <button
              onClick={() => exportToCsv(events, `events-page${page}.csv`)}
              disabled={events.length === 0}
              className="rounded border border-zinc-700 bg-zinc-800 px-2.5 py-1 text-xs text-zinc-300 hover:border-zinc-500 hover:text-white transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
            >
              ↓ CSV
            </button>
          </div>
        </div>

        {loading ? (
          <div className="py-16 text-center text-sm text-zinc-500">Chargement...</div>
        ) : (
          <EventTable events={events} onIpClick={(ip) => navigate(`/ip/${ip}`)} />
        )}

        <Pagination
          page={page}
          totalPages={totalPages}
          onPageChange={handlePageChange}
        />
      </div>

      <ToastNotification toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}
