<template>
  <div class="screen-card driver-box">
    <h2>🚕 Панель водія </h2>
    <div class="driver-badge-info" style="margin-bottom: 15px; font-size: 15px; color: #ffffff;">
      👨‍✈️ Автомобіль авторизовано як:
      <span style="text-transform: uppercase; color: #000000; font-weight: bold;">
        {{ authStore.currentUser.driver_car_class || 'comfort' }}
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
</template>

<script setup>
import { useAuthStore } from '../../stores/authStore';
import { useOrderStore } from '../../stores/orderStore';
import TaxiMap from '../TaxiMap.vue';

const authStore = useAuthStore();
const orderStore = useOrderStore();
</script>
