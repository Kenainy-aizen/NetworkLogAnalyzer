import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

// Regroupe les événements par minute pour la timeline
function bucketByMinute(events) {
  const buckets = {};

  events.forEach((event) => {
    const date = new Date(event.timestamp);
    const key = `${date.getHours().toString().padStart(2, '0')}:${date.getMinutes().toString().padStart(2, '0')}`;
    buckets[key] = (buckets[key] || 0) + 1;
  });

  return Object.entries(buckets)
    .map(([time, count]) => ({ time, count }))
    .sort((a, b) => a.time.localeCompare(b.time));
}

export default function TimelineChart({ events }) {
  const data = bucketByMinute(events);

  if (data.length === 0) {
    return <div className="py-12 text-center text-sm text-zinc-500">Pas assez de données pour la timeline.</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <AreaChart data={data} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="colorCount" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.4} />
            <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#27272a" />
        <XAxis dataKey="time" stroke="#71717a" fontSize={11} />
        <YAxis stroke="#71717a" fontSize={11} allowDecimals={false} />
        <Tooltip
          contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 4, fontSize: 12 }}
          labelStyle={{ color: '#d4d4d8' }}
        />
        <Area type="monotone" dataKey="count" stroke="#3b82f6" fillOpacity={1} fill="url(#colorCount)" />
      </AreaChart>
    </ResponsiveContainer>
  );
}
