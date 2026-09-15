import { http } from './http';
import type { LatLng, SafeRouteResult } from '../types/geo';

export const routingApi = {
  /** Реальний маршрут по дорогах з найменшою кількістю поворотів (ORS). Null — якщо сервіс недоступний. */
  getSafeRoute: (from: LatLng, to: LatLng) =>
    http.get<SafeRouteResult | null>(
      `/api/routing/safe-route?fromLat=${from.lat}&fromLng=${from.lng}&toLat=${to.lat}&toLng=${to.lng}`,
    ),
};
