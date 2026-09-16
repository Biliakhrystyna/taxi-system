import { http } from './http';
import type { LatLng, SafeRouteResult } from '../types/geo';

export const routingApi = {
  /** Стандартний (найшвидший) маршрут по справжніх дорогах (ORS). Null — якщо сервіс недоступний. */
  getRoute: (from: LatLng, to: LatLng) =>
    http.get<SafeRouteResult | null>(
      `/api/routing/route?fromLat=${from.lat}&fromLng=${from.lng}&toLat=${to.lat}&toLng=${to.lng}`,
    ),
  /** Реальний маршрут по дорогах з найменшою кількістю поворотів (ORS). Null — якщо сервіс недоступний. */
  getSafeRoute: (from: LatLng, to: LatLng) =>
    http.get<SafeRouteResult | null>(
      `/api/routing/safe-route?fromLat=${from.lat}&fromLng=${from.lng}&toLat=${to.lat}&toLng=${to.lng}`,
    ),
};
