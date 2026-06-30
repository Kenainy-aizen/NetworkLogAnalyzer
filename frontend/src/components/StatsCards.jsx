export default function StatsCards({ events }) {
  const total = events.length;
  const critical = events.filter(e => e.severity === 'CRITICAL').length;
  const warning = events.filter(e => e.severity === 'WARNING').length;
  const blocked = events.filter(e => e.action === 'BLOCK').length;

  const cards = [
    { label: 'Total événements', value: total },
    { label: 'Critiques', value: critical },
    { label: 'Avertissements', value: warning },
    { label: 'Bloqués', value: blocked },
  ];

  return (
    <div className="mb-5 grid grid-cols-4 gap-3">
      {cards.map((card) => (
        <div key={card.label} className="rounded border border-zinc-800 bg-zinc-900 p-4">
          <div className="mb-2 text-xs uppercase tracking-wide text-zinc-500">{card.label}</div>
          <div className="text-2xl font-medium text-white">{card.value}</div>
        </div>
      ))}
    </div>
  );
}
