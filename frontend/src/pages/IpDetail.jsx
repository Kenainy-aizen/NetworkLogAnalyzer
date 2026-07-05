import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getEventsByIp } from '../services/api';
import { exportToCsv } from '../utils/exportCsv';
import TimelineChart from '../components/Charts/TimelineChart';
import EventTable from '../components/EventTable';
import Pagination from '../components/Pagination';

const PAGE_SIZE = 20;

function StatCard({ label, value, color = 'text-white' }) {
  return (
    <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
      <div className="mb-2 text-xs uppercase tracking-wide text-zinc-500">{label}</div>
      <div className={`text-2xl font-medium ${color}`}>{value}</div>
    </div>
  );
}

export default function IpDetail() {
  const { ip } = useParams();
  const navigate = useNavigate();
  const [pagedResult, setPagedResult] = useState(null);
  const [allEvents, setAllEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('ALL');
  const [page, setPage] = useState(1);

  const loadPage = async (currentPage, currentFilter) => {
    try {
      const params = { page: currentPage, pageSize: PAGE_SIZE };
      if (currentFilter !== 'ALL') params.severity = currentFilter;
      const data = await getEventsByIp(ip, currentPage, PAGE_SIZE);
      setPagedResult(data);
    } finally {
      setLoading(false);
    }
  };

  const loadAll = async () => {
    const data = await getEventsByIp(ip, 1, 500);
    setAllEvents(data.items || []);
  };

  useEffect(() => {
    loadPage(page, filter);
    loadAll();
  }, [ip, page, filter]);

  const events     = pagedResult?.items ?? [];
  const totalPages = pagedResult?.totalPages ?? 1;
  const totalCount = pagedResult?.totalCount ?? 0;

  const blocked  = allEvents.filter(e => e.action === 'BLOCK').length;
  const critical = allEvents.filter(e => e.severity === 'CRITICAL').length;
  const protocols = [...new Set(allEvents.map(e => e.protocol))].join(', ');
  const firstSeen = allEvents.length ? new Date(allEvents[allEvents.length - 1].timestamp).toLocaleString() : '—';
  const lastSeen  = allEvents.length ? new Date(allEvents[0].timestamp).toLocaleString() : '—';

  return (
    <div className="min-h-screen bg-zinc-950 px-7 py-5 text-zinc-300">
      <div className="mb-5 flex items-center gap-4">
        <button
          onClick={() => navigate('/')}
          className="rounded border border-zinc-800 px-3 py-1.5 text-xs text-zinc-400 hover:border-zinc-600 hover:text-zinc-200 transition-colors"
        >
          ← Retour
        </button>
        <button
          onClick={() => exportToCsv(allEvents, `ip-${ip}.csv`)}
          className="rounded border border-zinc-700 bg-zinc-800 px-3 py-1.5 text-xs text-zinc-300 hover:border-zinc-500 transition-colors"
        >
          ↓ CSV
        </button>
        <div>
          <h1 className="font-mono text-xl font-medium text-white">
            {ip === 'localhost' ? 'localhost (machine locale)' : ip}
          </h1>
          <p className="text-xs text-zinc-500">Historique complet · {totalCount} événements</p>
        </div>
      </div>

      {loading ? (
        <div className="py-16 text-center text-sm text-zinc-500">Chargement...</div>
      ) : totalCount === 0 ? (
        <div className="py-16 text-center text-sm text-zinc-500">Aucun événement trouvé.</div>
      ) : (
        <>
          <div className="mb-4 grid grid-cols-5 gap-3">
            <StatCard label="Total" value={totalCount} />
            <StatCard label="Bloqués" value={blocked} color="text-amber-400" />
            <StatCard label="Critiques" value={critical} color="text-red-400" />
            <StatCard label="Protocoles" value={protocols || '—'} />
            <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
              <div className="mb-1 text-xs uppercase tracking-wide text-zinc-500">Première vue</div>
              <div className="text-sm text-zinc-300">{firstSeen}</div>
              <div className="mt-2 mb-1 text-xs uppercase tracking-wide text-zinc-500">Dernière vue</div>
              <div className="text-sm text-zinc-300">{lastSeen}</div>
            </div>
          </div>

          <div className="mb-4 rounded border border-zinc-800 bg-zinc-900 p-4">
            <div className="mb-3 text-sm font-medium">Timeline d'activité</div>
            <TimelineChart events={allEvents} />
          </div>

          <div className="overflow-hidden rounded border border-zinc-800 bg-zinc-900">
            <div className="flex items-center justify-between border-b border-zinc-800 px-4 py-3 text-sm font-medium">
              <span>Événements</span>
              <div className="flex gap-2">
                {['ALL', 'INFO', 'WARNING', 'CRITICAL'].map((f) => (
                  <button
                    key={f}
                    onClick={() => { setFilter(f); setPage(1); }}
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
            <EventTable events={events} />
            <Pagination
              page={page}
              totalPages={totalPages}
              onPageChange={setPage}
            />
          </div>
        </>
      )}
    </div>
  );
}
