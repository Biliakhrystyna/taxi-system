import { acceptHMRUpdate, defineStore } from 'pinia';
import { ref } from 'vue';
import { useAuthStore } from './authStore';
import { useUiStore } from './uiStore';
import { orderApi } from '../services/orderApi';
import { weatherApi } from '../services/weatherApi';
import { routingApi } from '../services/routingApi';
import { ensureOrderHubConnected } from '../services/signalr/orderHubConnection';

export const useOrderStore = defineStore('order', () => {
  const pickupLocation = ref('');
  const destinationLocation = ref('');
  /** @type {import('vue').Ref<import('../types/geo').LatLng | null>} */
  const pickupCoords = ref(null);
  /** @type {import('vue').Ref<import('../types/geo').LatLng | null>} */
  const destinationCoords = ref(null);
  const carClass = ref('comfort');

  const paymentMethod = ref('cash');
  const cardNumber = ref('');
  const cardExpiry = ref('');
  const cardCvv = ref('');
  const isCardPaying = ref(false);
  const cardPaymentSuccess = ref(false);

  /** @type {import('vue').Ref<import('../types/order').ApiOrder | null>} */
  const currentOrder = ref(null);
  const isBadWeather = ref(false);
  const showAIWarning = ref(false);
  const useSafeRoute = ref(false);
  /** @type {import('vue').Ref<{standardKm: number|null, standardTurns: number|null, safeKm: number|null, safeTurns: number|null}|null>} */
  const routeComparison = ref(null);

  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const passengerTrips = ref([]);
  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const driverTrips = ref([]);
  const totalTripsCounter = ref(0);

  // email/роль поточного користувача — потрібні всередині SignalR-хендлера,
  let sessionEmail = null;
  let sessionRole = null;

  const toggleWeather = () => {
    isBadWeather.value = !isBadWeather.value;

    if (!isBadWeather.value) {
      showAIWarning.value = false;
      useSafeRoute.value = false;
    }
  };

  const resetSession = () => {
    sessionEmail = null;
    sessionRole = null;
    currentOrder.value = null;
    passengerTrips.value = [];
    driverTrips.value = [];
    totalTripsCounter.value = 0;
    showAIWarning.value = false;
    pickupLocation.value = '';
    destinationLocation.value = '';
    pickupCoords.value = null;
    destinationCoords.value = null;
  };

  const refreshHistory = async () => {
    if (!sessionEmail) return;
    const trips = await orderApi.getHistory(sessionEmail, sessionRole);
    if (sessionRole === 'driver') {
      driverTrips.value = trips;
    } else {
      passengerTrips.value = trips;
    }
  };


  const handleOrderUpdated = (order) => {
    if (!sessionEmail) return;
    const isMine = sessionRole === 'driver' || order.passenger_email === sessionEmail;
    if (!isMine) return;

    if (order.current_status === 'completed' || order.current_status === 'cancelled') {
      currentOrder.value = null;
      const isMyTrip = order.passenger_email === sessionEmail || order.driver_email === sessionEmail;
      if (isMyTrip) {
        // Лічильник росте лише за завершені поїздки, не за скасовані.
        if (order.current_status === 'completed') {
          totalTripsCounter.value += 1;
        }
        refreshHistory();
      }
    } else {
      const driverJustAssigned =
        sessionRole === 'passenger' &&
        order.current_status === 'accepted' &&
        order.driver_name &&
        currentOrder.value?.current_status !== 'accepted';

      currentOrder.value = order;

      if (driverJustAssigned) {
        useUiStore().triggerSuccess(`🚖 Водія призначено: ${order.driver_name}!`);
      }
    }
  };

  // Аналіз погоди через Open-Meteo для конкретної точки 
  const fetchWeatherHazard = async (lat, lng) => {
    try {
      const forecast = lat !== undefined && lng !== undefined
        ? await weatherApi.getForecast(lat, lng)
        : await weatherApi.getForecast();
      isBadWeather.value = forecast.hazard_level === 'HIGH';
    } catch {

    }
  };


  const handleWeatherUpdated = (forecast) => {
    if (pickupCoords.value) return;
    isBadWeather.value = forecast.hazard_level === 'HIGH';
  };

  // Ініціалізація сесії після успішного логіну/реєстрації/верифікації:
  // поточне замовлення й історія одноразово через REST, далі — SignalR push.
  const subscribeToUserData = async (email, role) => {
    if (!email) return;

    sessionEmail = email;
    sessionRole = role;
    totalTripsCounter.value = 0;

    try {
      const [current, history] = await Promise.all([
        orderApi.getCurrent(email, role),
        orderApi.getHistory(email, role),
      ]);

      currentOrder.value = current;
      if (role === 'driver') {
        driverTrips.value = history;
      } else {
        passengerTrips.value = history;
      }
    } catch {
     
    }

    fetchWeatherHazard();

    try {
      const connection = await ensureOrderHubConnected();
      connection.off('OrderUpdated');
      connection.on('OrderUpdated', handleOrderUpdated);
      connection.off('WeatherUpdated');
      connection.on('WeatherUpdated', handleWeatherUpdated);
    } catch {
      // Без SignalR застосунок лишається робочим на REST, просто без realtime-оновлень.
    }
  };


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
        pickup_lat: pickupCoords.value?.lat ?? null,
        pickup_lng: pickupCoords.value?.lng ?? null,
        destination_lat: destinationCoords.value?.lat ?? null,
        destination_lng: destinationCoords.value?.lng ?? null,
      });

      
      currentOrder.value = order;

      cardPaymentSuccess.value = false;
      cardNumber.value = '';
      cardExpiry.value = '';
      cardCvv.value = '';
    } catch {
      uiStore.triggerError("Не вдалося створити замовлення. Перевірте з'єднання з сервером.");
    }
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
        pickupLocation.value = '';
        destinationLocation.value = '';
        pickupCoords.value = null;
        destinationCoords.value = null;
        uiStore.triggerSuccess('Поїздку успішно завершено! Каунтери оновлено.');
      } else if (newStatus === 'cancelled') {
        currentOrder.value = null;
        useSafeRoute.value = false;
        pickupLocation.value = '';
        destinationLocation.value = '';
        pickupCoords.value = null;
        destinationCoords.value = null;
        uiStore.triggerSuccess('Замовлення скасовано.');
      } else {
        currentOrder.value = order;
      }
    } catch {
      uiStore.triggerError("Не вдалося оновити статус замовлення. Перевірте з'єднання з сервером.");
    }
  };

  // Очищення локально введених точок А/Б перед формуванням нового замовлення.
  const resetDemo = () => {
    pickupLocation.value = '';
    destinationLocation.value = '';
    pickupCoords.value = null;
    destinationCoords.value = null;
    useSafeRoute.value = false;
  };

  return {
    currentOrder, isBadWeather,
    showAIWarning, useSafeRoute, routeComparison, passengerTrips, driverTrips, totalTripsCounter,
    pickupLocation, destinationLocation, pickupCoords, destinationCoords,
    carClass, paymentMethod, cardNumber, cardExpiry, cardCvv, isCardPaying, cardPaymentSuccess,
    toggleWeather, resetSession, subscribeToUserData, createOrder, updateStatus, resetDemo, fetchWeatherHazard, loadRouteComparison
  };
});


if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useOrderStore, import.meta.hot));
}
