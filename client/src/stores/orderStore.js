import { acceptHMRUpdate, defineStore } from 'pinia';
import { ref } from 'vue';
import { useAuthStore } from './authStore';
import { useUiStore } from './uiStore';
import { orderApi } from '../services/orderApi';
import { usePoints } from './order/usePoints';
import { useWeather } from './order/useWeather';
import { useBooking } from './order/useBooking';
import { useOrderSync } from './order/useOrderSync';

// Склад стору: точки А/Б (usePoints), погода й небезпечні ділянки (useWeather),
// форма бронювання й ціна (useBooking), синхронізація із сервером (useOrderSync).
export const useOrderStore = defineStore('order', () => {
  /** @type {import('vue').Ref<import('../types/order').ApiOrder | null>} */
  const currentOrder = ref(null);
  const showAIWarning = ref(false);
  const useSafeRoute = ref(false);

  const points = usePoints();
  const weather = useWeather({ currentOrder, points, showAIWarning, useSafeRoute });
  const booking = useBooking({ currentOrder, points, weather, showAIWarning, useSafeRoute });
  const sync = useOrderSync({ currentOrder, points, weather, useSafeRoute });

  const resetSession = () => {
    sync.resetSync();
    currentOrder.value = null;
    showAIWarning.value = false;
    points.clearPoints();
  };

  const updateStatus = async (newStatus) => {
    if (!currentOrder.value) return;
    const authStore = useAuthStore();
    const uiStore = useUiStore();

    try {
      const order = await orderApi.updateStatus(currentOrder.value.order_id, {
        new_status: newStatus,
        driver_email: newStatus === 'accepted' ? authStore.currentUser.email : null,
        driver_name:
          newStatus === 'accepted'
            ? `${authStore.currentUser.first_name} ${authStore.currentUser.last_name}`
            : null,
      });

      if (newStatus === 'completed') {
        currentOrder.value = null;
        useSafeRoute.value = false;
        points.clearPoints();
        uiStore.triggerSuccess('Поїздку успішно завершено!');
      } else if (newStatus === 'cancelled') {
        currentOrder.value = null;
        useSafeRoute.value = false;
        points.clearPoints();
        uiStore.triggerError('Замовлення скасовано.');
      } else {
        currentOrder.value = order;
      }
    } catch {
      uiStore.triggerError("Не вдалося оновити статус замовлення. Перевірте з'єднання з сервером.");
    }
  };

  return {
    currentOrder, showAIWarning, useSafeRoute,
    pickupLocation: points.pickupLocation,
    destinationLocation: points.destinationLocation,
    pickupCoords: points.pickupCoords,
    destinationCoords: points.destinationCoords,
    isBadWeather: weather.isBadWeather,
    weatherReason: weather.weatherReason,
    routeHazards: weather.routeHazards,
    fetchWeatherHazard: weather.fetchWeatherHazard,
    carClass: booking.carClass,
    paymentMethod: booking.paymentMethod,
    cardNumber: booking.cardNumber,
    cardExpiry: booking.cardExpiry,
    cardCvv: booking.cardCvv,
    routeComparison: booking.routeComparison,
    routesCoincide: booking.routesCoincide,
    estimatedPrice: booking.estimatedPrice,
    loadRouteComparison: booking.loadRouteComparison,
    createOrder: booking.createOrder,
    passengerTrips: sync.passengerTrips,
    driverTrips: sync.driverTrips,
    totalTripsCounter: sync.totalTripsCounter,
    tripToRate: sync.tripToRate,
    subscribeToUserData: sync.subscribeToUserData,
    rateOrder: sync.rateOrder,
    dismissRating: sync.dismissRating,
    resetSession, updateStatus,
  };
});

if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useOrderStore, import.meta.hot));
}
