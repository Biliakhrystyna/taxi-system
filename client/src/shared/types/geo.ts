export interface LatLng {
  lat: number;
  lng: number;
}

/** Реальний маршрут по дорогах з бекенду (OpenRouteServiceClient)  */
export interface SafeRouteResult {
  points: LatLng[];
  turn_count: number;
  distance_meters: number;
  source: string;
}

/** Результат гео-пошуку (OpenRouteServiceGeocodingClient) — адреса з координатами. */
export interface GeocodedAddress {
  label: string;
  lat: number;
  lng: number;
}

/** Прогноз небезпеки для контрольної точки маршруту (POST /api/weather/route). */
export interface RoutePointForecast extends LatLng {
  hazard_level: 'HIGH' | 'NORMAL';
  precipitation_mm: number;
  source: string;
  reason: string;
}

/** Суцільна небезпечна ділянка маршруту — лінія та причини небезпеки. */
export interface HazardStretch {
  points: LatLng[];
  reason: string;
}
