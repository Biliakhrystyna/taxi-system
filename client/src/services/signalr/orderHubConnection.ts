import * as signalR from '@microsoft/signalr';
import { ORDER_HUB_URL } from '../../config';

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(ORDER_HUB_URL)
      .withAutomaticReconnect()
      .build();
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
    startPromise = conn.start().catch((err) => {
      startPromise = null;
      throw err;
    });
  }

  return startPromise.then(() => conn);
}

export function getOrderHubConnection(): signalR.HubConnection {
  return getConnection();
}
