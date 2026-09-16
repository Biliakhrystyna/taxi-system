import { defineStore } from 'pinia';
import { ref } from 'vue';
import { useAuthStore } from './authStore';
import { useUiStore } from './uiStore';
import { orderApi } from '../services/orderApi';
import { weatherApi } from '../services/weatherApi';
import { ensureOrderHubConnected } from '../services/signalr/orderHubConnection';

// Замовлення, тариф (рахує тепер сервер), ШІ-зони та історія поїздок —
// REST + SignalR (OrderHub) до ASP.NET Core, а не Firestore.
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
  const selectedZone = ref('center');
  const showAIWarning = ref(false);
  const useSafeRoute = ref(false);

  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const passengerTrips = ref([]);
  /** @type {import('vue').Ref<import('../types/order').ApiOrder[]>} */
  const driverTrips = ref([]);
  const totalTripsCounter = ref(0);

  // email/роль поточного користувача — потрібні всередині SignalR-хендлера,
  // куди authStore напряму не передається (щоб не зв'язувати стори зайвим імпортом).
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

  // Push із SignalR OrderHub. Сервер шле подію ВСІМ клієнтам — тут фільтруємо,
  // чи вона взагалі стосується поточного користувача (той самий глобальний
  // "живий" заказ, що й раніше, тому водії бачать усі, пасажири — лише свої).
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
      currentOrder.value = order;
    }
  };

  // Предиктивний аналіз погоди через Open-Meteo — початкове значення перемикача
  // при вході в застосунок. Ручний тумблер (WeatherControls.vue) лишається як
  // свідомо підписаний demo-режим для контрольованої демонстрації на захисті.
  const fetchWeatherHazard = async () => {
    try {
      const forecast = await weatherApi.getForecast();
      isBadWeather.value = forecast.hazard_level === 'HIGH';
    } catch {
      // Бекенд/Open-Meteo недоступні — лишаємо ручний перемикач як є.
    }
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
      // Бекенд недоступний — стартуємо з порожнім станом, а не валимо весь логін.
    }

    fetchWeatherHazard();

    try {
      const connection = await ensureOrderHubConnected();
      connection.off('OrderUpdated');
      connection.on('OrderUpdated', handleOrderUpdated);
    } catch {
      // Без SignalR застосунок лишається робочим на REST, просто без realtime-оновлень.
    }
  };

  // ЛОГІКА ШІ ТА РОЗРАХУНКУ ТАРИФУ
  const checkOrderConditions = (zone) => {
    selectedZone.value = zone;
    destinationLocation.value = zone === 'center' ? 'Львів, Площа Ринок, 1' : 'Львів, Сихів (вул. Зубрівська, 12)';
    if (isBadWeather.value) {
      showAIWarning.value = true;
    } else {
      createOrder();
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
        zone: selectedZone.value,
        is_bad_weather: isBadWeather.value,
        safe_route_applied: useSafeRoute.value,
      });

      // Тариф і бонус тепер рахує сервер — просто показуємо, що повернулось.
      currentOrder.value = order;

      cardPaymentSuccess.value = false;
      cardNumber.value = '';
      cardExpiry.value = '';
      cardCvv.value = '';
    } catch {
      uiStore.triggerError("Не вдалося створити замовлення. Перевірте з'єднання з сервером.");
    }
  };

  // ОНОВЛЕННЯ СТАТУСУ ЗАМОВЛЕННЯ
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
        // Лічильник поїздок інкрементує handleOrderUpdated (SignalR-відлуння цього
        // ж запиту) — не тут, інакше цей самий клієнт порахує поїздку двічі.
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
  // Не зачіпає вже створене на сервері замовлення (раніше, з єдиним глобальним
  // Firestore-документом, ця кнопка могла стерти й активне замовлення — тепер,
  // коли кожне замовлення — реальний рядок у БД, це було б оманливо).
  const resetDemo = () => {
    pickupLocation.value = '';
    destinationLocation.value = '';
    pickupCoords.value = null;
    destinationCoords.value = null;
    useSafeRoute.value = false;
  };

  return {
    currentOrder, isBadWeather, selectedZone,
    showAIWarning, useSafeRoute, passengerTrips, driverTrips, totalTripsCounter,
    pickupLocation, destinationLocation, pickupCoords, destinationCoords,
    carClass, paymentMethod, cardNumber, cardExpiry, cardCvv, isCardPaying, cardPaymentSuccess,
    toggleWeather, resetSession, subscribeToUserData, checkOrderConditions, createOrder, updateStatus, resetDemo, fetchWeatherHazard
  };
});
