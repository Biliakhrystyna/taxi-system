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
import { useOrderStore } from '../stores/orderStore';
import type { LatLng } from '../types/geo';
import { buildDirectRoute, buildSimulatedSafeRoute } from './map/geo/routeBuilder';
import { routingApi } from '../services/routingApi';
import { geocodingApi } from '../services/geocodingApi';
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

// Універсальна функція малювання лінії маршруту (для пасажира і водія)
const drawRoute = async (a: LatLng, b: LatLng, isSafeApplied: boolean) => {
  if (!map) return;
  if (routeLine) map.removeLayer(routeLine);

  if (isSafeApplied && orderStore.isBadWeather) {
    // Спершу пробуємо реальний маршрут по дорогах з мінімумом поворотів
    // (OpenRouteServiceClient на бекенді). Якщо ORS недоступний/без ключа —
    // падаємо на клієнтську симуляцію (синусоїда), як і раніше.
    let points: LatLng[] = [];
    let popupLabel = '🛡️ ШІ-маршрут: симуляція обходу (ORS недоступний)';

    try {
      const safeRoute = await routingApi.getSafeRoute(a, b);
      if (safeRoute && safeRoute.points.length > 1) {
        points = safeRoute.points;
        popupLabel = `🛡️ Безпечний маршрут (ORS): ${safeRoute.turn_count} поворотів`;
      }
    } catch {
      // Мережева помилка/сервер лежить — тихо падаємо на симуляцію нижче.
    }

    if (points.length === 0) {
      points = buildSimulatedSafeRoute(a, b);
    }

    if (!map) return; // компонент міг демонтуватись, поки чекали відповідь

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
    // Так само пробуємо реальний маршрут по дорогах (ORS) замість прямої
    // лінії "навпростець" по мапі; якщо ORS недоступний — падаємо на пряму лінію.
    let points: LatLng[] = [];
    let popupLabel = '✨ Стандартний шлях (пряма лінія — ORS недоступний)';

    try {
      const route = await routingApi.getRoute(a, b);
      if (route && route.points.length > 1) {
        points = route.points;
        popupLabel = '✨ Стандартний найкоротший шлях (по дорогах)';
      }
    } catch {
      // Мережева помилка/сервер лежить — тихо падаємо на пряму лінію нижче.
    }

    if (points.length === 0) {
      points = buildDirectRoute(a, b);
    }

    if (!map) return; // компонент міг демонтуватись, поки чекали відповідь

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

// Єдина точка входу для встановлення точки А — байдуже, прийшли координати
// з кліку на мапі чи з вибору в автопідказках адреси (AddressAutocomplete
// у PassengerDashboard.vue пише напряму в orderStore.pickupCoords).
const placePickupPoint = async (coords: LatLng) => {
  if (!map) return;
  if (markerA) map.removeLayer(markerA);
  markerA = L.marker([coords.lat, coords.lng]).addTo(map).bindPopup('📍 Точка А (Звідки)').openPopup();

  // Адресу вже могли підставити з автопідказок (тоді вона не порожня) —
  // зворотне геокодування потрібне лише для кліку "наосліп" по мапі.
  if (!orderStore.pickupLocation.trim()) {
    try {
      const address = await geocodingApi.reverse(coords.lat, coords.lng);
      orderStore.pickupLocation = address?.label ?? `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    } catch {
      orderStore.pickupLocation = `${coords.lat.toFixed(4)}, ${coords.lng.toFixed(4)}`;
    }
  }

  // Реальна погода саме в точці відправлення, а не в опорній точці Львова —
  // щоб можна було демонструвати реальні опади, обравши будь-яку адресу.
  orderStore.fetchWeatherHazard(coords.lat, coords.lng);

  // Якщо точка Б вже стояла (з попереднього кліку чи пошуку) — маршрут мав
  // перемалюватись під нову точку А, а не лишатись приклеєним до старої.
  if (markerB) {
    drawRoute(coords, markerB.getLatLng(), orderStore.useSafeRoute);
  }
};

const placeDestinationPoint = async (coords: LatLng) => {
  if (!map || !markerA) return;
  if (markerB) map.removeLayer(markerB);
  markerB = L.marker([coords.lat, coords.lng]).addTo(map).bindPopup('🏁 Точка В (Куди)').openPopup();

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

  if (props.role === 'passenger') {
    // Клік лише пише координати в стор — саме розміщення маркера/геокодування
    // відбувається у watch() нижче, тій самій точці входу, що й вибір адреси
    // з автопідказок (AddressAutocomplete у PassengerDashboard.vue).
    map.on('click', (e) => {
      // Поки є активне замовлення — точки А/Б лишаються "заморожені" до кінця
      // поїздки (завершення/скасування), клік по мапі більше їх не чіпає.
      // Раніше третій клік завжди повністю скидав обидві точки, навіть якщо
      // вони обидві вже належали активному замовленню — це виглядало так,
      // ніби точки "самі зникають" від випадкового кліку по мапі.
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

    // Скинуто ззовні (третій клік / "Очистити поточне замовлення") — прибираємо все.
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

    if (markerB) { map?.removeLayer(markerB); markerB = null; }
    if (routeLine) { map?.removeLayer(routeLine); routeLine = null; }
  },
);

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
