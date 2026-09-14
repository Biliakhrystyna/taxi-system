<template>
  <div class="screen-card passenger-box">
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
</template>

<script setup>
import { useOrderStore } from '../../stores/orderStore';
import TaxiMap from '../TaxiMap.vue';

const orderStore = useOrderStore();
</script>
