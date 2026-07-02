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

function displayIp(ip) {
  if (!ip || ip === '') return 'localhost';
  if (ip === '::1' || ip === '127.0.0.1') return 'localhost (::1)';
  return ip;
}

export default function EventTable({ events, onIpClick }) {
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
          {['Heure', 'Source IP', 'Protocole', 'Port', 'Action', 'Sévérité'].map(h => (
            <th key={h} className="px-4 py-2 text-left text-xs font-medium uppercase tracking-wide text-zinc-500">{h}</th>
          ))}
        </tr>
      </thead>
      <tbody>
        {events.map((event) => (
          <tr key={event.id} className="border-b border-zinc-900 hover:bg-zinc-900">
            <td className="px-4 py-2 font-mono text-xs text-zinc-300">
              {new Date(event.timestamp).toLocaleTimeString()}
            </td>
            <td className="px-4 py-2">
              <button
                onClick={() => onIpClick?.(event.sourceIp || 'localhost')}
                className="font-mono text-xs text-blue-400 hover:underline"
              >
                {displayIp(event.sourceIp)}
              </button>
            </td>
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
