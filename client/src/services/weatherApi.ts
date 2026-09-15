import { http } from './http';

export interface WeatherForecast {
  hazard_level: 'HIGH' | 'NORMAL';
  precipitation_mm: number;
  source: string;
}

export const weatherApi = {
  /** Опади зараз + найближча година для заданої точки (за замовчуванням — Львів). */
  getForecast: (lat?: number, lng?: number) => {
    const query = lat !== undefined && lng !== undefined ? `?lat=${lat}&lng=${lng}` : '';
    return http.get<WeatherForecast>(`/api/weather/forecast${query}`);
  },
};
