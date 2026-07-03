import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';

const COLORS = {
  'SSH':       '#3b82f6',
  'SUDO':      '#f59e0b',
  'ALERT':     '#ef4444',
  'HTTP GET':  '#10b981',
  'HTTP POST': '#8b5cf6',
  'UDP':       '#06b6d4',
  'TCP':       '#6366f1',
  'ICMP':      '#ec4899',
};

function getDefaultColor(index) {
  const defaults = ['#3b82f6','#f59e0b','#ef4444','#10b981','#8b5cf6','#06b6d4'];
  return defaults[index % defaults.length];
}

function getProtocolData(events) {
  const counts = {};
  events.forEach(e => {
    const key = e.protocol || 'UNKNOWN';
    counts[key] = (counts[key] || 0) + 1;
  });
  return Object.entries(counts)
    .map(([name, value]) => ({ name, value }))
    .sort((a, b) => b.value - a.value);
}

export default function ProtocolPieChart({ events }) {
  const data = getProtocolData(events);

  if (data.length === 0) {
    return <div className="py-12 text-center text-sm text-zinc-500">Aucune donnée.</div>;
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <PieChart>
        <Pie
          data={data}
          cx="50%"
          cy="50%"
          innerRadius={50}
          outerRadius={80}
          paddingAngle={2}
          dataKey="value"
        >
          {data.map((entry, index) => (
            <Cell
              key={entry.name}
              fill={COLORS[entry.name] || getDefaultColor(index)}
            />
          ))}
        </Pie>
        <Tooltip
          contentStyle={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 4, fontSize: 12 }}
          labelStyle={{ color: '#d4d4d8' }}
        />
        <Legend
          iconSize={8}
          iconType="circle"
          wrapperStyle={{ fontSize: 11, color: '#a1a1aa' }}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}
