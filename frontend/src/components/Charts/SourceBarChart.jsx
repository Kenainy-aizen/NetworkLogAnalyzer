import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';

const SOURCE_COLORS = {
  'journalctl' : '#3b82f6',
  'nginx'      : '#10b981',
  'apache'     : '#f59e0b',
  'firewalld'  : '#ef4444',
  'analyzer'   : '#8b5cf6',
  'test'       : '#71717a',
};

function getSourceData(events) {
  const counts = {};
  events.forEach(e => {
    const key = e.source || 'unknown';
    counts[key] = (counts[key] || 0) + 1;
  });
  return Object.entries(counts)
    .map(([source, count]) => ({ source, count }))
    .sort((a, b) => b.count - a.count);
}

export default function SourceBarChart({ events }) {
  const data = getSourceData(events);

  if (data.length === 0) {
    return <div className="py-12 text-center text-sm text-zinc-500">Aucune donnée.</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={data} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
        <XAxis dataKey="source" stroke="#71717a" fontSize={11} />
        <YAxis stroke="#71717a" fontSize={11} allowDecimals={false} />
        <Tooltip
          contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 4, fontSize: 12 }}
          labelStyle={{ color: '#d4d4d8' }}
        />
        <Bar dataKey="count" radius={[4, 4, 0, 0]}>
          {data.map((entry) => (
            <Cell key={entry.source} fill={SOURCE_COLORS[entry.source] || '#71717a'} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
