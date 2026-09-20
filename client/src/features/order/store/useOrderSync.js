import { ref } from 'vue';
import { useAuthStore } from '../../auth/store/authStore';
import { useUiStore } from '../../../shared/uiStore';
import { orderApi } from '../orderApi';
import { ensureOrderHubConnected, onOrderHubReconnected } from '../orderHubConnection';

/** Синхронізація замовлень із сервером: SignalR-push, опит, історія, лічильник поїздок та оцінка. */
export function useOrderSync({ currentOrder, points, weather, useSafeRoute }) {
  /** @type {import('vue').Ref<import('../../../shared/types/order').ApiOrder[]>} */
  const passengerTrips = ref([]);
  /** @type {import('vue').Ref<import('../../../shared/types/order').ApiOrder[]>} */
  const driverTrips = ref([]);
  const totalTripsCounter = ref(0);

  /** @type {import('vue').Ref<import('../../../shared/types/order').ApiOrder | null>} */
  const tripToRate = ref(null);

  // email/роль поточного користувача для SignalR-хендлерів і опитування.
  let sessionEmail = null;
  let sessionRole = null;
  let driverPollInterval = null;
  let passengerPollInterval = null;

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

    if (sessionRole === 'driver') {
      const isMyOwnOrder = order.driver_email === sessionEmail;
      const isCurrentlyDisplayed = currentOrder.value?.order_id === order.order_id;

      if (!isMyOwnOrder && !isCurrentlyDisplayed) {
        const driverCarClass = useAuthStore().currentUser?.driver_car_class;
        const isNewMatchingOffer =
          !currentOrder.value && order.current_status === 'waiting' &&
          !order.driver_email && order.car_class === driverCarClass;

        if (!isNewMatchingOffer) return;

        orderApi.getCurrent(sessionEmail, 'driver').then((o) => { currentOrder.value = o; });
        return;
      }

      if (!isMyOwnOrder && order.driver_email) {
        orderApi.getCurrent(sessionEmail, 'driver').then((o) => { currentOrder.value = o; });
        return;
      }
    } else if (order.passenger_email !== sessionEmail) {
      return;
    }

    if (order.current_status === 'completed' || order.current_status === 'cancelled') {
      const wasMyActiveOrder = currentOrder.value?.order_id === order.order_id;
      currentOrder.value = null;
      const isMyTrip = order.passenger_email === sessionEmail || order.driver_email === sessionEmail;
      if (isMyTrip) {
        if (order.current_status === 'completed' && wasMyActiveOrder) {
          totalTripsCounter.value += 1;
          const authUser = useAuthStore().currentUser;
          if (authUser) authUser.total_trips = totalTripsCounter.value;
          if (sessionRole === 'passenger') {
            tripToRate.value = order;
          }
        }
        if (wasMyActiveOrder && sessionRole === 'passenger') {
          useSafeRoute.value = false;
          points.clearPoints();
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

  // SignalR-push може "заснути" разом із вкладкою, тож активне замовлення звіряємо з сервером;
  // завершення визначаємо за історією.
  const syncActiveOrder = async () => {
    const active = currentOrder.value;
    if (!sessionRole || !sessionEmail || !active?.order_id) return;

    try {
      const current = await orderApi.getCurrent(sessionEmail, sessionRole);

      if (current && current.order_id === active.order_id) {
        if (current.current_status !== active.current_status || current.driver_name !== active.driver_name) {
          handleOrderUpdated(current);
        }
        return;
      }

      const isOwnOrder = sessionRole === 'passenger' || active.driver_email === sessionEmail;
      if (isOwnOrder) {
        const history = await orderApi.getHistory(sessionEmail, sessionRole);
        const finished = history.find((o) => o.order_id === active.order_id);
        if (finished) handleOrderUpdated(finished);
        return;
      }

      // Пропозицію, яку водій розглядав, вже забрав інший водій або її скасовано.
      currentOrder.value = current;
    } catch {
      // Тимчасова мережева помилка — спробуємо знову на наступному тіку.
    }
  };

  const syncOnTabVisible = () => {
    if (document.visibilityState === 'visible') syncActiveOrder();
  };

  const startDriverPolling = () => {
    if (driverPollInterval) return;
    document.addEventListener('visibilitychange', syncOnTabVisible);
    driverPollInterval = setInterval(async () => {
      if (sessionRole !== 'driver' || !sessionEmail) return;
      if (currentOrder.value) {
        syncActiveOrder();
        return;
      }
      try {
        const current = await orderApi.getCurrent(sessionEmail, 'driver');
        if (current) currentOrder.value = current;
      } catch {
        // Тимчасова мережева помилка — спробуємо знову на наступному тіку.
      }
    }, 5000);
  };

  const stopDriverPolling = () => {
    if (driverPollInterval) {
      clearInterval(driverPollInterval);
      driverPollInterval = null;
      document.removeEventListener('visibilitychange', syncOnTabVisible);
    }
  };

  const startPassengerPolling = () => {
    if (passengerPollInterval) return;
    passengerPollInterval = setInterval(syncActiveOrder, 5000);
    document.addEventListener('visibilitychange', syncOnTabVisible);
  };

  const stopPassengerPolling = () => {
    if (passengerPollInterval) {
      clearInterval(passengerPollInterval);
      passengerPollInterval = null;
      document.removeEventListener('visibilitychange', syncOnTabVisible);
    }
  };

  // Після перезавантаження відновлюємо точки А/Б із активного замовлення.
  const restorePassengerPoints = (order) => {
    if (!order?.order_id) return;
    const { pickup_lat, pickup_lng, destination_lat, destination_lng } = order;
    if ([pickup_lat, pickup_lng, destination_lat, destination_lng].some((v) => v === null || v === undefined)) return;

    points.pickupLocation.value = order.pickup_location;
    points.destinationLocation.value = order.destination;
    weather.isBadWeather.value = order.weather_hazard_level === 'HIGH';
    useSafeRoute.value = !!order.safe_route_applied;
    points.pickupCoords.value = { lat: pickup_lat, lng: pickup_lng };
    points.destinationCoords.value = { lat: destination_lat, lng: destination_lng };
  };

  // Ініціалізація сесії: поточне замовлення й історія через REST, далі SignalR push.
  const subscribeToUserData = async (email, role) => {
    if (!email) return;

    sessionEmail = email;
    sessionRole = role;
    // Кількість завершених поїздок у поточній ролі приходить із сервера.
    totalTripsCounter.value = useAuthStore().currentUser?.total_trips ?? 0;

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
        restorePassengerPoints(current);
      }
    } catch {
      // Без початкових даних кабінет відкриється порожнім і оновиться опитуванням.
    }

    if (role === 'driver') startDriverPolling();
    else startPassengerPolling();

    weather.fetchWeatherHazard();

    try {
      const connection = await ensureOrderHubConnected();
      connection.off('OrderUpdated');
      connection.on('OrderUpdated', handleOrderUpdated);
      connection.off('WeatherUpdated');
      connection.on('WeatherUpdated', weather.handleWeatherUpdated);

      onOrderHubReconnected(() => {
        if (!sessionEmail) return;
        if (currentOrder.value?.order_id) {
          syncActiveOrder();
          return;
        }
        Promise.all([
          orderApi.getCurrent(sessionEmail, sessionRole),
          orderApi.getHistory(sessionEmail, sessionRole),
        ]).then(([current, history]) => {
          currentOrder.value = current;
          if (sessionRole === 'driver') {
            driverTrips.value = history;
          } else {
            passengerTrips.value = history;
          }
        }).catch(() => {});
      });
    } catch {
      // Без SignalR застосунок лишається робочим на REST, просто без realtime-оновлень.
    }
  };

  const resetSync = () => {
    sessionEmail = null;
    sessionRole = null;
    stopDriverPolling();
    stopPassengerPolling();
    passengerTrips.value = [];
    driverTrips.value = [];
    totalTripsCounter.value = 0;
    tripToRate.value = null;
  };

  const rateOrder = async (orderId, rating) => {
    const authStore = useAuthStore();
    const uiStore = useUiStore();

    try {
      await orderApi.rateOrder(orderId, {
        passenger_email: authStore.currentUser.email,
        rating,
      });
      if (tripToRate.value?.order_id === orderId) tripToRate.value = null;
      await refreshHistory();
      uiStore.triggerSuccess('Дякуємо за оцінку!');
    } catch {
      uiStore.triggerError('Не вдалося зберегти оцінку.');
    }
  };

  // Пасажир закрив модалку без оцінки — рейс так і лишається неоціненим.
  const dismissRating = () => {
    tripToRate.value = null;
  };

  return {
    passengerTrips, driverTrips, totalTripsCounter, tripToRate,
    subscribeToUserData, resetSync, rateOrder, dismissRating,
  };
}
