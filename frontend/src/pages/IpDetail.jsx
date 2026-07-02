import { exportToCsv } from "../utils/exportCsv";
import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getEventsByIp } from "../services/api";
import TimelineChart from "../components/Charts/TimelineChart";
import EventTable from "../components/EventTable";

function StatCard({ label, value, color = "text-white" }) {
  return (
    <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
      <div className="mb-2 text-xs uppercase tracking-wide text-zinc-500">
        {label}
      </div>
      <div className={`text-2xl font-medium ${color}`}>{value}</div>
    </div>
  );
}

export default function IpDetail() {
  const { ip } = useParams();
  const navigate = useNavigate();
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("ALL");

  useEffect(() => {
    getEventsByIp(ip)
      .then(setEvents)
      .finally(() => setLoading(false));
  }, [ip]);

  const total = events.length;
  const blocked = events.filter((e) => e.action === "BLOCK").length;
  const critical = events.filter((e) => e.severity === "CRITICAL").length;
  const protocols = [...new Set(events.map((e) => e.protocol))].join(", ");

  const firstSeen = events.length
    ? new Date(events[events.length - 1].timestamp).toLocaleString()
    : "—";

  const lastSeen = events.length
    ? new Date(events[0].timestamp).toLocaleString()
    : "—";

  const filteredEvents =
    filter === "ALL" ? events : events.filter((e) => e.severity === filter);

  return (
    <div className="min-h-screen bg-zinc-950 px-7 py-5 text-zinc-300">
      {/* Header */}
      <div className="mb-5 flex items-center gap-4">
        <button
          onClick={() => navigate("/")}
          className="rounded border border-zinc-800 px-3 py-1.5 text-xs text-zinc-400 hover:border-zinc-600 hover:text-zinc-200 transition-colors"
        >
          ← Retour
        </button>
        <button
          onClick={() => exportToCsv(events, `ip-${ip}.csv`)}
          className="rounded border border-zinc-700 bg-zinc-800 px-3 py-1.5 text-xs text-zinc-300 hover:border-zinc-500 transition-colors"
        >
          ↓ CSV
        </button>
        <div>
          <h1 className="font-mono text-xl font-medium text-white">
            {ip === "localhost" ? "localhost (machine locale)" : ip}
          </h1>
          <p className="text-xs text-zinc-500">
            Historique complet de cette adresse IP
          </p>
        </div>
      </div>

      {loading ? (
        <div className="py-16 text-center text-sm text-zinc-500">
          Chargement...
        </div>
      ) : events.length === 0 ? (
        <div className="py-16 text-center text-sm text-zinc-500">
          Aucun événement trouvé pour cette IP.
        </div>
      ) : (
        <>
          {/* Stats */}
          <div className="mb-4 grid grid-cols-5 gap-3">
            <StatCard label="Total événements" value={total} />
            <StatCard label="Bloqués" value={blocked} color="text-amber-400" />
            <StatCard
              label="Alertes critiques"
              value={critical}
              color="text-red-400"
            />
            <StatCard label="Protocoles" value={protocols || "—"} />
            <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
              <div className="mb-1 text-xs uppercase tracking-wide text-zinc-500">
                Première vue
              </div>
              <div className="text-sm text-zinc-300">{firstSeen}</div>
              <div className="mt-2 mb-1 text-xs uppercase tracking-wide text-zinc-500">
                Dernière vue
              </div>
              <div className="text-sm text-zinc-300">{lastSeen}</div>
            </div>
          </div>

          {/* Timeline */}
          <div className="mb-4 rounded border border-zinc-800 bg-zinc-900 p-4">
            <div className="mb-3 text-sm font-medium">Timeline d'activité</div>
            <TimelineChart events={events} />
          </div>

          {/* Tableau */}
          <div className="overflow-hidden rounded border border-zinc-800 bg-zinc-900">
            <div className="flex items-center justify-between border-b border-zinc-800 px-4 py-3 text-sm font-medium">
              <span>Événements</span>
              <div className="flex gap-2">
                {["ALL", "INFO", "WARNING", "CRITICAL"].map((f) => (
                  <button
                    key={f}
                    onClick={() => setFilter(f)}
                    className={`rounded border px-2.5 py-1 text-xs transition-colors ${
                      filter === f
                        ? "border-blue-500 bg-blue-500/10 text-blue-400"
                        : "border-zinc-800 text-zinc-500 hover:border-zinc-700 hover:text-zinc-300"
                    }`}
                  >
                    {f}
                  </button>
                ))}
              </div>
            </div>
            <EventTable events={filteredEvents} />
          </div>
        </>
      )}
    </div>
  );
}
