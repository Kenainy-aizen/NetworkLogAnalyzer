import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const HUB_URL = 'http://localhost:5000/hubs/logs';

export function useSignalR(onNewEvent) {
  const connectionRef = useRef(null);
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    connection.on('NewEvent', (event) => {
      onNewEvent(event);
    });

    connection.onreconnected(() => setConnected(true));
    connection.onclose(() => setConnected(false));

    connection
      .start()
      .then(() => setConnected(true))
      .catch((err) => {
        // Ignore l'erreur d'annulation due au double-montage de React StrictMode en dev
        if (err?.message?.includes('stopped during negotiation')) return;
        console.error('Connexion SignalR échouée :', err);
        setConnected(false);
      });

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [onNewEvent]);

  return connected;
}
