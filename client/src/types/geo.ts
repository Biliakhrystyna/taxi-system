export interface LatLng {
  lat: number;
  lng: number;
}

/** Кругова зона попиту на мапі — приходить із сервера (DemandZoneStore) через SignalR. */
export interface DemandZoneCircle extends LatLng {
  id: string;
  /** Радіус зони в метрах. */
  radius: number;
}

export interface MapBounds {
  north: number;
  south: number;
  east: number;
  west: number;
}

/** Реальний маршрут по дорогах з бекенду (OpenRouteServiceClient) — null, якщо сервіс недоступний. */
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
