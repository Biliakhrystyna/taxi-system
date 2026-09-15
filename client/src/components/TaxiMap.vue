<template>
  <div class="map-wrapper">
    <div id="map" class="leaflet-map-container"></div>
    <div class="map-hint">
      💡 <span v-if="role === 'passenger'">Клікніть на мапі: 1-й клік — Точка А, 2-й клік — Точка В</span>
      <span v-else>Предиктивний ШІ-моніторинг дефіциту автомобілів</span>
    </div>

    <div v-if="role === 'driver'" class="ai-legend">
      <h4>🔥 ШІ: Теплова карта попиту</h4>
      <div class="legend-scale">
        <span class="scale-item red">High (Дефіцит / Бонус)</span>
        <span class="scale-item green">Normal (Профіцит)</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import type { HubConnection } from '@microsoft/signalr';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { useOrderStore } from '../stores/orderStore';
import type { DemandZoneCircle, LatLng } from '../types/geo';
import { isPointInAnyZone, filterZonesWithinBounds } from './map/geo/geoFilter';
import { buildDirectRoute, buildSimulatedSafeRoute } from './map/geo/routeBuilder';
import { ensureOrderHubConnected } from '../services/signalr/orderHubConnection';
import { LVIV_CENTER } from '../config';

const props = defineProps<{
  role: 'passenger' | 'driver';
  isBadWeather: boolean;
}>();

const orderStore = useOrderStore();

let map: L.Map | null = null;
let markerA: L.Marker | null = null;
let markerB: L.Marker | null = null;
let routeLine: L.Polyline | null = null;
let heatCircles: L.Circle[] = [];
let hubConnection: HubConnection | null = null;

// Зони підвищеного попиту тепер приходять з сервера (DemandZoneCalculatorService
// через OrderHub), а не генеруються локально при монтуванні карти.
let aiGeneratedDeficitZones: DemandZoneCircle[] = [];

const handleZonesUpdated = (zones: DemandZoneCircle[]) => {
  aiGeneratedDeficitZones = zones;
  renderAllZones();
};

const renderAllZones = () => {
  if (!map) return;
  heatCircles.forEach((circle) => map!.removeLayer(circle));
  heatCircles = [];

  if (props.role === 'passenger') return;

  // Зелені (профіцитні) зони — статичні опорні точки демо-режиму.
  [
    { coords: [49.8419, 24.0315] as [number, number], rad: 800 },
    { coords: [49.835, 24.015] as [number, number], rad: 600 },
  ].forEach((cz) => {
    const gc = L.circle(cz.coords, { color: '#10b981', fillColor: '#34d399', fillOpacity: 0.1, radius: cz.rad, interactive: false }).addTo(map!);
    heatCircles.push(gc);
  });

  // Червоні зони дефіциту — препроцесинг (фільтр за видимою областю карти) перед рендером.
  const visibleBounds = map.getBounds();
  const zonesToRender = filterZonesWithinBounds(aiGeneratedDeficitZones, {
    north: visibleBounds.getNorth(),
    south: visibleBounds.getSouth(),
    east: visibleBounds.getEast(),
    west: visibleBounds.getWest(),
  });

  zonesToRender.forEach((zone) => {
    const rc = L.circle([zone.lat, zone.lng], {
      color: '#ef4444',
      fillColor: '#f87171',
      fillOpacity: orderStore.isBadWeather ? 0.5 : 0.25,
      radius: zone.radius,
      interactive: false,
    }).addTo(map!);
    heatCircles.push(rc);
  });
};

// Універсальна функція малювання лінії маршруту (для пасажира і водія)
const drawRoute = (a: LatLng, b: LatLng, isSafeApplied: boolean) => {
  if (!map) return;
  if (routeLine) map.removeLayer(routeLine);

  if (isSafeApplied && orderStore.isBadWeather) {
    const points = buildSimulatedSafeRoute(a, b);

    routeLine = L.polyline(points, {
      color: '#f97316',
      weight: 5,
      dashArray: '6, 6',
      opacity: 0.95,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent('🏁 Точка В<br><b style="color: #f97316;">🛡️ ШІ-маршрут: Оптимальний безпечний обхід</b>').openPopup();
    }
  } else {
    const points = buildDirectRoute(a, b);

    routeLine = L.polyline(points, {
      color: '#38bdf8',
      weight: 4,
      opacity: 0.8,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent('🏁 Точка В<br><b style="color: #38bdf8;">✨ Стандартний найкоротший шлях</b>').openPopup();
    }
  }
};

onMounted(() => {
  map = L.map('map').setView([LVIV_CENTER.lat, LVIV_CENTER.lng], 12);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap' }).addTo(map);

  renderAllZones();

  ensureOrderHubConnected()
    .then((connection) => {
      hubConnection = connection;
      connection.off('ZonesUpdated');
      connection.on('ZonesUpdated', handleZonesUpdated);
      // BackgroundService тіка раз на 60с — просимо поточні зони одразу,
      // щоб не чекати до першого тіку після підключення.
      return connection.invoke('RequestCurrentZones');
    })
    .catch(() => {
      // Без SignalR карта лишається робочою, просто без теплової карти попиту.
    });

  if (props.role === 'passenger') {
    map.on('click', (e) => {
      const { lat, lng } = e.latlng;

      if (!markerA) {
        // Перший клік — Точка А
        markerA = L.marker([lat, lng]).addTo(map!).bindPopup('📍 Точка А (Звідки)').openPopup();
        orderStore.pickupLocation = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
      } else if (!markerB) {
        // Другий клік — Точка В
        markerB = L.marker([lat, lng]).addTo(map!).bindPopup('🏁 Точка В (Куди)').openPopup();
        orderStore.destinationLocation = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;

        // Фільтрація: чи потрапляє точка Б в зону підвищеного попиту (ШІ-геофенсинг)
        const insideDeficitZone = isPointInAnyZone({ lat, lng }, aiGeneratedDeficitZones);
        orderStore.selectedZone = insideDeficitZone ? 'outskirts' : 'center';

        drawRoute(markerA.getLatLng(), { lat, lng }, orderStore.useSafeRoute);
      } else {
        // Третій клік — повне скидання
        if (markerA) map!.removeLayer(markerA);
        if (markerB) map!.removeLayer(markerB);
        if (routeLine) map!.removeLayer(routeLine);
        markerA = null;
        markerB = null;
        routeLine = null;
        orderStore.pickupLocation = '';
        orderStore.destinationLocation = '';
        orderStore.selectedZone = 'center';
      }
    });
  }
});

watch(
  () => orderStore.currentOrder,
  (newOrder) => {
    if (!map || props.role === 'passenger') return;

    if (newOrder && newOrder.order_id && newOrder.pickup_location && newOrder.destination) {
      const parseCoords = (str: string): LatLng => {
        const parts = str.replace(/[^\d.,-]/g, '').split(',');
        return { lat: parseFloat(parts[0]), lng: parseFloat(parts[1]) };
      };

      try {
        const coordsA = parseCoords(newOrder.pickup_location);
        const coordsB = parseCoords(newOrder.destination);

        if (!markerA) markerA = L.marker([coordsA.lat, coordsA.lng]).addTo(map).bindPopup('📍 Пасажир тут');
        if (!markerB) markerB = L.marker([coordsB.lat, coordsB.lng]).addTo(map).bindPopup('🏁 Кінцева точка рейсу');

        drawRoute(coordsA, coordsB, newOrder.safe_route_applied);
      } catch (e) {
        console.log('Парсинг адресних координат...');
      }
    } else {
      if (markerA) map.removeLayer(markerA);
      if (markerB) map.removeLayer(markerB);
      if (routeLine) map.removeLayer(routeLine);
      markerA = null;
      markerB = null;
      routeLine = null;
    }
  },
  { deep: true },
);

watch(
  () => orderStore.useSafeRoute,
  () => {
    if (props.role === 'passenger' && markerA && markerB) {
      drawRoute(markerA.getLatLng(), markerB.getLatLng(), orderStore.useSafeRoute);
    }
  },
);

watch(
  () => orderStore.isBadWeather,
  () => {
    renderAllZones();
    if (props.role === 'passenger' && markerA && markerB) {
      drawRoute(markerA.getLatLng(), markerB.getLatLng(), orderStore.useSafeRoute);
    }
  },
);

onUnmounted(() => {
  hubConnection?.off('ZonesUpdated', handleZonesUpdated);
});
</script>

<style scoped>
.map-wrapper { position: relative; width: 100%; height: 350px; margin-top: 15px; border-radius: 8px; overflow: hidden; border: 2px solid #334155; }
.leaflet-map-container { width: 100%; height: 100%; z-index: 1; }
.map-hint { position: absolute; top: 10px; right: 10px; background: rgba(15, 23, 42, 0.85); padding: 6px 12px; border-radius: 4px; font-size: 11px; color: #e2e8f0; z-index: 1000; border: 1px solid #475569; }
.ai-legend { position: absolute; bottom: 10px; left: 10px; background: rgba(15, 23, 42, 0.95); padding: 8px; border-radius: 6px; border: 1px solid #38bdf8; z-index: 1000; font-size: 11px; max-width: 180px; }
.ai-legend h4 { margin: 0 0 5px 0; color: #38bdf8; font-size: 11px; }
.legend-scale { display: flex; flex-direction: column; gap: 4px; }
.scale-item { padding: 1px 6px; border-radius: 3px; font-weight: bold; font-size: 10px; text-align: center; }
.scale-item.red { background: rgba(239, 68, 68, 0.25); color: #f87171; border: 1px solid #ef4444; }
.scale-item.green { background: rgba(16, 185, 129, 0.2); color: #34d399; border: 1px solid #10b981; }
</style>
