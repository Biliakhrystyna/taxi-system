import { http } from './http';
import type { LatLng, RoutePointForecast } from '../types/geo';

export interface WeatherForecast {
  hazard_level: 'HIGH' | 'NORMAL';
  precipitation_mm: number;
  source: string;
  reason: string;
}

export const weatherApi = {
  /** Опади зараз + найближча година для заданої точки . */
  getForecast: (lat?: number, lng?: number) => {
    const query = lat !== undefined && lng !== undefined ? `?lat=${lat}&lng=${lng}` : '';
    return http.get<WeatherForecast>(`/api/weather/forecast${query}`);
  },
  /** Прогноз небезпеки для контрольних точок маршруту одним запитом. */
  getRouteForecast: (points: LatLng[]) =>
    http.post<RoutePointForecast[]>('/api/weather/route', { points }),
};
