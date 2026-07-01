import { useCallback, useState } from 'react';

export function useToast() {
  const [toasts, setToasts] = useState([]);

  const addToast = useCallback((event) => {
    const toast = {
      id: Date.now(),
      message: event.rawData,
      sourceIp: event.sourceIp,
      timestamp: event.timestamp,
    };

    setToasts((prev) => [...prev, toast]);
  }, []);

  const dismissToast = useCallback((id) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  return { toasts, addToast, dismissToast };
}
