import type { HazardStretch, LatLng, RoutePointForecast } from '../../../types/geo';

const EARTH_RADIUS_M = 6371000;


export function haversineDistance(a: LatLng, b: LatLng): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(b.lat - a.lat);
  const dLng = toRad(b.lng - a.lng);
  const lat1 = toRad(a.lat);
  const lat2 = toRad(b.lat);

  const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;
  return 2 * EARTH_RADIUS_M * Math.asin(Math.sqrt(h));
}


export interface RouteSample {
  point: LatLng;
  index: number;
}

/**
 * Препроцесинг маршруту: проріджує лінію до контрольних точок із кроком
 * `stepMeters` уздовж шляху (перша й остання точки завжди входять). Якщо точок
 * вийшло більше за `maxSamples`, крок збільшується, щоб вкластися в ліміт
 * (один запит до Open-Meteo на весь маршрут).
 */
export function sampleRoute(points: LatLng[], stepMeters = 2000, maxSamples = 12): RouteSample[] {
  if (points.length === 0) return [];
  if (points.length === 1) return [{ point: points[0], index: 0 }];

  const cumulative: number[] = [0];
  for (let i = 1; i < points.length; i++) {
    cumulative.push(cumulative[i - 1] + haversineDistance(points[i - 1], points[i]));
  }
  const total = cumulative[cumulative.length - 1];

  const segments = Math.min(maxSamples - 1, Math.max(1, Math.ceil(total / stepMeters)));
  const step = total / segments;

  const samples: RouteSample[] = [{ point: points[0], index: 0 }];
  let target = step;
  for (let i = 1; i < points.length - 1 && samples.length < segments; i++) {
    if (cumulative[i] >= target) {
      samples.push({ point: points[i], index: i });
      target += step;
    }
  }
  samples.push({ point: points[points.length - 1], index: points.length - 1 });

  return samples;
}

/**
 * Фільтрація: лишає лише небезпечні ділянки маршруту. Кожна контрольна
 * точка «відповідає» за відрізок лінії до середини шляху до сусідніх точок;
 * сусідні небезпечні відрізки зливаються в одну ділянку.
 */
export function findHazardStretches(
  route: LatLng[],
  samples: RouteSample[],
  forecasts: RoutePointForecast[],
): HazardStretch[] {
  const stretches: HazardStretch[] = [];
  let current: { from: number; to: number; reasons: Set<string> } | null = null;

  const flush = () => {
    if (!current) return;
    stretches.push({
      points: route.slice(current.from, current.to + 1),
      reason: [...current.reasons].join(', '),
    });
    current = null;
  };

  samples.forEach((sample, i) => {
    if (forecasts[i]?.hazard_level !== 'HIGH') {
      flush();
      return;
    }

    const from = i === 0 ? 0 : Math.floor((samples[i - 1].index + sample.index) / 2);
    const to = i === samples.length - 1 ? route.length - 1 : Math.ceil((sample.index + samples[i + 1].index) / 2);

    if (!current) {
      current = { from, to, reasons: new Set() };
    } else {
      current.to = to;
    }
    forecasts[i].reason.split(', ').forEach((r) => current!.reasons.add(r));
  });
  flush();

  return stretches;
}
