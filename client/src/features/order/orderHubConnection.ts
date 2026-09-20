import * as signalR from '@microsoft/signalr';
import { ORDER_HUB_URL } from '../../shared/config';

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;
let hasConnectedBefore = false;
let onReconnectedCallback: (() => void) | null = null;

function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(ORDER_HUB_URL)
      .withAutomaticReconnect()
      .build();

  
    connection.onreconnected(() => {
      onReconnectedCallback?.();
    });
  }
  return connection;
}


export function ensureOrderHubConnected(): Promise<signalR.HubConnection> {
  const conn = getConnection();

  if (conn.state === signalR.HubConnectionState.Connected) {
    return Promise.resolve(conn);
  }

  if (!startPromise) {
    startPromise = conn
      .start()
      .then(() => {
        // hasConnectedBefore розрізняє перший вхід (initial connect — не
        // "перепідключення") від реального відновлення після розриву.
        if (hasConnectedBefore) onReconnectedCallback?.();
        hasConnectedBefore = true;
      })
      .catch((err) => {
        startPromise = null;
        throw err;
      });
  }

  return startPromise.then(() => conn);
}


export function onOrderHubReconnected(callback: () => void): void {
  onReconnectedCallback = callback;
}

function reviveIfDead(): void {
  if (connection?.state === signalR.HubConnectionState.Disconnected) {
    startPromise = null;
    ensureOrderHubConnected().catch(() => {});
  }
}


if (typeof document !== 'undefined') {
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') reviveIfDead();
  });

  // Підстраховка на випадок, коли вкладка весь час лишалась активною й
  // visibilitychange узагалі не спрацював (напр. мережа зникла й вернулась,
  // поки вкладка була на екрані) — періодично самі перевіряємо стан, а не
  // чекаємо нагоди про це дізнатись.
  setInterval(reviveIfDead, 20000);
}
