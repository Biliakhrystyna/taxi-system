import { http } from '../../shared/http';
import type { LatLng, SafeRouteResult } from '../../shared/types/geo';

export const routingApi = {
  /** Стандартний (найшвидший) маршрут по справжніх дорогах (ORS).*/
  getRoute: (from: LatLng, to: LatLng) =>
    http.get<SafeRouteResult | null>(
      `/api/routing/route?fromLat=${from.lat}&fromLng=${from.lng}&toLat=${to.lat}&toLng=${to.lng}`,
    ),
  /** Реальний маршрут по дорогах з найменшою кількістю поворотів (ORS). */
  getSafeRoute: (from: LatLng, to: LatLng) =>
    http.get<SafeRouteResult | null>(
      `/api/routing/safe-route?fromLat=${from.lat}&fromLng=${from.lng}&toLat=${to.lat}&toLng=${to.lng}`,
    ),
};
