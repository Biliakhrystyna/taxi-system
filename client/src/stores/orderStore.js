import { acceptHMRUpdate, defineStore } from 'pinia';
import { computed, ref, watch } from 'vue';
import { useAuthStore } from './authStore';
import { useUiStore } from './uiStore';
import { orderApi } from '../services/orderApi';
import { weatherApi } from '../services/weatherApi';
import { routingApi } from '../services/routingApi';
import { ensureOrderHubConnected, onOrderHubReconnected } from '../services/signalr/orderHubConnection';

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
  const weatherReason = ref('');
  const showAIWarning = ref(false);
  const useSafeRoute = ref(false);
  /** @type {import('vue').Ref<{standardKm: number|null, standardTurns: number|null, safeKm: number|null, safeTurns: number|null}|null>} */
  const routeComparison = ref(null);

  // Орієнтовна ціна — щоб пасажир бачив вартість ДО оплати/бронювання, а не
  // вперше вже після формування замовлення.
  /** @type {import('vue').Ref<number | null>} */
  const estimatedPrice = ref(null);

  // Коли для пари точок реально немає об'їзду з меншою кількістю поворотів,
  // "безпечний" маршрут дорівнює стандартному — не варто ні показувати це як
  // окремий об'їзд, ні брати за нього надбавку до ціни.
  const routesCoincide = computed(() => {
    const cmp = routeComparison.value;
    if (!cmp || cmp.standardKm === null || cmp.safeKm === null) return false;
    return Math.abs(cmp.standardKm - cmp.safeKm) < 0.05 && cmp.standardTurns === cmp.safeTurns;
  });

  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const passengerTrips = ref([]);
  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const driverTrips = ref([]);
  const totalTripsCounter = ref(0);

  // Замовлення, щойно завершене водієм, яке пасажир ще не оцінив — модалка
  // "Оцініть поїздку" показується, поки тут не null.
  /** @type {import('vue').Ref<import('../types/order').ApiOrder | null>} */
  const tripToRate = ref(null);

  // email/роль поточного користувача — потрібні всередині SignalR-хендлера,
  let sessionEmail = null;
  let sessionRole = null;
  let driverPollInterval = null;

  const toggleWeather = () => {
    isBadWeather.value = !isBadWeather.value;

    if (!isBadWeather.value) {
      showAIWarning.value = false;
      useSafeRoute.value = false;
    }
  };

  // SignalR-push — швидкий, але не 100% надійний шлях (з'єднання може
  // "зомбі"-зависнути, виглядаючи підключеним, і мовчки не доставляти дані —
  // відома проблема довгих WebSocket-з'єднань). Це незалежний запасний
  // варіант: поки водій не має свого замовлення, час від часу самі питаємо
  // сервер — навіть якщо push повністю мовчить, водій не застрягне довше
  // ніж на цей інтервал.
  const startDriverPolling = () => {
    if (driverPollInterval) return;
    driverPollInterval = setInterval(async () => {
      if (sessionRole !== 'driver' || !sessionEmail || currentOrder.value) return;
      try {
        const current = await orderApi.getCurrent(sessionEmail, 'driver');
        if (current) currentOrder.value = current;
      } catch {
        // Тимчасова мережева помилка — спробуємо знову на наступному тіку.
      }
    }, 15000);
  };

  const stopDriverPolling = () => {
    if (driverPollInterval) {
      clearInterval(driverPollInterval);
      driverPollInterval = null;
    }
  };

  const resetSession = () => {
    sessionEmail = null;
    sessionRole = null;
    stopDriverPolling();
    currentOrder.value = null;
    passengerTrips.value = [];
    driverTrips.value = [];
    totalTripsCounter.value = 0;
    tripToRate.value = null;
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

    if (sessionRole === 'driver') {
      const isMyOwnOrder = order.driver_email === sessionEmail;
      const isCurrentlyDisplayed = currentOrder.value?.order_id === order.order_id;

      if (!isMyOwnOrder && !isCurrentlyDisplayed) {
        // Broadcast летить УСІМ клієнтам для БУДЬ-ЯКОГО замовлення в системі.
        // Приймаємо чужий broadcast лише як сигнал "з'явилась нова пропозиція
        // саме мого класу авто, а в мене зараз немає рейсу" — і тоді питаємо
        // в сервера авторитетну відповідь (FIFO+клас), а не довіряємо сирому
        // payload напряму. Інакше водій підхоплював випадкове чуже
        // замовлення (не свого класу, не своє) — звідси "випадкове ім'я".
        const driverCarClass = useAuthStore().currentUser?.driver_car_class;
        const isNewMatchingOffer =
          !currentOrder.value && order.current_status === 'waiting' &&
          !order.driver_email && order.car_class === driverCarClass;

        if (!isNewMatchingOffer) return;

        orderApi.getCurrent(sessionEmail, 'driver').then((o) => { currentOrder.value = o; });
        return;
      }

      if (!isMyOwnOrder && order.driver_email) {
        // Замовлення, яке я саме розглядав як "очікує", щойно перехопив
        // інший водій — питаємо в сервера, що реально показувати мені далі,
        // а не підміняємо екран чужим прийнятим рейсом.
        orderApi.getCurrent(sessionEmail, 'driver').then((o) => { currentOrder.value = o; });
        return;
      }
    } else if (order.passenger_email !== sessionEmail) {
      return;
    }

    if (order.current_status === 'completed' || order.current_status === 'cancelled') {
      // Оцінка поїздки згодом теж шле "OrderUpdated" із тим самим
      // current_status === 'completed' (щоб історія й лічильник в іншої
      // сторони оновились) — без цієї перевірки кожен такий повторний
      // broadcast знову рахувався б як нове завершення й задвоював лічильник.
      const wasMyActiveOrder = currentOrder.value?.order_id === order.order_id;
      currentOrder.value = null;
      const isMyTrip = order.passenger_email === sessionEmail || order.driver_email === sessionEmail;
      if (isMyTrip) {
        if (order.current_status === 'completed' && wasMyActiveOrder) {
          totalTripsCounter.value += 1;
          if (sessionRole === 'passenger') {
            tripToRate.value = order;
          }
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
      weatherReason.value = forecast.reason;
    } catch {

    }
  };


  const handleWeatherUpdated = (forecast) => {
    if (pickupCoords.value) return;
    isBadWeather.value = forecast.hazard_level === 'HIGH';
    weatherReason.value = forecast.reason;
  };

  // Ініціалізація сесії після успішного логіну/реєстрації/верифікації:
  // поточне замовлення й історія одноразово через REST, далі — SignalR push.
  const subscribeToUserData = async (email, role) => {
    if (!email) return;

    sessionEmail = email;
    sessionRole = role;
    // Реальна к-сть поїздок зберігається на акаунті (User.TotalTrips) — тут
    // лише продовжуємо з неї, а не скидаємо лічильник у 0 щоразу при вході.
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
      }
    } catch {

    }

    if (role === 'driver') startDriverPolling();

    fetchWeatherHazard();

    try {
      const connection = await ensureOrderHubConnected();
      connection.off('OrderUpdated');
      connection.on('OrderUpdated', handleOrderUpdated);
      connection.off('WeatherUpdated');
      connection.on('WeatherUpdated', handleWeatherUpdated);
      // З'єднання могло обірватись надовше, ніж вбудований reconnect
      // намагається сам (напр. ноутбук заснув) — поки зв'язку не було, ми
      // пропустили всі broadcast. Коли SignalR оживає, дозаписуємо
      // авторитетний стан через REST, а не чекаємо наступної випадкової події.
      onOrderHubReconnected(() => {
        if (!sessionEmail) return;
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

  // Живе оновлення кошторису — щойно обрано обидві точки, і далі при зміні
  // класу авто/погоди/безпечного маршруту, без потреби тиснути "Сформувати
  // замовлення", щоб уперше побачити ціну.
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
        uiStore.triggerError('Замовлення скасовано.');
      } else {
        currentOrder.value = order;
      }
    } catch {
      uiStore.triggerError("Не вдалося оновити статус замовлення. Перевірте з'єднання з сервером.");
    }
  };

  // Оцінка поїздки пасажиром — з модалки одразу після завершення (сервер
  // однаково не дасть оцінити двічі чи чужу поїздку).
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

  // Очищення локально введених точок А/Б перед формуванням нового замовлення.
  const resetDemo = () => {
    pickupLocation.value = '';
    destinationLocation.value = '';
    pickupCoords.value = null;
    destinationCoords.value = null;
    useSafeRoute.value = false;
  };

  return {
    currentOrder, isBadWeather, weatherReason,
    showAIWarning, useSafeRoute, routeComparison, routesCoincide, passengerTrips, driverTrips, totalTripsCounter,
    pickupLocation, destinationLocation, pickupCoords, destinationCoords,
    carClass, paymentMethod, cardNumber, cardExpiry, cardCvv, isCardPaying, cardPaymentSuccess,
    tripToRate, estimatedPrice,
    toggleWeather, resetSession, subscribeToUserData, createOrder, updateStatus, resetDemo, fetchWeatherHazard, loadRouteComparison, rateOrder, dismissRating
  };
});


if (import.meta.hot) {
  import.meta.hot.accept(acceptHMRUpdate(useOrderStore, import.meta.hot));
}
