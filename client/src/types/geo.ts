export interface LatLng {
  lat: number;
  lng: number;
}

/** Узагальнена кругова область на мапі (координати + радіус у метрах) —
 * використовується геопросторовим модулем фільтрації (geoFilter.ts). */
export interface RadiusZone extends LatLng {
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
