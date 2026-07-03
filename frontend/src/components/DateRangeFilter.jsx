export default function DateRangeFilter({ value, onChange }) {
  const presets = [
    { label: '30 min', minutes: 30 },
    { label: '1 h',   minutes: 60 },
    { label: '6 h',   minutes: 360 },
    { label: '24 h',  minutes: 1440 },
    { label: 'Tout',  minutes: null },
  ];

  return (
    <div className="flex items-center gap-2">
      <span className="text-xs text-zinc-500">Période :</span>
      {presets.map((preset) => (
        <button
          key={preset.label}
          onClick={() => onChange(preset.minutes)}
          className={`rounded border px-2.5 py-1 text-xs transition-colors ${
            value === preset.minutes
              ? 'border-blue-500 bg-blue-500/10 text-blue-400'
              : 'border-zinc-800 text-zinc-500 hover:border-zinc-700 hover:text-zinc-300'
          }`}
        >
          {preset.label}
        </button>
      ))}
    </div>
  );
}
