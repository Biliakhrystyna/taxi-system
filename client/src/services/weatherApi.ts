import { http } from './http';

export interface WeatherForecast {
  hazard_level: 'HIGH' | 'NORMAL';
  precipitation_mm: number;
  source: string;
  /*Пояснення, чому HIGH/NORMAL — опади, ожеледиця, іній тощо. */
  reason: string;
}

export const weatherApi = {
  /** Опади зараз + найближча година для заданої точки . */
  getForecast: (lat?: number, lng?: number) => {
    const query = lat !== undefined && lng !== undefined ? `?lat=${lat}&lng=${lng}` : '';
    return http.get<WeatherForecast>(`/api/weather/forecast${query}`);
  },
};
