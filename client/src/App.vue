<template>
  <div class="app-container">
    <div v-if="orderStore.showSuccessAlert" class="global-alert">
      🎉 {{ orderStore.showSuccessAlert }}
    </div>

    <header class="app-header">
      <h1>🚖 Система таксі перевезень </h1>
      <p>Розробник: Біляк Христина (Група ОІ-32)</p>
      <button v-if="orderStore.authState === 'main_app'" class="btn logout-btn" @click="orderStore.logout">🚪 Вийти з акаунту</button>
    </header>

    <div v-if="orderStore.authState === 'role_selection'" class="auth-card text-center">
      <h2>Вітаємо у системі таксі! Хто ви?</h2>
      <p class="subtitle">Будь ласка, оберіть вашу роль для входу в систему:</p>
      <div class="btn-group-row">
        <button class="btn passenger-btn-big" @click="orderStore.userRole = 'passenger'; orderStore.authState = 'auth_form'">
          🙋‍♀️ Я Пасажир
        </button>
        <button class="btn driver-btn-big" @click="orderStore.userRole = 'driver'; orderStore.authState = 'auth_form'">
          🚖 Я Водій
        </button>
      </div>
    </div>

    <div v-if="orderStore.authState === 'auth_form'" class="auth-card">
      <button class="back-link" @click="orderStore.authState = 'role_selection'">← Назад до вибору ролі</button>
      <h2>
        {{ orderStore.userRole === 'passenger' ? '🙋‍♀️ Кабінет Пасажира' : '🚖 Кабінет Водія' }} : 
        {{ orderStore.isSignUp ? 'Реєстрація' : 'Авторизація' }}
      </h2>

     <form @submit.prevent="orderStore.handleAuthSubmit" class="auth-form">
        <div v-if="orderStore.isSignUp" class="form-grid">
          
          <div class="form-group">
            <label>Ім'я:</label>
            <input type="text" v-model="orderStore.firstNameInput" class="form-input" required />
          </div>
          
          <div class="form-group mt-2">
            <label>Прізвище:</label>
            <input type="text" v-model="orderStore.lastNameInput" class="form-input" required />
          </div>
          
          <div class="form-group mt-2">
            <label>Номер телефону:</label>
            <input type="text" v-model="orderStore.phoneInput" placeholder="+380..." class="form-input" required />
          </div>

          <div v-if="orderStore.userRole === 'driver'" class="driver-fields mt-3" style="width: 100%;">
            <div class="form-group">
              <label>🪪 Номер водійського посвідчення:</label>
              <input type="text" v-model="orderStore.licenseInput" placeholder="BXX XXXXXX" class="form-input" required />
            </div>

            <div class="form-group mt-2">
              <label>🚗 Робочий клас автомобіля:</label>
              <select v-model="orderStore.driverCarClass" class="form-select">
                <option value="econom">🍃 Економ (Доступний сегмент)</option>
                <option value="comfort">⭐ Комфорт (Оптимальний сегмент)</option>
                <option value="lux">💎 Люкс / Бізнес (Преміум сегмент)</option>
              </select>
            </div>
          </div>

        </div> <div class="form-group mt-3">
          <label>Електронна пошта (Email):</label>
          <input type="email" v-model="orderStore.emailInput" placeholder="name@example.com" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Пароль:</label>
          <input type="password" v-model="orderStore.passwordInput" placeholder="••••••••" class="form-input" required />
        </div>

        <div class="mt-4">
          <button type="submit" class="btn success-btn w-full">
            {{ orderStore.isSignUp ? '✨ Зареєструватися в системі' : '🔑 Увійти в кабінет' }}
          </button>
        </div>

        <p class="toggle-auth-text">
        {{ orderStore.isSignUp ? 'Вже є акаунт?' : 'Вперше в системі?' }}
        <span @click="orderStore.isSignUp = !orderStore.isSignUp">
          {{ orderStore.isSignUp ? 'Увійти' : 'Зареєструватися' }}
        </span>
      </p>
      </form>

      
    </div>

   <div v-if="orderStore.authState === 'verified_check'" class="auth-card text-center">
      <h2 style="color: #f59e0b;">🪪 Контроль безпеки</h2>
      <p class="subtitle">Для активації облікового запису водія в базі даних виконайте завантаження та сканування особи:</p>
      
      <div 
        class="upload-zone" 
        :class="{ 'success-border': orderStore.documentUploaded }"
        style="border: 2px dashed #475569; padding: 25px; border-radius: 8px; margin: 20px 0; background: #0f172a; cursor: pointer; transition: 0.2s;"
        @dragover.prevent
        @drop.prevent="orderStore.documentUploaded = true"
        @click="orderStore.documentUploaded = true"
      >
        <p v-if="!orderStore.documentUploaded">📁 Перетягніть скан-копію посвідчення або натисніть для вибору файлу</p>
        <p v-else style="color: #10b981; font-weight: bold;">✅ Документ "license_scan.pdf" успішно розпізнано!</p>
      </div>

      <div style="margin-bottom: 25px;">
        <button 
          type="button"
          class="btn" 
          :style="orderStore.faceVerified ? 'background: rgba(16, 185, 129, 0.2); border-color: #10b981; color: #34d399;' : 'background: rgba(56, 189, 248, 0.1); border-color: #38bdf8; color: #38bdf8;'"
          @click="orderStore.faceVerified = true"
          style="width: 100%; padding: 12px; font-weight: bold; border: 1px solid;"
        >
          <span v-if="orderStore.faceVerified">📸 Особу підтверджено нейромережею Face-API.js</span>
          <span v-else>📷 Запустити Face-API біометричний контроль</span>
        </button>
      </div>

      <button 
        class="btn success-btn w-full" 
        style="padding: 14px; font-size: 15px;"
        :disabled="!orderStore.documentUploaded || !orderStore.faceVerified"
        @click="orderStore.verifyDriverDocuments"
      >
        Завершити реєстрацію та увійти в систему
      </button>
      
      <p v-if="!orderStore.documentUploaded || !orderStore.faceVerified" style="color: #ef4444; font-size: 11px; margin-top: 8px;">
        * Кнопка активації стане доступною після завантаження скану та сканування обличчя.
      </p>
    </div>

    <div v-if="orderStore.authState === 'main_app'">
      
      <section class="env-controls">
        <h3>🌤️ Панель симуляції метеоумов :</h3>
        <div class="flex-row">
          <label class="toggle-container">
            <input type="checkbox" v-model="orderStore.isBadWeather" />
            <span class="checkmark"></span>
            <span class="label-text">Активувати складні погодні умови (Ожеледиця / Сильна злива)</span>
          </label>
          <button class="btn reset-btn" @click="orderStore.resetDemo">Очистити поточне замовлення</button>
        </div>
      </section>

      <div class="user-profile-banner">
        <span>👤 Користувач: <strong>{{ orderStore.currentUser.first_name }} {{ orderStore.currentUser.last_name }}</strong></span>
        <span class="counter-badge">📊 Ваш особистий лічильник поїздок в базі: <strong>{{ orderStore.totalTripsCounter }}</strong></span>
      </div>

      <div class="main-grid">
      <div v-if="orderStore.currentUser && orderStore.currentUser.role === 'passenger'" class="screen-card passenger-box">
      <h2>📱 Панель пасажира (Створення замовлення)</h2>
      <div class="weather-simulator" style="background: rgba(30, 41, 59, 0.4); border: 1px solid #334155; padding: 10px; border-radius: 8px; margin-bottom: 15px; display: flex; align-items: center; justify-content: space-between;">
            <span style="font-size: 13px; color: #94a3b8;">Поточна погода в місті:</span>
            <button type="button" class="btn" :style="orderStore.isBadWeather ? 'background: rgba(239, 68, 68, 0.2); border-color: #ef4444; color: #f87171;' : 'background: rgba(234, 179, 8, 0.1); border-color: #eab308; color: #fde047;'" @click="orderStore.toggleWeather()">
              <span v-if="orderStore.isBadWeather">🌧️ Сильна злива (ШІ-захист ON)</span>
              <span v-else>☀️ Сонячно (Звичайний режим)</span>
            </button>
          </div>
      
      <div v-if="!orderStore.currentOrder || !orderStore.currentOrder.order_id" class="booking-form">
        
        <div class="form-group">
          <label>📍 Звідки (Точка А):</label>
          <input type="text" v-model="orderStore.pickupLocation" placeholder="Встановіть перший клік на мапі" class="form-input" readonly />
        </div>

        <div class="form-group mt-3">
          <label>🏁 Куди (Точка B):</label>
          <input type="text" v-model="orderStore.destinationLocation" placeholder="Встановіть другий клік на мапі" class="form-input" readonly />
        </div>

        <div class="form-group mt-3">
          <label>🚗 Клас автомобіля:</label>
          <select v-model="orderStore.carClass" class="form-select">
            <option value="econom">🍃 Економ</option>
            <option value="comfort">⭐ Комфорт</option>
            <option value="lux">💎 Люкс</option>
          </select>
        </div>

        <div class="form-group mt-3">
          <label>💳 Спосіб оплати:</label>
          <div class="payment-methods" style="display: flex; gap: 10px; margin-top: 5px;">
            <button type="button" class="btn" style="flex: 1; background: #0f172a; border: 1px solid #475569;" :style="orderStore.paymentMethod === 'cash' ? 'border-color: #38bdf8; background: rgba(56, 189, 248, 0.1);' : ''" @click="orderStore.paymentMethod = 'cash'">
              💵 Готівка
            </button>
            <button type="button" class="btn" style="flex: 1; background: #0f172a; border: 1px solid #475569;" :style="orderStore.paymentMethod === 'card' ? 'border-color: #38bdf8; background: rgba(56, 189, 248, 0.1);' : ''" @click="orderStore.paymentMethod = 'card'">
              💳 Картка
            </button>
          </div>
        </div>

        <div v-if="orderStore.paymentMethod === 'card'" class="form-group mt-3" style="background: rgba(30, 41, 59, 0.6); padding: 15px; border-radius: 8px; border: 1px solid #475569;">
          <div class="form-group">
            <label>Номер картки:</label>
            <input type="text" v-model="orderStore.cardNumber" placeholder="4441 4000 0000 0000" maxlength="19" class="form-input" style="letter-spacing: 2px; text-align: center;" />
          </div>
          
          <div style="display: flex; gap: 10px; margin-top: 12px;">
            <div class="form-group" style="flex: 1;">
              <label>Термін дії:</label>
              <input type="text" v-model="orderStore.cardExpiry" placeholder="MM/YY" maxlength="5" class="form-input" style="text-align: center;" />
            </div>
            <div class="form-group" style="flex: 1;">
              <label>CVC/CVV:</label>
              <input type="password" v-model="orderStore.cardCvv" placeholder="•••" maxlength="3" class="form-input" style="text-align: center; letter-spacing: 3px;" />
            </div>
          </div>

          <div v-if="orderStore.cardNumber && orderStore.cardNumber.length >= 16 && orderStore.cardExpiry.length >= 5" class="mt-3 text-center" style="font-size: 11px; color: #10b981; font-weight: bold;">
            ✅ Картку та платіжні дані успішно верифіковано 
          </div>
        </div>

        <div class="form-group mt-4 text-center" v-if="orderStore.pickupLocation && orderStore.destinationLocation">
          <div class="tag" :class="orderStore.selectedZone === 'outskirts' ? 'bonus-tag' : 'safe-tag'" style="display: inline-block; padding: 6px 12px; font-weight: bold;">
            <span v-if="orderStore.selectedZone === 'outskirts'">🔥Виявлено зону ДЕФІЦИТУ авто (+50 грн водію)</span>
            <span v-else>✨  Стандартна зона (Баланс попиту)</span>
          </div>
        </div>

        <div class="mt-4">
          <button class="btn success-btn w-full" :disabled="!orderStore.pickupLocation || !orderStore.destinationLocation" @click="orderStore.isBadWeather ? (orderStore.showAIWarning = true) : orderStore.createOrder()">
            Сформувати замовлення 
          </button>
          <p v-if="!orderStore.pickupLocation || !orderStore.destinationLocation" class="text-center mt-2" style="font-size: 11px; color: #94a3b8;">
            * Будь ласка, оберіть Точки А та В на мапі нижче, щоб розрахувати маршрут.
          </p>
        </div>
      </div>

      <div v-if="orderStore.currentOrder && orderStore.currentOrder.order_id" class="active-order-box">
        <h3>Статус замовлення: <span class="badge" :class="orderStore.currentOrder.current_status">{{ orderStore.currentOrder.current_status }}</span></h3>
        <p><strong>Звідки (А):</strong> {{ orderStore.currentOrder.pickup_location }}</p>
        <p><strong>Куди (B):</strong> {{ orderStore.currentOrder.destination }}</p>
        <p><strong>Вартість поїздки:</strong> <span class="price-text">{{ orderStore.currentOrder.estimated_cost }} грн</span></p>
        <p><strong>Тип оплати:</strong> {{ orderStore.currentOrder.payment_method || 'Готівка' }}</p>
            <p><strong>Статус транзакції:</strong> 
              <span :style="orderStore.currentOrder.payment_status && orderStore.currentOrder.payment_status.includes('Оплачено') ? 'color: #10b981; font-weight: bold;' : 'color: #ffffff; font-weight: bold;'">
                {{ orderStore.currentOrder.payment_status || 'Очікує оплати' }}
          </span>
        </p>
        
        <div class="tags-container">
          <span v-if="orderStore.currentOrder.safe_route_applied" class="tag safe-tag"> Без配чний ШІ-маршрут активовано</span>
        </div>
        <p v-if="orderStore.currentOrder.driver_name" style="color: #000000; margin-top: 10px;">👨‍✈️ Призначений водій: {{ orderStore.currentOrder.driver_name }}</p>
      </div>

      <TaxiMap role="passenger" :isBadWeather="orderStore.isBadWeather" />
    </div>
      
        <div v-if="orderStore.currentUser.role === 'driver'" class="screen-card driver-box">
          <h2>🚕 Панель водія </h2>
          <div class="driver-badge-info" style="margin-bottom: 15px; font-size: 15px; color: #ffffff;">
            👨‍✈️ Автомобіль авторизовано як: 
            <span style="text-transform: uppercase; color: #000000; font-weight: bold;">
              {{ orderStore.currentUser.driver_car_class || 'comfort' }}
            </span>
          </div>
          <div v-if="!orderStore.currentOrder || !orderStore.currentOrder.order_id" class="no-orders">
            <p>Немає активних замовлень у місті. Очікування клієнтів...</p>
          </div>

          <div v-else class="active-order-box">
            <div v-if="orderStore.currentOrder.zone === 'outskirts' || orderStore.currentOrder.motivation_bonus" class="bonus-alert">
           🔥 Увага! Виявлено зону ДЕФІЦИТУ авто! Вам нараховано мотиваційну надбавку +{{ orderStore.currentOrder.motivation_bonus || 50 }} грн до тарифу!
          </div>
            <p><strong>Клієнт:</strong> {{ orderStore.currentOrder.passenger_name }}</p>
            <p><strong>Маршрут:</strong> {{ orderStore.currentOrder.pickup_location }} → {{ orderStore.currentOrder.destination }}</p>
            <p><strong>Ваш чистий дохід:</strong> <span class="price-text">{{ orderStore.currentOrder.estimated_cost }} грн</span></p>
            <p><strong>Погодний hazard рівень:</strong> {{ orderStore.currentOrder.weather_hazard_level }}</p>

            <div style="background: rgba(15, 23, 42, 0.4); padding: 12px; border-radius: 6px; margin-top: 12px; border: 1px dashed #475569; text-align: left;">
              <p style="margin: 0;">💵 <b>Метод оплати:</b> {{ orderStore.currentOrder.payment_method || 'Готівка' }}</p>
              <p style="margin: 6px 0 0 0;">📊 <b>Фінансовий статус:</b> 
                <span :style="orderStore.currentOrder.payment_status && orderStore.currentOrder.payment_status.includes('Оплачено') ? 'color: #10b981; font-weight: bold;' : 'color: #fbbf24; font-weight: bold;'">
                  {{ orderStore.currentOrder.payment_status || 'Очікує завершення поїздки' }}
                </span>
              </p>
            </div>
            <div class="driver-buttons mt-4" style="display: flex; flex-direction: column; gap: 10px;">
              <button v-if="orderStore.currentOrder.current_status === 'waiting'" class="btn success-btn w-full" @click="orderStore.updateStatus('accepted')">
                🔀 Прийняти замовлення 
              </button>
              <button v-if="orderStore.currentOrder.current_status === 'accepted'" class="btn primary-btn w-full" @click="orderStore.updateStatus('in_progress')">
                🚗 Пасажир сів в авто (Почати рух)
              </button>
              
              <button v-if="orderStore.currentOrder.current_status === 'in_progress'" class="btn danger-btn w-full" @click="orderStore.updateStatus('completed')">
                💵 Завершити рейс та оновити базу
              </button>
            </div>
          </div>

          <TaxiMap role="driver" :isBadWeather="orderStore.isBadWeather" />
        </div>
        
        <div class="screen-card history-box">
          <h2>📜 Історія виконаних поїздок з Firebase</h2>
          
          <div v-if="orderStore.currentUser.role === 'passenger' && orderStore.passengerTrips.length === 0" class="no-data">Історія поїздок порожня.</div>
          <div v-if="orderStore.currentUser.role === 'driver' && orderStore.driverTrips.length === 0" class="no-data">Ви ще не виконали жодного рейсу.</div>

          <table class="history-table" v-if="orderStore.currentUser.role === 'passenger' ? orderStore.passengerTrips.length > 0 : orderStore.driverTrips.length > 0">
            <thead>
              <tr>
                <th>ID</th>
                <th>Маршрут призначення</th>
                <th>Вартість</th>
                <th>ШІ Безпека</th>
                <th>Статус</th>
              </tr>
            </thead>
           <tbody>
              <tr v-for="trip in (orderStore.currentUser.role === 'passenger' ? orderStore.passengerTrips : orderStore.driverTrips)" :key="trip.order_id">
                <td>{{ trip.order_id.substring(4, 10) }}...</td>
                <td>{{ trip.destination }}</td>
                
                <td style="color: #10b981; font-weight: bold;">
                  {{ trip.estimated_cost }} грн
                  <div v-if="trip.motivation_bonus" style="font-size: 10px; color: #b45309; font-weight: 800; text-transform: uppercase; margin-top: 2px;">
                    💰 (+{{ trip.motivation_bonus }} грн Бонус)
                  </div>
                </td>
                
                <td>{{ trip.safe_route_applied ? 'Захист' : 'Стандарт' }}</td>
                <td><span class="badge completed">Завершено</span></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
    </div>
    <div v-if="orderStore.showAIWarning" class="modal-backdrop" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0, 0, 0, 0.75); display: flex; align-items: center; justify-content: center; z-index: 9999; padding: 20px;">
      <div class="modal-card" style="background: #0f172a; border: 2px solid #ef4444; padding: 25px; border-radius: 12px; max-width: 500px; width: 100%; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5);">
        
        <div style="text-align: center; margin-bottom: 15px;">
          <span style="font-size: 40px;">🌧️</span>
          <h3 style="color: #ef4444; margin: 10px 0 0 0; font-size: 20px; font-weight: bold; text-transform: uppercase;">⚠️ Попередження Метеомоніторингу</h3>
        </div>

        <p style="color: #e2e8f0; font-size: 14px; line-height: 1.6; text-align: center;">
          Система виявила <b style="color: #f87171;">сильну зливу та підвищений ризик небезпеки</b> на стандартному шляху. Наш ШІ-алгоритм розрахував альтернативний безпечний маршрут в обхід критичних ділянок дороги.
        </p>

        <div style="background: rgba(30, 41, 59, 0.6); padding: 12px; border-radius: 8px; margin: 15px 0; border: 1px solid #334155;">
          <div style="display: flex; justify-content: space-between; font-size: 13px; margin-bottom: 5px;">
            <span style="color: #38bdf8;">🔵 Стандартний шлях:</span>
            <span style="font-weight: bold;">Найкоротший (Але є ризик злетіти з дороги)</span>
          </div>
          <div style="display: flex; justify-content: space-between; font-size: 13px; color: #f97316;">
            <span> Безпечний ШІ-маршрут:</span>
            <span style="font-weight: bold;">+800 м в обхід </span>
          </div>
        </div>

        <div style="display: flex; flex-direction: column; gap: 10px; margin-top: 20px;">
          <button class="btn" style="background: #f97316; color: white; font-weight: bold; padding: 12px;" @click="orderStore.useSafeRoute = true; orderStore.showAIWarning = false; orderStore.createOrder();">
            Активувати безпечний ШІ-маршрут (Рекомендовано)
          </button>
          
          <button class="btn" style="background: rgba(71, 85, 105, 0.3); color: #cbd5e1; border: 1px solid #475569; padding: 10px;" @click="orderStore.useSafeRoute = false; orderStore.showAIWarning = false; orderStore.createOrder();">
            🚫 Ігнорувати, їхати звичайним шляхом
          </button>
        </div>

      </div>
    </div>
</template>

<script setup>
import { onMounted } from 'vue';
import { useOrderStore } from './stores/orderStore';
import TaxiMap from './components/TaxiMap.vue';

const orderStore = useOrderStore();

onMounted(() => {
  // Якщо є збережені стани, можемо ініціалізувати
});
</script>

<style>
body {
  font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
  
  /* Фото тла з яскравим проявленням текстури дороги */
  background-image: linear-gradient(rgba(11, 15, 25, 0.40), rgba(11, 15, 25, 0.55)), url('./assets/taxi.jpg.jpg');
  
  background-size: cover;          /* Розтягує картинку на весь екран */
  background-position: center;     /* Центрує текстуру розмітки */
  background-attachment: fixed;    /* Залишає тло нерухомим при прокручуванні */
  background-repeat: no-repeat;
  
  color: #f8fafc; /* Білий колір для загального тексту поза картками */
  margin: 0; 
  padding: 20px;
}

.app-container { 
  max-width: 1200px; 
  margin: 0 auto; 
  position: relative; 
}

/* 🏢 ШАПКА ПЛАТФОРМИ (ФЛЕКС-АДАПТИВНІСТЬ БЕЗ НАЛАЗАННЯ КНОПКИ) */
.app-header { 
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center; 
  border-bottom: 3px solid #eab308; /* Фірмовий жовтий бордюр */
  padding-bottom: 20px; 
  margin-bottom: 25px; 
  position: relative; 
  gap: 10px;
}
.app-header h1 { 
  margin: 0; 
  color: #eab308; /* Жовтий титул */
  font-size: 30px; 
  font-weight: 900; /* Максимально жирний шрифт */
  text-transform: uppercase; /* Тільки великі літери */
  letter-spacing: 2px; /* Сучасний розріджений інтервал */
  text-shadow: 0 0 15px rgba(234, 179, 8, 0.3);
}
.app-header p {
  color: #ffffff; 
  font-size: 16px;
  font-weight: 500;
  margin: 5px 0 0 0;
}
.logout-btn { 
  background: #000000; 
  color: #eab308; 
  border: 2px solid #eab308; /* Жовтий обідок кнопки під колір теми */
  font-weight: bold;
  font-size: 12px; 
  padding: 8px 16px; 
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-top: 5px; 
}
.logout-btn:hover {
  background: #eab308;
  color: #000000;
  border-color: #000000;
}

@media (min-width: 768px) {
  .app-header {
    flex-direction: row;
    justify-content: space-between;
    text-align: left;
    align-items: center;
  }
  .logout-btn {
    margin-top: 0;
  }
}

/* 🖨️ НОВИЙ СТИЛЬ КАРТОК: ЖОВТИЙ ФОН, ЧОРНИЙ ОБІДОК, ЧОРНИЙ ШРИФТ */
.auth-card, .screen-card { 
  background: #eab308; /* Фірмовий насичений жовтий */
  color: #000000;      /* Текст всередині карток тепер СУТO ЧОРНИЙ */
  padding: 25px; 
  border-radius: 12px; 
  border: 3px solid #000000; /* Жирний чорний обідок */
  box-shadow: 0 15px 25px rgba(0, 0, 0, 0.6);
}
.auth-card h2, .screen-card h2 { 
  margin-top: 0; 
  font-size: 22px; 
  color: #000000 !important; /* Назви вікон тепер теж строго чорні */
  border-bottom: 2px solid #000000; /* Чорна лінія розподілу */
  padding-bottom: 12px;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

/* Корекція кольору заголовків у різних боксах */
.passenger-box h2, .driver-box h2, .auth-card h2 { color: #000000 !important; }

.subtitle { color: #000000; font-size: 14px; margin-bottom: 25px; font-weight: 600; }
.back-link { background: none; border: none; color: #000000; font-weight: bold; cursor: pointer; font-size: 13px; margin-bottom: 15px; display: block; }
.back-link:hover { text-decoration: underline; }

/* 🌤️ БЛОК СИМУЛЯЦІЇ ПОГОДИ */
.weather-simulator span {
  color: #ffffff !important; 
  font-weight: 700;
  text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8); 
}

/* 🕹️ КНОПКИ ВИБОРУ РОЛЕЙ */
.btn-group-row { display: flex; gap: 20px; justify-content: center; }
.passenger-btn-big { 
  background: #ffffff; 
  color: #000000; 
  padding: 20px; 
  font-size: 18px; 
  border-radius: 8px; 
  width: 180px; 
  font-weight: bold;
  border: 2px solid #000000;
  transition: 0.2s;
}
.passenger-btn-big:hover { background: #e2e8f0; transform: translateY(-2px); }

.driver-btn-big { 
  background: #000000; 
  color: #eab308; 
  padding: 20px; 
  font-size: 18px; 
  border-radius: 8px; 
  width: 180px; 
  font-weight: bold;
  border: 2px solid #000000;
  transition: 0.2s;
}
.driver-btn-big:hover { background: #1e293b; transform: translateY(-2px); }

/* 📋 ФОРМИ, ТЕКСТИ ТА ІНПУТИ ВСЕРЕДИНІ ЖОВТИХ КАРТОК */
.auth-form, .booking-form { display: flex; flex-direction: column; gap: 15px; text-align: left; background: none; border: none; padding: 0; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group label { font-size: 12px; color: #000000; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; }

/* Контрастні інпути для зчитування даних */
.form-input, .form-select { 
  background: #ffffff; 
  border: 2px solid #000000; 
  border-radius: 6px; 
  color: #000000; 
  padding: 10px 12px; 
  font-size: 14px; 
  font-weight: 600;
  outline: none; 
  transition: all 0.2s; 
}
.form-input:focus, .form-select:focus { 
  background: #fffdf2;
  box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.15);
}
.form-input:disabled { background: #e2e8f0; color: #ebedef; cursor: not-allowed; border-color: #94a3b8; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }

/* 💳 КНОПКИ СПОСОБУ ОПЛАТИ */
.payment-methods .btn {
  background: #eab308 !important; 
  color: #000000 !important;      
  border: 3px solid #000000 !important; 
  font-weight: 900 !important;
  text-transform: uppercase;
  transition: all 0.2s;
}
.payment-methods .btn[style*="border-color: rgb(56, 189, 248)"],
.payment-methods .btn[style*="border-color: #38bdf8"] {
  background: #000000 !important;  
  color: #eab308 !important;      
  border: 3px solid #000000 !important;
}

/* ⚡ СИСТЕМНІ КНОПКИ */
.btn { padding: 11px 18px; border: none; border-radius: 6px; font-weight: 800; cursor: pointer; transition: all 0.2s; display: inline-flex; align-items: center; justify-content: center; text-transform: uppercase; font-size: 13px; }

/* Виправлено: Кнопка відправки форми, коли вона активна/неактивна */
.success-btn { background: #000000; color: #eab308; border: 3px solid #000000; }
.success-btn:hover:not(:disabled) { background: #1e293b; color: #ffffff; }

/* Виправлено: Стан, коли кнопка верифікації заблокована (Не біла, а строга темно-сіра з контуром) */
.success-btn:disabled { 
  background: #374151 !important; 
  color: #9ca3af !important; 
  border-color: #000000 !important; 
  cursor: not-allowed; 
}

.primary-btn { background: #ffffff; color: #000000; border: 2px solid #000000; }
.primary-btn:hover { background: #e2e8f0; }

.danger-btn { background: #dc2626; color: #ffffff; border: 2px solid #000000; }
.danger-btn:hover { background: #b91c1c; }

.reset-btn { background: #000000; color: #ffffff; font-size: 12px; border: 2px solid #000000; }
.reset-btn:hover { background: #dc2626; color: #ffffff; }

.toggle-auth-text { font-size: 13px; color: #000000; font-weight: 700; margin-top: 15px; text-align: center; }
.toggle-auth-text span { color: #000000; cursor: pointer; text-decoration: underline; }
.auth-form a { color: #000000 !important; text-decoration: underline; }

/* 🛡️ КЕРУВАННЯ МЕТЕОУМОВАМИ */
.env-controls { background: #111827; padding: 15px; border-radius: 8px; margin-bottom: 20px; border: 2px solid #eab308; }
.env-controls h3 { margin: 0 0 10px 0; font-size: 14px; color: #eab308; text-transform: uppercase; font-weight: bold; }
.flex-row { display: flex; justify-content: space-between; align-items: center; gap: 15px; }
.label-text { font-weight: 600; color: #ffffff; }

.user-profile-banner { background: #eab308; color: #000000; padding: 12px 20px; border-radius: 8px; margin-bottom: 20px; border: 3px solid #000000; display: flex; justify-content: space-between; align-items: center; font-weight: 700; }
.counter-badge { background: #000000; padding: 6px 12px; border-radius: 4px; border: 1px solid #000000; color: #eab308; font-weight: bold; }

/* 📦 АКТИВНІ ЗАМОВЛЕННЯ ВСЕРЕДИНІ ЖОВТОЇ КАРТКИ */
.active-order-box { background: #eab308; padding: 18px; border-radius: 8px; margin-top: 15px; border: 2px solid #000000; color: #000000; }
.active-order-box p, .active-order-box strong { color: #000000; }
.price-text { font-size: 22px; color: #b45309; font-weight: 900; }

/* 🏷️ ШІ ГЕОФЕНСИНГ БЕЙДЖІ */
.tag { 
  padding: 8px 14px; 
  border-radius: 6px; 
  font-size: 13px; 
  font-weight: 900; 
  text-align: center; 
  text-transform: uppercase; 
  width: 100%;
  display: block;
  box-sizing: border-box;
}
.safe-tag { 
  background: #eab308 !important; 
  color: #000000 !important; 
  border: 3px solid #000000 !important; 
}
.bonus-tag { 
  background: #000000 !important; 
  color: #eab308 !important; 
  border: 3px solid #eab308 !important; 
}
.bonus-alert { background: #000000; color: #fde047; padding: 12px; border-radius: 6px; border: 2px solid #000000; font-weight: bold; margin-bottom: 15px; font-size: 13px; text-align: left; }

/* 📸 БІОМЕТРІЯ ТА DRAG & DROP СКАНИ (ФІКС: КРЕМОВО-ЖОВТИЙ КОНТРАСТНИЙ БОКС) */
.upload-zone, .drop-zone { 
  border: 3px dashed #000000 !important; 
  padding: 25px; 
  border-radius: 8px; 
  margin: 15px 0; 
  background: #fffbeb !important; /* Ніжно-кремове світле тло для виділення тексту */
  text-align: center; 
  color: #000000 !important; 
  font-weight: 800 !important; 
  font-size: 14px;
}
.success-border, .drop-zone.active { 
  background: #fef08a !important; /* Яскравіший жовтий при успішному завантаженні */
}

/* Виправлено: Кнопка Face-API тепер має чіткий шрифт і чорний текст на зеленому підтвердженому стані */
.auth-card button[style*="color: rgb(52, 211, 153)"],
.auth-card button[style*="color: #34d399"] {
  background: #10b981 !important; /* Насичений зелений ШІ-колір */
  color: #000000 !important;      /* Чорний контрастний шрифт літер */
  border: 3px solid #000000 !important;
  font-weight: 900 !important;
  font-size: 13px !important;
  letter-spacing: 0.5px;
}
/* Стартовий стан кнопки Face-API (чорний з жовтим) */
.auth-card button[style*="color: rgb(56, 189, 248)"],
.auth-card button[style*="color: #38bdf8"] {
  background: #000000 !important;
  color: #eab308 !important;
  border: 3px solid #000000 !important;
  font-weight: 900 !important;
}

/* 📊 ТАБЛИЦІ ІСТОРІЇ ПОЇЗДОК */
.history-box { margin-top: 25px; }
.history-table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; }
.history-table th { background: #000000; padding: 12px; text-align: left; color: #eab308; border-bottom: 2px solid #000000; font-weight: bold; }
.history-table td { padding: 12px; border-bottom: 1px solid #000000; color: #000000; font-weight: 600; }
.badge { padding: 4px 8px; border-radius: 4px; font-size: 11px; font-weight: bold; text-transform: uppercase; }
.badge.completed { background: #000000; color: #ffffff; border: 1px solid #000000; }
.badge.waiting { background: #ffffff; color: #000000; border: 2px solid #000000; }
.no-data { color: #4b5563; padding: 20px; text-align: center; font-size: 13px; font-weight: bold; }

/* 🔔 ГЛОБАЛЬНІ СПОВІЩЕННЯ */
.global-alert { position: fixed; top: 20px; left: 50%; transform: translateX(-50%); background: #000000; color: #eab308; padding: 12px 30px; border-radius: 30px; font-weight: bold; border: 2px solid #eab308; box-shadow: 0 10px 20px rgba(0,0,0,0.5); z-index: 9999; }

/* 🗂️ СІТКА GRID */
.main-grid { display: grid; grid-template-columns: 1fr; gap: 25px; }
.w-full { width: 100%; }
.mt-2 { margin-top: 8px; }
.mt-3 { margin-top: 12px; }
.mt-4 { margin-top: 16px; }
.text-center { text-align: center; }
</style>