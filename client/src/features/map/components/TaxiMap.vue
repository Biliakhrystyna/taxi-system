<template>
  <div class="map-wrapper">
    <div id="map" class="leaflet-map-container"></div>
    <div class="map-hint">
      💡 <span v-if="role === 'passenger'">Введіть адресу вище або клікніть на мапі: 1-й клік — Точка А, 2-й клік — Точка В</span>
      <span v-else>Маршрут поточного замовлення</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { useOrderStore } from '../../order/store/orderStore';
import type { LatLng } from '../../../shared/types/geo';
import { buildDirectRoute } from '../routeBuilder';
import { routingApi } from '../routingApi';
import { geocodingApi } from '../geocodingApi';
import { LVIV_CENTER } from '../../../shared/config';

const props = defineProps<{
  role: 'passenger' | 'driver';
  isBadWeather: boolean;
}>();

const orderStore = useOrderStore();

let map: L.Map | null = null;
let markerA: L.Marker | null = null;
let markerB: L.Marker | null = null;
let routeLine: L.Polyline | null = null;
let hazardLayer: L.LayerGroup | null = null;

// Небезпечні ділянки стандартного маршруту (результат геопросторової
// фільтрації в orderStore.loadRouteHazards) — червоні відрізки поверх лінії.
const drawHazards = () => {
  hazardLayer?.clearLayers();
  if (!map || !hazardLayer || props.role !== 'passenger') return;
  if (orderStore.useSafeRoute && orderStore.isBadWeather) return;

  for (const stretch of orderStore.routeHazards) {
    L.polyline(stretch.points, { color: '#ef4444', weight: 7, opacity: 0.9, pane: 'hazard' })
      .bindTooltip(`⚠️ Небезпечна ділянка: ${stretch.reason}`, { sticky: true })
      .addTo(hazardLayer);
  }
};


let routeDrawToken = 0;
const invalidatePendingRoute = () => { routeDrawToken++; };


const drawRoute = async (a: LatLng, b: LatLng, isSafeApplied: boolean) => {
  if (!map) return;
  const myToken = ++routeDrawToken;
  if (routeLine) map.removeLayer(routeLine);

  if (isSafeApplied && orderStore.isBadWeather) {
    // Реальний маршрут по дорогах з мінімумом поворотів (OpenRouteServiceClient
    // на бекенді). 
    let points: LatLng[] = [];
    let popupLabel = '🛡️ Безпечний маршрут недоступний (ORS) — показано пряму лінію';

    try {
      const safeRoute = await routingApi.getSafeRoute(a, b);
      if (safeRoute && safeRoute.points.length > 1) {
        points = safeRoute.points;
        popupLabel = `🛡️ Безпечний маршрут (ORS): ${safeRoute.turn_count} поворотів`;
      }
    } catch {
     
    }

    if (points.length === 0) {
      points = buildDirectRoute(a, b);
    }


    if (!map || myToken !== routeDrawToken) return;

    routeLine = L.polyline(points, {
      color: '#f97316',
      weight: 5,
      dashArray: '6, 6',
      opacity: 0.95,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent(`🏁 Точка В<br><b style="color: #f97316;">${popupLabel}</b>`).openPopup();
    }
  } else {
 
    let points: LatLng[] = [];
    let popupLabel = '✨ Стандартний шлях (пряма лінія — ORS недоступний)';

    try {
      const route = await routingApi.getRoute(a, b);
      if (route && route.points.length > 1) {
        points = route.points;
        popupLabel = '✨ Стандартний найкоротший шлях (по дорогах)';
      }
    } catch {
      
    }

    if (points.length === 0) {
      points = buildDirectRoute(a, b);
    }

    if (!map || myToken !== routeDrawToken) return; 
    routeLine = L.polyline(points, {
      color: '#38bdf8',
      weight: 4,
      opacity: 0.8,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent(`🏁 Точка В<br><b style="color: #38bdf8;">${popupLabel}</b>`).openPopup();
    }
  }
};

const placePickupPoint = async (coords: LatLng) => {
  if (!map) return;
  if (markerA) map.removeLayer(markerA);
  markerA = L.marker([coords.lat, coords.lng]).addTo(map).bindPopup('📍 Точка А (Звідки)').openPopup();
  map.setView([coords.lat, coords.lng], Math.max(map.getZoom(), 13));

  // Адресу вже могли підставити з автопідказок (тоді вона не порожня) 
  if (!orderStore.pickupLocation.trim()) {
    try {
      const address = await geocodingApi.reverse(coords.lat, coords.lng);
      orderStore.pickupLocation = address?.label ?? `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    } catch {
      orderStore.pickupLocation = `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    }
  }

  // Реальна погода саме в точці відправлення —
  // щоб можна було демонструвати реальні опади, обравши будь-яку адресу.
  orderStore.fetchWeatherHazard(coords.lat, coords.lng);

 
  if (markerB) {
    drawRoute(coords, markerB.getLatLng(), orderStore.useSafeRoute);
  }
};

const placeDestinationPoint = async (coords: LatLng) => {
  if (!map || !markerA) return;
  if (markerB) map.removeLayer(markerB);
  markerB = L.marker([coords.lat, coords.lng]).addTo(map).bindPopup('🏁 Точка В (Куди)').openPopup();
  map.fitBounds(L.latLngBounds([markerA.getLatLng(), coords]), { padding: [40, 40], maxZoom: 15 });

  if (!orderStore.destinationLocation.trim()) {
    try {
      const address = await geocodingApi.reverse(coords.lat, coords.lng);
      orderStore.destinationLocation = address?.label ?? `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    } catch {
      orderStore.destinationLocation = `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    }
  }

  drawRoute(markerA.getLatLng(), coords, orderStore.useSafeRoute);
};

onMounted(() => {
  map = L.map('map').setView([LVIV_CENTER.lat, LVIV_CENTER.lng], 12);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap' }).addTo(map);

  map.createPane('hazard').style.zIndex = '450';
  hazardLayer = L.layerGroup().addTo(map);


  if (props.role === 'passenger') {
    if (orderStore.pickupCoords) placePickupPoint(orderStore.pickupCoords);
    if (orderStore.destinationCoords) placeDestinationPoint(orderStore.destinationCoords);
  }

  if (props.role === 'passenger') {
    
    map.on('click', (e) => {
      // Поки є активне замовлення — точки А/Б лишаються "заморожені" до кінця
      // поїздки (завершення/скасування), клік по мапі більше їх не чіпає.
      if (orderStore.currentOrder && orderStore.currentOrder.order_id) return;

      const { lat, lng } = e.latlng;

      if (!orderStore.pickupCoords) {
        orderStore.pickupCoords = { lat, lng };
      } else if (!orderStore.destinationCoords) {
        orderStore.destinationCoords = { lat, lng };
      } else {
        // Третій клік — повне скидання
        orderStore.pickupLocation = '';
        orderStore.destinationLocation = '';
        orderStore.pickupCoords = null;
        orderStore.destinationCoords = null;
      }
    });
  }
});

watch(
  () => orderStore.pickupCoords,
  (coords) => {
    if (props.role !== 'passenger') return;

    if (coords) {
      placePickupPoint(coords);
      return;
    }

    // Скинуто ззовні (третій клік / "Очистити поточне замовлення") — прибирає все.
    invalidatePendingRoute();
    if (markerA) { map?.removeLayer(markerA); markerA = null; }
    if (markerB) { map?.removeLayer(markerB); markerB = null; }
    if (routeLine) { map?.removeLayer(routeLine); routeLine = null; }
  },
);

watch(
  () => orderStore.destinationCoords,
  (coords) => {
    if (props.role !== 'passenger') return;

    if (coords) {
      placeDestinationPoint(coords);
      return;
    }

    invalidatePendingRoute();
    if (markerB) { map?.removeLayer(markerB); markerB = null; }
    if (routeLine) { map?.removeLayer(routeLine); routeLine = null; }
  },
);

watch(
  () => orderStore.currentOrder,
  (newOrder) => {
    if (!map || props.role === 'passenger') return;


    invalidatePendingRoute();
    if (markerA) { map.removeLayer(markerA); markerA = null; }
    if (markerB) { map.removeLayer(markerB); markerB = null; }
    if (routeLine) { map.removeLayer(routeLine); routeLine = null; }

    if (
      newOrder && newOrder.order_id &&
      newOrder.pickup_lat !== null && newOrder.pickup_lat !== undefined &&
      newOrder.pickup_lng !== null && newOrder.pickup_lng !== undefined &&
      newOrder.destination_lat !== null && newOrder.destination_lat !== undefined &&
      newOrder.destination_lng !== null && newOrder.destination_lng !== undefined
    ) {
      const coordsA: LatLng = { lat: newOrder.pickup_lat, lng: newOrder.pickup_lng };
      const coordsB: LatLng = { lat: newOrder.destination_lat, lng: newOrder.destination_lng };

      markerA = L.marker([coordsA.lat, coordsA.lng]).addTo(map).bindPopup('📍 Пасажир тут');
      markerB = L.marker([coordsB.lat, coordsB.lng]).addTo(map).bindPopup('🏁 Кінцева точка рейсу');

      drawRoute(coordsA, coordsB, newOrder.safe_route_applied);
    }
  },
  { deep: true },
);

watch(() => [orderStore.routeHazards, orderStore.useSafeRoute, orderStore.isBadWeather], drawHazards, { deep: true });

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
    if (props.role === 'passenger' && markerA && markerB) {
      drawRoute(markerA.getLatLng(), markerB.getLatLng(), orderStore.useSafeRoute);
    }
  },
);

</script>

<style scoped>
.map-wrapper { position: relative; width: 100%; height: 350px; margin-top: 15px; border-radius: 8px; overflow: hidden; border: 2px solid #334155; }
.leaflet-map-container { width: 100%; height: 100%; z-index: 1; }
.map-hint { position: absolute; top: 10px; right: 10px; background: rgba(15, 23, 42, 0.85); padding: 6px 12px; border-radius: 4px; font-size: 11px; color: #e2e8f0; z-index: 1000; border: 1px solid #475569; }
</style>
