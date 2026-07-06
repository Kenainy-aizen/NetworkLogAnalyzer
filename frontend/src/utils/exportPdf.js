import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';

export async function exportStatsToPdf(stats) {
  const pdf = new jsPDF('p', 'mm', 'a4');
  const pageWidth  = pdf.internal.pageSize.getWidth();
  const pageHeight = pdf.internal.pageSize.getHeight();
  let y = 15;

  // ── En-tête ───────────────────────────────────────────────
  pdf.setFillColor(17, 17, 27); // zinc-950
  pdf.rect(0, 0, pageWidth, 30, 'F');

  pdf.setTextColor(255, 255, 255);
  pdf.setFontSize(16);
  pdf.setFont('helvetica', 'bold');
  pdf.text('Network Log Analyzer', 14, 12);

  pdf.setFontSize(9);
  pdf.setFont('helvetica', 'normal');
  pdf.setTextColor(161, 161, 170); // zinc-400
  pdf.text('Rapport de statistiques', 14, 20);
  pdf.text(new Date().toLocaleString('fr-FR'), pageWidth - 14, 20, { align: 'right' });

  y = 40;

  // ── Résumé global ─────────────────────────────────────────
  pdf.setTextColor(30, 30, 30);
  pdf.setFontSize(12);
  pdf.setFont('helvetica', 'bold');
  pdf.text('Résumé global', 14, y);
  y += 6;

  const summaryData = [
    ['Total événements', stats.totalEvents.toString()],
    ['Bloqués',          `${stats.totalBlocked} (${Math.round(stats.totalBlocked / stats.totalEvents * 100) || 0}%)`],
    ['Autorisés',        stats.totalAllowed.toString()],
    ['Critiques',        stats.totalCritical.toString()],
    ['Avertissements',   stats.totalWarning.toString()],
    ['Informatifs',      stats.totalInfo.toString()],
  ];

  summaryData.forEach(([label, value], i) => {
    const col = i % 3;
    const row = Math.floor(i / 3);
    const x   = 14 + col * 62;
    const cardY = y + row * 20;

    pdf.setFillColor(244, 244, 245); // zinc-100
    pdf.roundedRect(x, cardY, 58, 16, 2, 2, 'F');

    pdf.setFontSize(7);
    pdf.setFont('helvetica', 'normal');
    pdf.setTextColor(113, 113, 122); // zinc-500
    pdf.text(label.toUpperCase(), x + 4, cardY + 5);

    pdf.setFontSize(13);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 30, 30);
    pdf.text(value, x + 4, cardY + 13);
  });

  y += 50;

  // ── Top IPs ───────────────────────────────────────────────
  if (stats.topSourceIps.length > 0) {
    pdf.setFontSize(12);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 30, 30);
    pdf.text('Top IPs sources', 14, y);
    y += 6;

    stats.topSourceIps.slice(0, 8).forEach((item, i) => {
      const barWidth = (item.value / stats.topSourceIps[0].value) * 100;

      pdf.setFontSize(8);
      pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(60, 60, 60);
      pdf.text(`${i + 1}. ${item.key}`, 14, y + 3);
      pdf.text(item.value.toString(), pageWidth - 14, y + 3, { align: 'right' });

      pdf.setFillColor(228, 228, 231); // zinc-200
      pdf.rect(70, y - 1, 100, 5, 'F');

      pdf.setFillColor(59, 130, 246); // blue-500
      pdf.rect(70, y - 1, barWidth, 5, 'F');

      y += 8;
    });

    y += 4;
  }

  // ── Top Ports ─────────────────────────────────────────────
  if (stats.topPorts.length > 0) {
    if (y > pageHeight - 60) { pdf.addPage(); y = 20; }

    pdf.setFontSize(12);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 30, 30);
    pdf.text('Top ports ciblés', 14, y);
    y += 6;

    stats.topPorts.slice(0, 8).forEach((item, i) => {
      const barWidth = (item.value / stats.topPorts[0].value) * 100;

      pdf.setFontSize(8);
      pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(60, 60, 60);
      pdf.text(`Port ${item.key}`, 14, y + 3);
      pdf.text(item.value.toString(), pageWidth - 14, y + 3, { align: 'right' });

      pdf.setFillColor(228, 228, 231);
      pdf.rect(70, y - 1, 100, 5, 'F');

      pdf.setFillColor(245, 158, 11); // amber-500
      pdf.rect(70, y - 1, barWidth, 5, 'F');

      y += 8;
    });

    y += 4;
  }

  // ── Protocoles ────────────────────────────────────────────
  if (stats.eventsByProtocol.length > 0) {
    if (y > pageHeight - 60) { pdf.addPage(); y = 20; }

    pdf.setFontSize(12);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 30, 30);
    pdf.text('Répartition par protocole', 14, y);
    y += 6;

    const colors = [
      [59,130,246],[245,158,11],[239,68,68],[16,185,129],
      [139,92,246],[6,182,212],[99,102,241],[236,72,153]
    ];

    stats.eventsByProtocol.slice(0, 8).forEach((item, i) => {
      const pct = Math.round(item.value / stats.totalEvents * 100);
      const [r, g, b] = colors[i % colors.length];

      pdf.setFillColor(r, g, b);
      pdf.rect(14, y - 3, 4, 4, 'F');

      pdf.setFontSize(8);
      pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(60, 60, 60);
      pdf.text(`${item.key}`, 22, y);
      pdf.text(`${item.value} (${pct}%)`, pageWidth - 14, y, { align: 'right' });

      y += 7;
    });

    y += 4;
  }

  // ── Sources ───────────────────────────────────────────────
  if (stats.eventsBySource.length > 0) {
    if (y > pageHeight - 60) { pdf.addPage(); y = 20; }

    pdf.setFontSize(12);
    pdf.setFont('helvetica', 'bold');
    pdf.setTextColor(30, 30, 30);
    pdf.text('Événements par source de log', 14, y);
    y += 6;

    stats.eventsBySource.forEach((item) => {
      const pct = Math.round(item.value / stats.totalEvents * 100);

      pdf.setFontSize(8);
      pdf.setFont('helvetica', 'normal');
      pdf.setTextColor(60, 60, 60);
      pdf.text(item.key, 14, y);
      pdf.text(`${item.value} (${pct}%)`, pageWidth - 14, y, { align: 'right' });

      y += 7;
    });
  }

  // ── Pied de page ──────────────────────────────────────────
  const totalPages = pdf.internal.getNumberOfPages();
  for (let i = 1; i <= totalPages; i++) {
    pdf.setPage(i);
    pdf.setFontSize(7);
    pdf.setTextColor(161, 161, 170);
    pdf.text(
      `Network Log Analyzer — Page ${i}/${totalPages}`,
      pageWidth / 2, pageHeight - 8,
      { align: 'center' }
    );
  }

  pdf.save(`network-log-stats-${new Date().toISOString().split('T')[0]}.pdf`);
}
