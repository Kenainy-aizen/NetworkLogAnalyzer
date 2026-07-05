import { useState } from 'react';

export default function SearchBar({ value, onChange }) {
  const [input, setInput] = useState(value || '');

  const handleSubmit = (e) => {
    e.preventDefault();
    onChange(input.trim());
  };

  const handleClear = () => {
    setInput('');
    onChange('');
  };

  return (
    <form onSubmit={handleSubmit} className="flex items-center gap-2 flex-1">
      <div className="relative flex-1">
        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500 text-xs">
          🔍
        </span>
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Rechercher dans les logs... (IP, protocole, message)"
          className="w-full rounded border border-zinc-800 bg-zinc-900 pl-8 pr-4 py-1.5 text-xs text-zinc-300 placeholder-zinc-600 focus:border-blue-500 focus:outline-none transition-colors"
        />
        {input && (
          <button
            type="button"
            onClick={handleClear}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-500 hover:text-zinc-300 text-xs"
          >
            ✕
          </button>
        )}
      </div>
      <button
        type="submit"
        className="rounded border border-zinc-700 bg-zinc-800 px-3 py-1.5 text-xs text-zinc-300 hover:border-zinc-500 hover:text-white transition-colors"
      >
        Rechercher
      </button>
    </form>
  );
}
