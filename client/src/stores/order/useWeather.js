import { ref, watch } from 'vue';
import { weatherApi } from '../../services/weatherApi';
import { routingApi } from '../../services/routingApi';
import { findHazardStretches, sampleRoute } from '../../components/map/geo/geoFilter';

/** Погода: у точці відправлення, push з сервера та небезпечні ділянки вздовж маршруту А→Б. */
export function useWeather({ currentOrder, points, showAIWarning, useSafeRoute }) {
  const { pickupCoords, destinationCoords } = points;

  const isBadWeather = ref(false);
  const weatherReason = ref('');
  /** Небезпечні (за Open-Meteo) ділянки стандартного маршруту А→Б. @type {import('vue').Ref<import('../../types/geo').HazardStretch[]>} */
  const routeHazards = ref([]);

  const fetchWeatherHazard = async (lat, lng) => {
    // Коли маршрут А→Б уже побудовано, небезпеку визначає loadRouteHazards.
    if (lat !== undefined && destinationCoords.value) return;
    try {
      const forecast = lat !== undefined && lng !== undefined
        ? await weatherApi.getForecast(lat, lng)
        : await weatherApi.getForecast();
      isBadWeather.value = forecast.hazard_level === 'HIGH';
      weatherReason.value = forecast.reason;
    } catch {
      // Без прогнозу лишається попередній стан.
    }
  };

  const handleWeatherUpdated = (forecast) => {
    if (pickupCoords.value) return;
    isBadWeather.value = forecast.hazard_level === 'HIGH';
    weatherReason.value = forecast.reason;
  };

  let routeHazardsToken = 0;

  // Препроцесинг маршруту й фільтрація небезпечних ділянок за Open-Meteo.
  const loadRouteHazards = async () => {
    const token = ++routeHazardsToken;
    routeHazards.value = [];
    if (currentOrder.value || !pickupCoords.value || !destinationCoords.value) return;

    try {
      const route = await routingApi.getRoute(pickupCoords.value, destinationCoords.value);
      if (!route || route.points.length < 2) return;

      const samples = sampleRoute(route.points);
      const forecasts = await weatherApi.getRouteForecast(samples.map((s) => s.point));
      if (token !== routeHazardsToken) return;
      if (forecasts.every((f) => f.source === 'unavailable')) return;

      const stretches = findHazardStretches(route.points, samples, forecasts);
      routeHazards.value = stretches;
      isBadWeather.value = stretches.length > 0;
      weatherReason.value = [...new Set(stretches.flatMap((st) => st.reason.split(', ')))].join(', ');
      if (!isBadWeather.value) {
        showAIWarning.value = false;
        useSafeRoute.value = false;
      }
    } catch {
      // Без даних вздовж маршруту лишається погода в точці відправлення.
    }
  };

  watch([pickupCoords, destinationCoords], loadRouteHazards, { deep: true });

  return { isBadWeather, weatherReason, routeHazards, fetchWeatherHazard, handleWeatherUpdated };
}
