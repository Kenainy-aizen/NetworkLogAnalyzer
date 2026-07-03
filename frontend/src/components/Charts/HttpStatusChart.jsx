import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';

function getStatusData(events) {
  const counts = {};

  events
    .filter(e => e.source === 'nginx' || e.source === 'apache')
    .forEach(e => {
      // Extraire le code HTTP depuis rawData
      const match = e.rawData?.match(/"[A-Z]+ \S+ HTTP\/[\d.]+" (\d+)/);
      if (!match) return;
      const status = match[1];
      counts[status] = (counts[status] || 0) + 1;
    });

  return Object.entries(counts)
    .map(([status, count]) => ({ status, count }))
    .sort((a, b) => a.status.localeCompare(b.status));
}

function statusColor(status) {
  if (status.startsWith('2')) return '#10b981';
  if (status.startsWith('3')) return '#3b82f6';
  if (status.startsWith('4')) return '#f59e0b';
  if (status.startsWith('5')) return '#ef4444';
  return '#71717a';
}

export default function HttpStatusChart({ events }) {
  const data = getStatusData(events);

  if (data.length === 0) {
    return <div className="py-12 text-center text-sm text-zinc-500">Aucune donnée HTTP.</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={data} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
        <XAxis dataKey="status" stroke="#71717a" fontSize={11} />
        <YAxis stroke="#71717a" fontSize={11} allowDecimals={false} />
        <Tooltip
          contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 4, fontSize: 12 }}
          labelStyle={{ color: '#d4d4d8' }}
        />
        <Bar dataKey="count" radius={[4, 4, 0, 0]}>
          {data.map((entry) => (
            <Cell key={entry.status} fill={statusColor(entry.status)} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
