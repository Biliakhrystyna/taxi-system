import type { LatLng, DemandZoneCircle, MapBounds } from '../../../types/geo';

const EARTH_RADIUS_M = 6371000;

/**
 * Гаверсинова відстань між двома точками в метрах.
 * Не залежить від інстансу карти Leaflet (на відміну від `map.distance(...)`),
 * тож придатна для фільтрації даних ще до їх рендеру / поза компонентом карти.
 */
export function haversineDistance(a: LatLng, b: LatLng): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(b.lat - a.lat);
  const dLng = toRad(b.lng - a.lng);
  const lat1 = toRad(a.lat);
  const lat2 = toRad(b.lat);

  const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;
  return 2 * EARTH_RADIUS_M * Math.asin(Math.sqrt(h));
}

/** Фільтрація: чи потрапляє точка в радіус хоча б однієї із зон підвищеного попиту. */
export function isPointInAnyZone(point: LatLng, zones: DemandZoneCircle[]): boolean {
  return zones.some((zone) => haversineDistance(point, zone) <= zone.radius);
}

/**
 * Препроцесинг перед рендером: відкидає зони, чиї центри лежать поза видимою
 * областю карти — уникає зайвого малювання об'єктів поза viewport для великих наборів даних.
 */
export function filterZonesWithinBounds(zones: DemandZoneCircle[], bounds: MapBounds): DemandZoneCircle[] {
  return zones.filter(
    (zone) =>
      zone.lat <= bounds.north && zone.lat >= bounds.south && zone.lng <= bounds.east && zone.lng >= bounds.west,
  );
}
