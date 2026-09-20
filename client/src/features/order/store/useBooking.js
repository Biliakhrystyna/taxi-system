import { computed, ref, watch } from 'vue';
import { useAuthStore } from '../../auth/store/authStore';
import { useUiStore } from '../../../shared/uiStore';
import { orderApi } from '../orderApi';
import { routingApi } from '../../map/routingApi';

/** Форма бронювання: клас авто, оплата, картка, порівняння маршрутів, ціна та створення замовлення. */
export function useBooking({ currentOrder, points, weather, showAIWarning, useSafeRoute }) {
  const { pickupLocation, destinationLocation, pickupCoords, destinationCoords } = points;
  const { isBadWeather } = weather;

  const carClass = ref('comfort');
  const paymentMethod = ref('cash');
  const cardNumber = ref('');
  const cardExpiry = ref('');
  const cardCvv = ref('');

  /** @type {import('vue').Ref<{standardKm: number|null, standardTurns: number|null, safeKm: number|null, safeTurns: number|null}|null>} */
  const routeComparison = ref(null);

  // Орієнтовна ціна, яку пасажир бачить до створення замовлення.
  /** @type {import('vue').Ref<number | null>} */
  const estimatedPrice = ref(null);

  // Безпечний маршрут збігається зі стандартним: об'їзду немає, надбавки немає.
  const routesCoincide = computed(() => {
    const cmp = routeComparison.value;
    if (!cmp || cmp.standardKm === null || cmp.safeKm === null) return false;
    return Math.abs(cmp.standardKm - cmp.safeKm) < 0.05 && cmp.standardTurns === cmp.safeTurns;
  });

  const loadRouteComparison = async () => {
    routeComparison.value = null;
    if (!pickupCoords.value || !destinationCoords.value) return;

    try {
      const [standard, safe] = await Promise.all([
        routingApi.getRoute(pickupCoords.value, destinationCoords.value),
        routingApi.getSafeRoute(pickupCoords.value, destinationCoords.value),
      ]);

      routeComparison.value = {
        standardKm: standard ? standard.distance_meters / 1000 : null,
        standardTurns: standard ? standard.turn_count : null,
        safeKm: safe ? safe.distance_meters / 1000 : null,
        safeTurns: safe ? safe.turn_count : null,
      };
    } catch {
      routeComparison.value = null;
    }
  };

  const loadPriceQuote = async () => {
    if (currentOrder.value || !pickupCoords.value || !destinationCoords.value) {
      estimatedPrice.value = null;
      return;
    }

    try {
      const quote = await orderApi.getQuote({
        car_class: carClass.value,
        is_bad_weather: isBadWeather.value,
        safe_route_applied: useSafeRoute.value,
        safe_route_matches_standard: routesCoincide.value,
        pickup_lat: pickupCoords.value.lat,
        pickup_lng: pickupCoords.value.lng,
        destination_lat: destinationCoords.value.lat,
        destination_lng: destinationCoords.value.lng,
      });
      estimatedPrice.value = quote.estimated_cost;
    } catch {
      estimatedPrice.value = null;
    }
  };

  watch(
    [carClass, isBadWeather, useSafeRoute, routesCoincide, pickupCoords, destinationCoords],
    loadPriceQuote,
    { deep: true },
  );

  const createOrder = async () => {
    const authStore = useAuthStore();
    const uiStore = useUiStore();
    showAIWarning.value = false;

    try {
      const order = await orderApi.create({
        passenger_email: authStore.currentUser.email,
        pickup_location: pickupLocation.value.trim() || 'Поточне місцезнаходження пасажира',
        destination: destinationLocation.value,
        car_class: carClass.value,
        payment_method: paymentMethod.value,
        is_bad_weather: isBadWeather.value,
        safe_route_applied: useSafeRoute.value,
        safe_route_matches_standard: routesCoincide.value,
        pickup_lat: pickupCoords.value?.lat ?? null,
        pickup_lng: pickupCoords.value?.lng ?? null,
        destination_lat: destinationCoords.value?.lat ?? null,
        destination_lng: destinationCoords.value?.lng ?? null,
      });

      currentOrder.value = order;

      cardNumber.value = '';
      cardExpiry.value = '';
      cardCvv.value = '';
    } catch {
      uiStore.triggerError("Не вдалося створити замовлення. Перевірте з'єднання з сервером.");
    }
  };

  return {
    carClass, paymentMethod, cardNumber, cardExpiry, cardCvv,
    routeComparison, routesCoincide, estimatedPrice,
    loadRouteComparison, createOrder,
  };
}
