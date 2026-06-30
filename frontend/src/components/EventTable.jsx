function severityBadgeClass(severity) {
  switch (severity) {
    case 'CRITICAL': return 'bg-red-500/10 text-red-400';
    case 'WARNING':  return 'bg-amber-500/10 text-amber-400';
    default:         return 'bg-green-500/10 text-green-400';
  }
}

function actionClass(action) {
  return action === 'BLOCK' ? 'text-red-400' : 'text-green-400';
}

export default function EventTable({ events }) {
  if (events.length === 0) {
    return (
      <div className="py-16 text-center text-sm text-zinc-500">
        Aucun événement pour le moment. En attente de logs...
      </div>
    );
  }

  return (
    <table className="w-full text-sm">
      <thead>
        <tr className="border-b border-zinc-800">
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Heure</th>
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Source IP</th>
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Protocole</th>
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Port</th>
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Action</th>
          <th className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">Sévérité</th>
        </tr>
      </thead>
      <tbody>
        {events.map((event) => (
          <tr key={event.id} className="border-b border-zinc-900 hover:bg-zinc-900">
            <td className="px-4 py-2 font-mono text-xs text-zinc-300">
              {new Date(event.timestamp).toLocaleTimeString()}
            </td>
            <td className="px-4 py-2 font-mono text-xs text-zinc-300">{event.sourceIp || '—'}</td>
            <td className="px-4 py-2 text-zinc-300">{event.protocol}</td>
            <td className="px-4 py-2 font-mono text-xs text-zinc-300">{event.port ?? '—'}</td>
            <td className={`px-4 py-2 font-medium ${actionClass(event.action)}`}>{event.action}</td>
            <td className="px-4 py-2">
              <span className={`rounded px-2 py-0.5 text-xs font-medium uppercase ${severityBadgeClass(event.severity)}`}>
                {event.severity}
              </span>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
