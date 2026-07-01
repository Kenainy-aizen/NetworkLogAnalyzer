import { useEffect, useState } from 'react';

export default function ToastNotification({ toasts, onDismiss }) {
  return (
    <div className="fixed bottom-5 right-5 z-50 flex flex-col gap-2">
      {toasts.map((toast) => (
        <Toast key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>
  );
}

function Toast({ toast, onDismiss }) {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    // Animation d'entrée
    const showTimer = setTimeout(() => setVisible(true), 10);

    // Disparition automatique après 6 secondes
    const hideTimer = setTimeout(() => {
      setVisible(false);
      setTimeout(() => onDismiss(toast.id), 300);
    }, 6000);

    return () => {
      clearTimeout(showTimer);
      clearTimeout(hideTimer);
    };
  }, [toast.id, onDismiss]);

  return (
    <div
      className={`w-80 rounded border border-red-500 bg-zinc-900 p-4 shadow-xl transition-all duration-300 ${
        visible ? 'translate-x-0 opacity-100' : 'translate-x-full opacity-0'
      }`}
    >
      <div className="mb-1 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-red-500"></span>
          <span className="text-xs font-semibold uppercase tracking-wide text-red-400">
            Alerte CRITICAL
          </span>
        </div>
        <button
          onClick={() => onDismiss(toast.id)}
          className="text-zinc-500 hover:text-zinc-300"
        >
          ✕
        </button>
      </div>
      <p className="text-sm text-zinc-300">{toast.message}</p>
      <p className="mt-1 text-xs text-zinc-500">
        {new Date(toast.timestamp).toLocaleTimeString()} — {toast.sourceIp}
      </p>
    </div>
  );
}
