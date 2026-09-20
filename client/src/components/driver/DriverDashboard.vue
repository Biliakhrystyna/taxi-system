<template>
  <div class="screen-card driver-box">
    <h2>🚕 Панель водія </h2>
    <div class="driver-badge-info" style="margin-bottom: 15px; font-size: 15px; color: #000000;">
      👨‍✈️ Автомобіль авторизовано як:
      <span class="person-name" style="text-transform: uppercase; color: #000000;">
        {{ authStore.currentUser?.driver_car_class || 'comfort' }}
      </span>
    </div>
    <div v-if="!orderStore.currentOrder || !orderStore.currentOrder.order_id" class="no-orders">
      <p>Немає активних замовлень. Очікування клієнтів...</p>
    </div>

    <div v-else class="active-order-box">
      <p><strong>Клієнт:</strong> {{ orderStore.currentOrder.passenger_name }}</p>
      <p><strong>Маршрут:</strong> <span class="person-name">{{ orderStore.currentOrder.pickup_location }} → {{ orderStore.currentOrder.destination }}</span></p>
      <p><strong>Ваш дохід:</strong> <span class="person-name">{{ orderStore.currentOrder.estimated_cost }} грн</span></p>
      <p><strong>Погодні умови:</strong> {{ orderStore.currentOrder.weather_hazard_level === 'HIGH' ? 'Небезпечні' : 'Нормальні' }}</p>

      <p><strong>Метод оплати:</strong> {{ orderStore.currentOrder.payment_method || 'Готівка' }}</p>
      <p><strong>Фінансовий статус:</strong>
        <span class="badge" :class="orderStore.currentOrder.payment_status && orderStore.currentOrder.payment_status.includes('Оплачено') ? 'paid' : 'pending'">
          {{ orderStore.currentOrder.payment_status || 'Очікує завершення поїздки' }}
        </span>
      </p>
      <div class="driver-buttons mt-4" style="display: flex; flex-direction: column; gap: 10px;">
        <button v-if="orderStore.currentOrder.current_status === 'waiting'" class="btn success-btn w-full" @click="orderStore.updateStatus('accepted')">
          🔀 Прийняти замовлення
        </button>
        <button v-if="orderStore.currentOrder.current_status === 'accepted'" class="btn primary-btn w-full" @click="orderStore.updateStatus('in_progress')">
          🚗 Пасажир сів в авто (Почати рух)
        </button>

        <button v-if="orderStore.currentOrder.current_status === 'in_progress'" class="btn danger-btn w-full" @click="orderStore.updateStatus('completed')">
          💵 Завершити рейс 
        </button>
      </div>
    </div>

    <TaxiMap role="driver" :isBadWeather="orderStore.isBadWeather" />
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../../stores/authStore';
import { useOrderStore } from '../../stores/orderStore';
import TaxiMap from '../TaxiMap.vue';

const authStore = useAuthStore();
const orderStore = useOrderStore();
</script>
