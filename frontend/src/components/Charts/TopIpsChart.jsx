import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';

function getTopIps(events, limit = 6) {
  const counts = {};

  events.forEach((event) => {
    if (!event.sourceIp) return;
    counts[event.sourceIp] = (counts[event.sourceIp] || 0) + 1;
  });

  return Object.entries(counts)
    .map(([ip, count]) => ({ ip, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, limit);
}

const COLORS = ['#3b82f6', '#3b82f6', '#3b82f6', '#3b82f6', '#3b82f6', '#3b82f6'];

export default function TopIpsChart({ events }) {
  const data = getTopIps(events);

  if (data.length === 0) {
    return <div className="py-12 text-center text-sm text-zinc-500">Aucune IP à afficher.</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={data} layout="vertical" margin={{ top: 8, right: 16, left: 8, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" horizontal={false} />
        <XAxis type="number" stroke="#71717a" fontSize={11} allowDecimals={false} />
        <YAxis dataKey="ip" type="category" stroke="#71717a" fontSize={11} width={90} />
        <Tooltip
          contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 4, fontSize: 12 }}
          labelStyle={{ color: '#d4d4d8' }}
        />
        <Bar dataKey="count" radius={[0, 4, 4, 0]}>
          {data.map((entry, index) => (
            <Cell key={entry.ip} fill={COLORS[index % COLORS.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
