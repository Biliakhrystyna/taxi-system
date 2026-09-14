import type { LatLng } from '../../../types/geo';

/** Прямий маршрут А→Б (стандартний, найкоротший шлях). */
export function buildDirectRoute(a: LatLng, b: LatLng): LatLng[] {
  return [a, b];
}

/**
 * Симуляція «безпечного» маршруту синусоїдним вигином навколо прямої А→Б.
 *
 * TODO(backend): замінити на реальний запит до routing-API з обходом
 * небезпечних ділянок дороги — поточна функція лише малює орієнтовну
 * альтернативу, фактичного обходу перешкод не виконує.
 */
export function buildSimulatedSafeRoute(a: LatLng, b: LatLng, steps = 10, amplitude = 0.0035): LatLng[] {
  const points: LatLng[] = [];

  for (let i = 0; i <= steps; i++) {
    const t = i / steps;
    const lat = a.lat + (b.lat - a.lat) * t;
    const lng = a.lng + (b.lng - a.lng) * t;
    const wave = Math.sin(t * Math.PI) * amplitude;
    points.push({ lat: lat + wave, lng: lng - wave });
  }

  return points;
}
