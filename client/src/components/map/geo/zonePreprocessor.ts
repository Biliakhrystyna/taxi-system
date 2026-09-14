import type { DemandZoneCircle } from '../../../types/geo';

/**
 * Препроцесинг зон підвищеного попиту («дефіциту авто») навколо заданого центру.
 *
 * TODO(backend): це тимчасова випадкова генерація. У фінальній архітектурі дані
 * сюди прийдуть з BackgroundService (DemandZoneCalculatorService) через SignalR —
 * реальне співвідношення вільних водіїв/активних замовлень по зонах. Ця функція
 * лишиться відповідальною лише за препроцесинг вхідного набору перед рендером
 * (див. filterZonesWithinBounds у geoFilter.ts), генератор буде видалено.
 */
export function generateOutskirtsZones(
  centerLat: number,
  centerLng: number,
  count = 3,
): DemandZoneCircle[] {
  const zones: DemandZoneCircle[] = [];

  for (let i = 0; i < count; i++) {
    const angle = Math.random() * Math.PI * 2;
    const distanceKm = 2.0 + Math.random() * 2.5;
    const latOffset = (distanceKm / 111.32) * Math.sin(angle);
    const lngOffset = (distanceKm / (111.32 * Math.cos((centerLat * Math.PI) / 180))) * Math.cos(angle);

    zones.push({
      id: `ZONE_${Date.now()}_${i}`,
      lat: centerLat + latOffset,
      lng: centerLng + lngOffset,
      radius: 1000 + Math.random() * 300,
    });
  }

  return zones;
}
