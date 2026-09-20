import type { LatLng } from '../../../types/geo';

/** Прямий маршрут А→Б (стандартний, найкоротший шлях). */
export function buildDirectRoute(a: LatLng, b: LatLng): LatLng[] {
  return [a, b];
}
