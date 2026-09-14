
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { db } from '../firebase';
import { doc, setDoc, updateDoc, onSnapshot, getDoc, collection, query, where } from 'firebase/firestore';

export const useOrderStore = defineStore('order', () => {
  // Аутентифікація та ролі
  const userRole = ref(null);       
  const authState = ref('role_selection'); 
  const isSignUp = ref(false);      
  const currentUser = ref(null);    
  
  // Поля форм
  const emailInput = ref('');
  const passwordInput = ref('');
  const firstNameInput = ref('');
  const lastNameInput = ref('');
  const phoneInput = ref('');
  const licenseInput = ref('');
  const driverCarClass = ref('comfort');
  const documentUploaded = ref(false);
  const faceVerified = ref(false);
  const showSuccessAlert = ref(false);

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

  // 1. ЛОГІКА АВТЕНТИФІКАЦІЇ ТА БАЗИ ДАНИХ
  const handleAuthSubmit = async () => {
    passengerTrips.value = []; 
    driverTrips.value = [];    
    totalTripsCounter.value = 0; 
    currentOrder.value = null; 
    showAIWarning.value = false;

    if (!emailInput.value || !passwordInput.value) return;

    const userDocRef = doc(db, "users", emailInput.value);

    if (isSignUp.value) {
      // РЕЄСТРАЦІЯ
      const newUser = {
        email: emailInput.value,
        password_hash: passwordInput.value, 
        first_name: firstNameInput.value,
        last_name: lastNameInput.value,
        phone: phoneInput.value,
        role: userRole.value,
        status: userRole.value === 'driver' ? 'pending_verification' : 'active',
        total_trips: 0
      };

      if (userRole.value === 'driver') {
        newUser.license_number = licenseInput.value;
        newUser.driver_car_class = driverCarClass.value;
      }

      await setDoc(userDocRef, newUser);
      currentUser.value = newUser;

      if (userRole.value === 'driver') {
        authState.value = 'verified_check';
      } else {
        triggerSuccess("Реєстрація пасажира успішна!");
        authState.value = 'main_app';
        setupUserTracking();
      }
    } else {
      // ВХІД
      const userSnap = await getDoc(userDocRef);
      if (userSnap.exists()) {
        const userData = userSnap.data();
        if (userData.password_hash === passwordInput.value && userData.role === userRole.value) {
          currentUser.value = userData;
          triggerSuccess(`Вітаємо, ${userData.first_name}! Вхід успішний.`);
          
          if (userData.status === 'pending_verification') {
            authState.value = 'verified_check';
          } else {
            authState.value = 'main_app';
            setupUserTracking();
          }
        } else {
          alert("Невірний пароль або роль!");
        }
      } else {
        alert("Користувача не знайдено! Пройдіть реєстрацію.");
      }
    }
  };

  // Біометрична верифікація 
  const verifyDriverDocuments = async () => {
    if (!currentUser.value) return;
    const userDocRef = doc(db, "users", currentUser.value.email);
    await updateDoc(userDocRef, {
      status: 'active'
    });

    currentUser.value.status = 'active';
    
    authState.value = 'main_app';
    
    setupUserTracking();

    triggerSuccess("Біометрію пройдено! Обліковий запис водія активовано.");
  };

  
  const triggerSuccess = (msg) => {
    showSuccessAlert.value = msg;
    setTimeout(() => { showSuccessAlert.value = false; }, 3500);
  };

  // Налаштування real-time відстеження каунтерів, історії та поточного замовлення
  const setupUserTracking = () => {
    if (!currentUser.value || !currentUser.value.email) return;


    onSnapshot(doc(db, "users", currentUser.value.email), (snap) => {
      if (snap.exists()) {
        totalTripsCounter.value = snap.data().total_trips || 0;
      }
    });

    
    onSnapshot(doc(db, "demo_orders", "live_order"), (snapshot) => {
      if (snapshot.exists()) {
        currentOrder.value = snapshot.data();
      } else {
        currentOrder.value = null;
      }
    });

    const isDriver = currentUser.value.role === 'driver';
    const filterField = isDriver ? "driver_email" : "passenger_email";

    const q = query(
      collection(db, "trips_history"),
      where(filterField, "==", currentUser.value.email)
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
  
  // 2. ЛОГІКА ШІ ТА РОЗРАХУНКУ ТАРИФУ
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
      passenger_email: currentUser.value.email,
      passenger_name: currentUser.value.first_name + " " + currentUser.value.last_name,
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

  // 3. ОНОВЛЕННЯ СТАТУСУ ЗАМОВЛЕННЯ ТА СИНХРОНІЗАЦІЯ З FIREBASE
  const updateStatus = async (newStatus) => {
    if (!currentOrder.value) return;
    
    const orderRef = doc(db, "demo_orders", "live_order");
    const updateData = { current_status: newStatus };
    
    if (newStatus === 'accepted') {
      updateData.driver_email = currentUser.value.email;
      updateData.driver_name = currentUser.value.first_name + " " + currentUser.value.last_name;
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
        driver_email: currentUser.value.email,
        driver_name: currentUser.value.first_name + " " + currentUser.value.last_name,
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

      const drvRef = doc(db, "users", currentUser.value.email);
      await updateDoc(drvRef, { total_trips: (totalTripsCounter.value + 1) });

      
      await setDoc(doc(db, "demo_orders", "live_order"), {});
      
      useSafeRoute.value = false;
      pickupLocation.value = '';
      destinationLocation.value = '';
      
      triggerSuccess("Поїздку успішно завершено! Каунтери оновлено.");
    }
  };

  
  const resetDemo = async () => {
    await setDoc(doc(db, "demo_orders", "live_order"), {});
    useSafeRoute.value = false;
    pickupLocation.value = '';
    destinationLocation.value = '';
  };
       
  // ВИХІД З АКАУНТУ
  const logout = () => {
    currentUser.value = null;
    userRole.value = null;
    authState.value = 'role_selection';
    
    passengerTrips.value = [];
    driverTrips.value = [];
    totalTripsCounter.value = 0;
    currentOrder.value = null;
    
    // Очищення інпутів форми
    emailInput.value = '';
    passwordInput.value = '';
    firstNameInput.value = '';
    lastNameInput.value = '';
    phoneInput.value = '';
    pickupLocation.value = '';
    destinationLocation.value = '';
  };

  
  return {
    userRole, authState, isSignUp, currentUser,
    emailInput, passwordInput, firstNameInput, lastNameInput, phoneInput, licenseInput, driverCarClass,
    documentUploaded, showSuccessAlert, currentOrder, isBadWeather, selectedZone,
    showAIWarning, useSafeRoute, passengerTrips, driverTrips, totalTripsCounter,
    pickupLocation, destinationLocation, carClass, paymentMethod, cardNumber, cardExpiry, isCardPaying, cardPaymentSuccess,
    handleAuthSubmit, checkOrderConditions, createOrder, updateStatus, resetDemo, logout, toggleWeather, cardCvv, faceVerified, verifyDriverDocuments
  }
});