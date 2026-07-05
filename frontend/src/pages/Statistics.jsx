import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getStatistics } from '../services/api';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Cell, PieChart, Pie, Legend,
  AreaChart, Area
} from 'recharts';

function StatCard({ label, value, color = 'text-white', sub }) {
  return (
    <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
      <div className="mb-1 text-xs uppercase tracking-wide text-zinc-500">{label}</div>
      <div className={`text-3xl font-medium ${color}`}>{value}</div>
      {sub && <div className="mt-1 text-xs text-zinc-600">{sub}</div>}
    </div>
  );
}

function SectionTitle({ children }) {
  return <h2 className="mb-3 text-sm font-medium text-zinc-300">{children}</h2>;
}

const PROTOCOL_COLORS = {
  'SSH': '#3b82f6', 'SUDO': '#f59e0b', 'ALERT': '#ef4444',
  'HTTP GET': '#10b981', 'HTTP POST': '#8b5cf6', 'UDP': '#06b6d4',
  'TCP': '#6366f1', 'FTP': '#ec4899', 'FAIL2BAN': '#f97316',
  'SU': '#a78bfa', 'LOGIN': '#34d399',
};

const SOURCE_COLORS = {
  'journalctl': '#3b82f6', 'nginx': '#10b981', 'apache': '#f59e0b',
  'firewalld': '#ef4444', 'vsftpd': '#ec4899', 'fail2ban': '#f97316',
  'analyzer': '#8b5cf6', 'pam': '#a78bfa', 'test': '#71717a',
};

function getColor(map, key, index) {
  return map[key] || ['#3b82f6','#10b981','#f59e0b','#ef4444','#8b5cf6'][index % 5];
}

export default function Statistics() {
  const navigate = useNavigate();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getStatistics()
      .then(setStats)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-zinc-950 text-zinc-500 text-sm">
        Chargement des statistiques...
      </div>
    );
  }

  if (!stats) return null;

  const blockRate = stats.totalEvents > 0
    ? Math.round((stats.totalBlocked / stats.totalEvents) * 100)
    : 0;

  return (
    <div className="min-h-screen bg-zinc-950 px-7 py-5 text-zinc-300">

      {/* Header */}
      <div className="mb-6 flex items-center gap-4">
        <button
          onClick={() => navigate('/')}
          className="rounded border border-zinc-800 px-3 py-1.5 text-xs text-zinc-400 hover:border-zinc-600 hover:text-zinc-200 transition-colors"
        >
          ← Retour
        </button>
        <h1 className="text-lg font-medium text-white">Statistiques</h1>
      </div>

      {/* Cartes résumé */}
      <div className="mb-6 grid grid-cols-6 gap-3">
        <StatCard label="Total" value={stats.totalEvents} />
        <StatCard label="Bloqués" value={stats.totalBlocked} color="text-red-400"
          sub={`${blockRate}% du trafic`} />
        <StatCard label="Autorisés" value={stats.totalAllowed} color="text-green-400" />
        <StatCard label="Critiques" value={stats.totalCritical} color="text-red-400" />
        <StatCard label="Avertissements" value={stats.totalWarning} color="text-amber-400" />
        <StatCard label="Informatifs" value={stats.totalInfo} color="text-zinc-400" />
      </div>

      {/* Activité par jour */}
      <div className="mb-4 rounded border border-zinc-800 bg-zinc-900 p-4">
        <SectionTitle>Activité sur 7 jours</SectionTitle>
        <ResponsiveContainer width="100%" height={180}>
          <AreaChart data={stats.eventsByDay} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
            <defs>
              <linearGradient id="colorDay" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.4} />
                <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
            <XAxis dataKey="key" stroke="#71717a" fontSize={11} />
            <YAxis stroke="#71717a" fontSize={11} allowDecimals={false} />
            <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
            <Area type="monotone" dataKey="value" stroke="#3b82f6" fill="url(#colorDay)" name="Événements" />
          </AreaChart>
        </ResponsiveContainer>
      </div>

      {/* Activité par heure */}
      <div className="mb-4 rounded border border-zinc-800 bg-zinc-900 p-4">
        <SectionTitle>Activité par heure de la journée</SectionTitle>
        <ResponsiveContainer width="100%" height={180}>
          <BarChart data={stats.eventsByHour} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
            <XAxis dataKey="key" stroke="#71717a" fontSize={10} />
            <YAxis stroke="#71717a" fontSize={11} allowDecimals={false} />
            <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
            <Bar dataKey="value" fill="#3b82f6" radius={[3, 3, 0, 0]} name="Événements" />
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Ligne : Top IPs + Top Ports */}
      <div className="mb-4 grid grid-cols-2 gap-4">
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <SectionTitle>Top 10 IPs sources</SectionTitle>
          {stats.topSourceIps.length === 0 ? (
            <p className="text-xs text-zinc-600 py-8 text-center">Aucune donnée</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={stats.topSourceIps} layout="vertical"
                margin={{ top: 4, right: 16, left: 8, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#27272a" horizontal={false} />
                <XAxis type="number" stroke="#71717a" fontSize={11} allowDecimals={false} />
                <YAxis dataKey="key" type="category" stroke="#71717a" fontSize={10} width={110} />
                <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
                <Bar dataKey="value" fill="#ef4444" radius={[0, 4, 4, 0]} name="Événements" />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>

        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <SectionTitle>Top 10 ports ciblés</SectionTitle>
          {stats.topPorts.length === 0 ? (
            <p className="text-xs text-zinc-600 py-8 text-center">Aucune donnée</p>
          ) : (
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={stats.topPorts} layout="vertical"
                margin={{ top: 4, right: 16, left: 8, bottom: 4 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#27272a" horizontal={false} />
                <XAxis type="number" stroke="#71717a" fontSize={11} allowDecimals={false} />
                <YAxis dataKey="key" type="category" stroke="#71717a" fontSize={10} width={50} />
                <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
                <Bar dataKey="value" fill="#f59e0b" radius={[0, 4, 4, 0]} name="Événements" />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Ligne : Protocoles + Sources */}
      <div className="mb-4 grid grid-cols-2 gap-4">
        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <SectionTitle>Répartition par protocole</SectionTitle>
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie data={stats.eventsByProtocol} dataKey="value" nameKey="key"
                cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={2}>
                {stats.eventsByProtocol.map((entry, index) => (
                  <Cell key={entry.key} fill={getColor(PROTOCOL_COLORS, entry.key, index)} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
              <Legend iconSize={8} iconType="circle"
                wrapperStyle={{ fontSize: 11, color: '#a1a1aa' }} />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <SectionTitle>Événements par source de log</SectionTitle>
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie data={stats.eventsBySource} dataKey="value" nameKey="key"
                cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={2}>
                {stats.eventsBySource.map((entry, index) => (
                  <Cell key={entry.key} fill={getColor(SOURCE_COLORS, entry.key, index)} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', fontSize: 12 }} />
              <Legend iconSize={8} iconType="circle"
                wrapperStyle={{ fontSize: 11, color: '#a1a1aa' }} />
            </PieChart>
          </ResponsiveContainer>
        </div>
      </div>

    </div>
  );
}
