/**
 * Адреса бекенду ASP.NET Core Web API. За замовчуванням — локальний dev-сервер
 * з server/TaxiSystem.Api/Properties/launchSettings.json (порт 5080).
 * Перевизначається через .env.local: VITE_API_BASE_URL=...
 */
export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5080';

export const ORDER_HUB_URL = `${API_BASE_URL}/hubs/orders`;

export const LVIV_CENTER = { lat: 49.8419, lng: 24.0315 };
