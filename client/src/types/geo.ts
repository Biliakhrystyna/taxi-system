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
