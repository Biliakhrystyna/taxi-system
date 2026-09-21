import type { HazardStretch, LatLng } from '../../shared/types/geo';
import { weatherApi } from '../order/weatherApi';
import { findHazardStretches, sampleRoute } from './geoFilter';

/** Небезпечні за Open-Meteo ділянки маршруту; null, якщо прогноз недоступний. */
export async function analyzeRouteHazards(routePoints: LatLng[]): Promise<HazardStretch[] | null> {
  if (routePoints.length < 2) return null;

  const samples = sampleRoute(routePoints);
  const forecasts = await weatherApi.getRouteForecast(samples.map((s) => s.point));
  if (forecasts.every((f) => f.source === 'unavailable')) return null;

  return findHazardStretches(routePoints, samples, forecasts);
}
