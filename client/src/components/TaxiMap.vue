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

<script setup>
import { onMounted, watch } from 'vue';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { useOrderStore } from '../stores/orderStore';

const props = defineProps({
  role: String,
  isBadWeather: Boolean
});

const orderStore = useOrderStore();

let map = null;
let markerA = null;
let markerB = null;
let routeLine = null; 
let heatCircles = [];
const LVIV_CENTER = [49.8419, 24.0315];
let aiGeneratedDeficitZones = [];

// 1. Генератор випадкових зон
const generateRandomOutskirtsZones = (centerLat, centerLng, count = 3) => {
  const zones = [];
  for (let i = 0; i < count; i++) {
    const angle = Math.random() * Math.PI * 2;
    const distanceKm = 2.0 + Math.random() * 2.5;
    const latOffset = (distanceKm / 111.32) * Math.sin(angle);
    const lngOffset = (distanceKm / (111.32 * Math.cos(centerLat * Math.PI / 180))) * Math.cos(angle);
    zones.push({
      id: `ZONE_${Date.now()}_${i}`,
      lat: centerLat + latOffset,
      lng: centerLng + lngOffset,
      radius: 1000 + Math.random() * 300
    });
  }
  return zones;
};

// 2. Рендер зон 
const renderAllZones = () => {
  if (!map) return;
  heatCircles.forEach(circle => map.removeLayer(circle));
  heatCircles = [];


  if (props.role === 'passenger') return;

  // Зелені зони
  [{ coords: [49.8419, 24.0315], rad: 800 }, { coords: [49.8350, 24.0150], rad: 600 }].forEach(cz => {
    const gc = L.circle(cz.coords, { color: '#10b981', fillColor: '#34d399', fillOpacity: 0.1, radius: cz.rad, interactive: false }).addTo(map);
    heatCircles.push(gc);
  });

  // Червоні зони дефіциту
  aiGeneratedDeficitZones.forEach((zone) => {
    const rc = L.circle([zone.lat, zone.lng], {
      color: '#ef4444',
      fillColor: '#f87171',
      fillOpacity: orderStore.isBadWeather ? 0.5 : 0.25,
      radius: zone.radius,
      interactive: false
    }).addTo(map);
    heatCircles.push(rc);
  });
};

// 3. Універсальна функція малювання ліній (для пасажира і водія)
const drawRoute = (latA, lngA, latB, lngB, isSafeApplied) => {
  if (!map) return;
  if (routeLine) map.removeLayer(routeLine);

  const posA = [latA, lngA];
  const posB = [latB, lngB];

  if (isSafeApplied && orderStore.isBadWeather) {
    const arcPoints = [];
    const steps = 10; 

    for (let i = 0; i <= steps; i++) {
      const t = i / steps;
      
      // Лінійна інтерполяція (пряма лінія між А та В)
      const lat = latA + (latB - latA) * t;
      const lng = lngA + (lngB - lngA) * t;
      const wave = Math.sin(t * Math.PI) * 0.0035; 
      
      arcPoints.push([lat + wave, lng - wave]);
    }

    
    routeLine = L.polyline(arcPoints, {
      color: '#f97316', 
      weight: 5,
      dashArray: '6, 6', 
      opacity: 0.95
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent('🏁 Точка В<br><b style="color: #f97316;">🛡️ ШІ-маршрут: Оптимальний безпечний обхід</b>').openPopup();
    }
  } else {
    // Стандартний найкоротший прямий шлях (синій колір)
    routeLine = L.polyline([posA, posB], {
      color: '#38bdf8',
      weight: 4,
      opacity: 0.8
    }).addTo(map);

    if (markerB && props.role === 'passenger') {
      markerB.setPopupContent('🏁 Точка В<br><b style="color: #38bdf8;">✨ Стандартний найкоротший шлях</b>').openPopup();
    }
  }
};
   

onMounted(() => {
  map = L.map('map').setView(LVIV_CENTER, 12);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '© OpenStreetMap' }).addTo(map);

  aiGeneratedDeficitZones = generateRandomOutskirtsZones(LVIV_CENTER[0], LVIV_CENTER[1], 3);
  renderAllZones();

  
  if (props.role === 'passenger') {
    map.on('click', (e) => {
      const { lat, lng } = e.latlng;

      if (!markerA) {
        // Перший клік — Точка А
        markerA = L.marker([lat, lng]).addTo(map).bindPopup('📍 Точка А (Звідки)').openPopup();
        orderStore.pickupLocation = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
      } else if (!markerB) {
        // Другий клік — Точка В
        markerB = L.marker([lat, lng]).addTo(map).bindPopup('🏁 Точка В (Куди)').openPopup();
        orderStore.destinationLocation = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
        
        // Перевірка на дефіцит через ШІ-геофенсинг
        let insideDeficitZone = false;
        for (let zone of aiGeneratedDeficitZones) {
          const distance = map.distance([lat, lng], [zone.lat, zone.lng]);
          if (distance <= zone.radius) { insideDeficitZone = true; break; }
        }
        orderStore.selectedZone = insideDeficitZone ? 'outskirts' : 'center';
        
        
        drawRoute(markerA.getLatLng().lat, markerA.getLatLng().lng, lat, lng, orderStore.useSafeRoute);
      } else {
        // Третій клік — повне скидання
        if (markerA) map.removeLayer(markerA);
        if (markerB) map.removeLayer(markerB);
        if (routeLine) map.removeLayer(routeLine);
        markerA = null; markerB = null; routeLine = null;
        orderStore.pickupLocation = '';
        orderStore.destinationLocation = '';
        orderStore.selectedZone = 'center';
      }
    });
  }
});


watch(() => orderStore.currentOrder, (newOrder) => {
  if (!map || props.role === 'passenger') return; 

  if (newOrder && newOrder.order_id && newOrder.pickup_location && newOrder.destination) {
    const parseCoords = (str) => {
      const parts = str.replace(/[^\d.,-]/g, '').split(',');
      return [parseFloat(parts[0]), parseFloat(parts[1])];
    };

    try {
      const coordsA = parseCoords(newOrder.pickup_location);
      const coordsB = parseCoords(newOrder.destination);

      if (!markerA) markerA = L.marker(coordsA).addTo(map).bindPopup('📍 Пасажир тут');
      if (!markerB) markerB = L.marker(coordsB).addTo(map).bindPopup('🏁 Кінцева точка рейсу');

      drawRoute(coordsA[0], coordsA[1], coordsB[0], coordsB[1], newOrder.safe_route_applied);
    } catch (e) {
      console.log("Парсинг адресних координат...");
    }
  } else {
    
    if (markerA) map.removeLayer(markerA);
    if (markerB) map.removeLayer(markerB);
    if (routeLine) map.removeLayer(routeLine);
    markerA = null; markerB = null; routeLine = null;
  }
}, { deep: true });

watch(() => orderStore.useSafeRoute, () => {
  if (props.role === 'passenger' && markerA && markerB) {
    drawRoute(markerA.getLatLng().lat, markerA.getLatLng().lng, markerB.getLatLng().lat, markerB.getLatLng().lng, orderStore.useSafeRoute);
  }
});

watch(() => orderStore.isBadWeather, () => {
  renderAllZones();
  if (props.role === 'passenger' && markerA && markerB) {
    drawRoute(markerA.getLatLng().lat, markerA.getLatLng().lng, markerB.getLatLng().lat, markerB.getLatLng().lng, orderStore.useSafeRoute);
  }
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