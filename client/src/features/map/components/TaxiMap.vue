<template>
  <div class="map-wrapper">
    <div id="map" class="leaflet-map-container"></div>
    <div v-if="routeUnavailable" class="map-warning">⚠️ Маршрут по дорогах для цих точок не знайдено</div>
    <div class="map-hint">
      💡 <span v-if="role === 'passenger'">Введіть адресу вище або клікніть на мапі: 1-й клік — Точка А, 2-й клік — Точка В</span>
      <span v-else>Маршрут поточного замовлення</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import '../leafletIcons';
import { useOrderStore } from '../../order/store/orderStore';
import type { HazardStretch, LatLng } from '../../../shared/types/geo';
import { analyzeRouteHazards } from '../routeHazards';
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
let driverHazards: HazardStretch[] = [];
let hazardToken = 0;
let shownOrderId: string | null = null;

const routeUnavailable = ref(false);

// Небезпечні ділянки стандартного маршруту (результат геопросторової
// фільтрації в orderStore.loadRouteHazards) — червоні відрізки поверх лінії.
const drawHazards = () => {
  hazardLayer?.clearLayers();
  if (!map || !hazardLayer) return;

  const isPassenger = props.role === 'passenger';
  const safeRouteShown = isPassenger
    ? orderStore.useSafeRoute && orderStore.isBadWeather
    : orderStore.currentOrder?.safe_route_applied;
  if (safeRouteShown) return;

  for (const stretch of isPassenger ? orderStore.routeHazards : driverHazards) {
    L.polyline(stretch.points, { color: '#ef4444', weight: 7, opacity: 0.9, pane: 'hazard' })
      .bindTooltip(`⚠️ Небезпечна ділянка: ${stretch.reason}`, { sticky: true })
      .addTo(hazardLayer);
  }
};


let routeDrawToken = 0;
const invalidatePendingRoute = () => {
  routeDrawToken++;
  routeUnavailable.value = false;
};

const markRouteUnavailable = () => {
  routeUnavailable.value = true;
  if (markerB && props.role === 'passenger') {
    markerB.setPopupContent('🏁 Точка В<br><b style="color: #ef4444;">Маршрут по дорогах не знайдено</b>').openPopup();
  }
};


const drawRoute = async (a: LatLng, b: LatLng, showSafeRoute: boolean): Promise<LatLng[] | null> => {
  if (!map) return null;
  const myToken = ++routeDrawToken;
  if (routeLine) map.removeLayer(routeLine);

  if (showSafeRoute) {
    // Реальний маршрут по дорогах з мінімумом поворотів (OpenRouteServiceClient
    // на бекенді).
    let points: LatLng[] = [];
    let popupLabel = '';

    try {
      const safeRoute = await routingApi.getSafeRoute(a, b);
      if (safeRoute && safeRoute.points.length > 1) {
        points = safeRoute.points;
        popupLabel = `🛡️ Безпечний маршрут (ORS): ${safeRoute.turn_count} поворотів`;
      }
    } catch {

    }

    if (!map || myToken !== routeDrawToken) return null;
    if (points.length === 0) {
      markRouteUnavailable();
      return null;
    }

    routeUnavailable.value = false;
    routeLine = L.polyline(points, {
      color: '#f97316',
      weight: 5,
      dashArray: '6, 6',
      opacity: 0.95,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent(`🏁 Точка В<br><b style="color: #f97316;">${popupLabel}</b>`).openPopup();
    }
    return points;
  } else {

    let points: LatLng[] = [];
    let popupLabel = '';

    try {
      const route = await routingApi.getRoute(a, b);
      if (route && route.points.length > 1) {
        points = route.points;
        popupLabel = '✨ Стандартний найкоротший шлях (по дорогах)';
      }
    } catch {

    }

    if (!map || myToken !== routeDrawToken) return null;
    if (points.length === 0) {
      markRouteUnavailable();
      return null;
    }

    routeUnavailable.value = false;
    routeLine = L.polyline(points, {
      color: '#38bdf8',
      weight: 4,
      opacity: 0.8,
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent(`🏁 Точка В<br><b style="color: #38bdf8;">${popupLabel}</b>`).openPopup();
    }
    return points;
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
    drawRoute(coords, markerB.getLatLng(), orderStore.useSafeRoute && orderStore.isBadWeather);
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

  drawRoute(markerA.getLatLng(), coords, orderStore.useSafeRoute && orderStore.isBadWeather);
};

onMounted(() => {
  map = L.map('map').setView([LVIV_CENTER.lat, LVIV_CENTER.lng], 12);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap' }).addTo(map);

  map.createPane('hazard').style.zIndex = '450';
  hazardLayer = L.layerGroup().addTo(map);


  if (props.role === 'passenger') {
    if (orderStore.pickupCoords) placePickupPoint(orderStore.pickupCoords);
    if (orderStore.destinationCoords) placeDestinationPoint(orderStore.destinationCoords);
  } else {
    showOrderForDriver(orderStore.currentOrder);
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

const loadDriverHazards = async (routePoints: LatLng[]) => {
  const token = ++hazardToken;
  try {
    const stretches = await analyzeRouteHazards(routePoints);
    if (token !== hazardToken || !stretches) return;

    driverHazards = stretches;
    drawHazards();
  } catch {
    // Без прогнозу вздовж маршруту небезпечні ділянки не підсвічуються.
  }
};

// Мапа водія: маршрут поточного замовлення з небезпечними ділянками, карта переходить на нього.
const showOrderForDriver = (order: any) => {
  if (!map) return;

  const hasCoords = order && order.order_id
    && order.pickup_lat != null && order.pickup_lng != null
    && order.destination_lat != null && order.destination_lng != null;

  if (hasCoords && shownOrderId === order.order_id) return;

  invalidatePendingRoute();
  hazardToken++;
  driverHazards = [];
  drawHazards();
  if (markerA) { map.removeLayer(markerA); markerA = null; }
  if (markerB) { map.removeLayer(markerB); markerB = null; }
  if (routeLine) { map.removeLayer(routeLine); routeLine = null; }

  if (!hasCoords) {
    shownOrderId = null;
    return;
  }

  const coordsA: LatLng = { lat: order.pickup_lat, lng: order.pickup_lng };
  const coordsB: LatLng = { lat: order.destination_lat, lng: order.destination_lng };

  markerA = L.marker([coordsA.lat, coordsA.lng]).addTo(map).bindPopup('📍 Пасажир тут');
  markerB = L.marker([coordsB.lat, coordsB.lng]).addTo(map).bindPopup('🏁 Кінцева точка рейсу');

  map.fitBounds(L.latLngBounds([coordsA, coordsB]), { padding: [40, 40], maxZoom: 15 });
  shownOrderId = order.order_id;

  drawRoute(coordsA, coordsB, !!order.safe_route_applied).then((points) => {
    if (points && points.length > 1 && !order.safe_route_applied) {
      loadDriverHazards(points);
    }
  });
};

watch(
  () => orderStore.currentOrder,
  (newOrder) => {
    if (props.role !== 'passenger') showOrderForDriver(newOrder);
  },
  { deep: true },
);

watch(() => [orderStore.routeHazards, orderStore.useSafeRoute, orderStore.isBadWeather], drawHazards, { deep: true });

watch(
  () => orderStore.useSafeRoute,
  () => {
    if (props.role === 'passenger' && markerA && markerB) {
      drawRoute(markerA.getLatLng(), markerB.getLatLng(), orderStore.useSafeRoute && orderStore.isBadWeather);
    }
  },
);

watch(
  () => orderStore.isBadWeather,
  () => {
    if (props.role === 'passenger' && markerA && markerB) {
      drawRoute(markerA.getLatLng(), markerB.getLatLng(), orderStore.useSafeRoute && orderStore.isBadWeather);
    }
  },
);

</script>

<style scoped>
.map-wrapper { position: relative; width: 100%; height: 350px; margin-top: 15px; border-radius: 8px; overflow: hidden; border: 2px solid #334155; }
.leaflet-map-container { width: 100%; height: 100%; z-index: 1; }
.map-warning { position: absolute; bottom: 10px; left: 10px; background: var(--red); color: var(--white); padding: 6px 12px; border-radius: 4px; font-size: 12px; font-weight: 700; z-index: 1000; border: 1px solid var(--black); }
.map-hint { position: absolute; top: 10px; right: 10px; background: rgba(15, 23, 42, 0.85); padding: 6px 12px; border-radius: 4px; font-size: 11px; color: #e2e8f0; z-index: 1000; border: 1px solid #475569; }
</style>
