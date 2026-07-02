export function exportToCsv(events, filename = 'network-events.csv') {
  if (events.length === 0) return;

  const headers = [
    'ID',
    'Timestamp',
    'Source IP',
    'Destination IP',
    'Protocole',
    'Port',
    'Action',
    'Sévérité',
    'Source',
    'Données brutes',
  ];

  const rows = events.map(e => [
    e.id,
    new Date(e.timestamp).toISOString(),
    e.sourceIp || '',
    e.destinationIp || '',
    e.protocol || '',
    e.port ?? '',
    e.action || '',
    e.severity || '',
    e.source || '',
    // Échapper les guillemets dans les données brutes
    `"${(e.rawData || '').replace(/"/g, '""')}"`,
  ]);

  const csvContent = [
    headers.join(','),
    ...rows.map(row => row.join(','))
  ].join('\n');

  // Créer un lien de téléchargement temporaire
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  URL.revokeObjectURL(url);
}
