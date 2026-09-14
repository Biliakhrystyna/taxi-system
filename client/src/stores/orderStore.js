import { defineStore } from 'pinia';
import { ref } from 'vue';
import { db } from '../firebase';
import { doc, setDoc, updateDoc, onSnapshot, getDoc, collection, query, where } from 'firebase/firestore';
import { useAuthStore } from './authStore';
import { useUiStore } from './uiStore';

// Замовлення, тариф, ШІ-зони та історія поїздок.
// TODO(backend): замінити Firestore на REST + SignalR (OrderHub) до
// ASP.NET Core API — див. docs/ARCHITECTURE.md.
export const useOrderStore = defineStore('order', () => {
  const pickupLocation = ref('');
  const destinationLocation = ref('');
  const carClass = ref('comfort');

  const paymentMethod = ref('cash');
  const cardNumber = ref('');
  const cardExpiry = ref(''); // Термін дії (MM/YY)
  const cardCvv = ref('');
  const isCardPaying = ref(false);
  const cardPaymentSuccess = ref(false);

  // Стан замовлень та ШІ
  const currentOrder = ref(null);
  const isBadWeather = ref(false);
  const selectedZone = ref('center');
  const showAIWarning = ref(false);
  const useSafeRoute = ref(false);

  const toggleWeather = () => {
    isBadWeather.value = !isBadWeather.value;

    if (!isBadWeather.value) {
      showAIWarning.value = false;
      useSafeRoute.value = false;
    }
  };

  // Історія та каунтери
  const passengerTrips = ref([]);
  const driverTrips = ref([]);
  const totalTripsCounter = ref(0);

  // Скидання замовлення/історії поточної сесії — використовується
  // на початку кожної спроби входу/реєстрації та при виході з акаунту.
  const resetSession = () => {
    currentOrder.value = null;
    passengerTrips.value = [];
    driverTrips.value = [];
    totalTripsCounter.value = 0;
    showAIWarning.value = false;
    pickupLocation.value = '';
    destinationLocation.value = '';
  };

  // Налаштування real-time відстеження каунтерів, історії та поточного замовлення
  const subscribeToUserData = (email, role) => {
    if (!email) return;

    onSnapshot(doc(db, "users", email), (snap) => {
      if (snap.exists()) {
        totalTripsCounter.value = snap.data().total_trips || 0;
      }
    });

    onSnapshot(doc(db, "demo_orders", "live_order"), (snapshot) => {
      currentOrder.value = snapshot.exists() ? snapshot.data() : null;
    });

    const isDriver = role === 'driver';
    const filterField = isDriver ? "driver_email" : "passenger_email";

    const q = query(
      collection(db, "trips_history"),
      where(filterField, "==", email)
    );

    onSnapshot(q, (snapshot) => {
      const trips = [];
      snapshot.forEach((docSnap) => {
        trips.push(docSnap.data());
      });

      if (isDriver) {
        driverTrips.value = trips;
      } else {
        passengerTrips.value = trips;
      }
    });
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
    showAIWarning.value = false;
    let basePrice = selectedZone.value === 'center' ? 120 : 180;

    if (carClass.value === 'econom') basePrice -= 30;
    if (carClass.value === 'lux') basePrice += 100;

    // Коефіцієнт погоди
    let weatherCoeff = isBadWeather.value ? (useSafeRoute.value ? 1.45 : 1.3) : 1.0;

    let bonus = selectedZone.value === 'outskirts' ? 50 : 0;

    const from = pickupLocation.value.trim() || 'Поточне місцезнаходження пасажира';
    const to = destinationLocation.value;

    // Чітка фіксація початкового статусу платіжної системи
    const chosenMethod = paymentMethod.value === 'card' ? 'Картка' : 'Готівка';
    const initialPaymentStatus = chosenMethod === 'Картка' ? 'Оплачено карткою' : 'Очікує оплати готівкою';

    const orderData = {
      order_id: "ORD_" + Date.now(),
      passenger_email: authStore.currentUser.email,
      passenger_name: authStore.currentUser.first_name + " " + authStore.currentUser.last_name,
      pickup_location: from,
      destination: to,
      car_class: carClass.value,
      estimated_cost: Math.round(basePrice * weatherCoeff + bonus),
      motivation_bonus: bonus,
      weather_hazard_level: isBadWeather.value ? 'HIGH' : 'NORMAL',
      current_status: 'waiting',
      zone: selectedZone.value,
      safe_route_applied: useSafeRoute.value,
      payment_method: chosenMethod,
      payment_status: initialPaymentStatus
    };

    await setDoc(doc(db, "demo_orders", "live_order"), orderData);

    cardPaymentSuccess.value = false;
    cardNumber.value = '';
    cardExpiry.value = '';
    cardCvv.value = '';
  };

  // ОНОВЛЕННЯ СТАТУСУ ЗАМОВЛЕННЯ ТА СИНХРОНІЗАЦІЯ З FIREBASE
  const updateStatus = async (newStatus) => {
    if (!currentOrder.value) return;
    const authStore = useAuthStore();
    const uiStore = useUiStore();

    const orderRef = doc(db, "demo_orders", "live_order");
    const updateData = { current_status: newStatus };

    if (newStatus === 'accepted') {
      updateData.driver_email = authStore.currentUser.email;
      updateData.driver_name = authStore.currentUser.first_name + " " + authStore.currentUser.last_name;
      await updateDoc(orderRef, updateData);
    }

    if (newStatus === 'in_progress') {
      await updateDoc(orderRef, updateData);
    }

    if (newStatus === 'completed') {
      const endTimeStr = new Date().toLocaleTimeString();
      const isCash = currentOrder.value.payment_method === 'Готівка';

      const finalTripObj = {
        ...currentOrder.value,
        current_status: 'completed',
        driver_email: authStore.currentUser.email,
        driver_name: authStore.currentUser.first_name + " " + authStore.currentUser.last_name,
        end_time: endTimeStr,
        payment_status: isCash ? 'Оплачено готівкою (водію)' : 'Оплачено карткою'
      };

      const uniqueOrderKey = currentOrder.value.order_id || ("ORD_" + Date.now());
      await setDoc(doc(db, "trips_history", uniqueOrderKey), finalTripObj);

      const passRef = doc(db, "users", currentOrder.value.passenger_email);
      const passSnap = await getDoc(passRef);
      if (passSnap.exists()) {
        await updateDoc(passRef, { total_trips: (passSnap.data().total_trips || 0) + 1 });
      }

      const drvRef = doc(db, "users", authStore.currentUser.email);
      await updateDoc(drvRef, { total_trips: (totalTripsCounter.value + 1) });

      await setDoc(doc(db, "demo_orders", "live_order"), {});

      useSafeRoute.value = false;
      pickupLocation.value = '';
      destinationLocation.value = '';

      uiStore.triggerSuccess("Поїздку успішно завершено! Каунтери оновлено.");
    }
  };

  const resetDemo = async () => {
    await setDoc(doc(db, "demo_orders", "live_order"), {});
    useSafeRoute.value = false;
    pickupLocation.value = '';
    destinationLocation.value = '';
  };

  return {
    currentOrder, isBadWeather, selectedZone,
    showAIWarning, useSafeRoute, passengerTrips, driverTrips, totalTripsCounter,
    pickupLocation, destinationLocation, carClass, paymentMethod, cardNumber, cardExpiry, cardCvv, isCardPaying, cardPaymentSuccess,
    toggleWeather, resetSession, subscribeToUserData, checkOrderConditions, createOrder, updateStatus, resetDemo
  };
});
