import * as signalR from '@microsoft/signalr';
import { ORDER_HUB_URL } from '../../config';

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

    // withAutomaticReconnect() сам оживляє з'єднання лише після короткого
    // розриву мережі — спроб обмежена кількість, і якщо вкладка "проспала"
    // довше (ноутбук заснув/закрили кришку), він здається назавжди й
    // переходить у onclose. Без цього обробника вкладка так і лишається
    // "глухою" до нових подій, навіть коли мережа згодом повертається.
    connection.onreconnected(() => {
      onReconnectedCallback?.();
    });
  }
  return connection;
}

/** Один спільний конект на весь застосунок — orderStore підписується на нього
 * (OrderUpdated, WeatherUpdated). */
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

/** Викликається щоразу, коли з'єднання ожило ПІСЛЯ реального розриву (не
 * при першому вході) — orderStore тут дозаписує стан, пропущений, поки
 * зв'язку не було. */
export function onOrderHubReconnected(callback: () => void): void {
  onReconnectedCallback = callback;
}

export function getOrderHubConnection(): signalR.HubConnection {
  return getConnection();
}

function reviveIfDead(): void {
  if (connection?.state === signalR.HubConnectionState.Disconnected) {
    startPromise = null;
    ensureOrderHubConnected().catch(() => {});
  }
}

// Засинання ноутбука з відкритою вкладкою — типовий сценарій, коли
// withAutomaticReconnect() вичерпує спроби задовго до пробудження й більше
// сам не намагається. При поверненні вкладки у фокус перевіряємо з'єднання
// і піднімаємо його вручну, якщо воно досі мертве.
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
