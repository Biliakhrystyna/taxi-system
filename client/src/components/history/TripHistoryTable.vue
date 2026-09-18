<template>
  <div class="screen-card history-box">
    <h2>📜 Історія виконаних поїздок</h2>

    <div v-if="authStore.currentUser?.role === 'passenger' && orderStore.passengerTrips.length === 0" class="no-data">Історія поїздок порожня.</div>
    <div v-if="authStore.currentUser?.role === 'driver' && orderStore.driverTrips.length === 0" class="no-data">Ви ще не виконали жодного рейсу.</div>

    <table class="history-table" v-if="authStore.currentUser?.role === 'passenger' ? orderStore.passengerTrips.length > 0 : orderStore.driverTrips.length > 0">
      <thead>
        <tr>
          <th>ID</th>
          <th>Маршрут призначення</th>
          <th>Вартість</th>
          <th>Безпека</th>
          <th>Статус</th>
          <th>Оцінка</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="trip in (authStore.currentUser?.role === 'passenger' ? orderStore.passengerTrips : orderStore.driverTrips)" :key="trip.order_id">
          <td>{{ trip.order_id.substring(4, 10) }}...</td>
          <td>{{ trip.destination }}</td>

          <td style="color: #10b981; font-weight: bold;">{{ trip.estimated_cost }} грн</td>

          <td>{{ trip.safe_route_applied ? 'Захист' : 'Стандарт' }}</td>
          <td><span class="badge" :class="trip.current_status">{{ translateOrderStatus(trip.current_status) }}</span></td>
          <td>
            <span v-if="trip.rating" class="rating-stars" title="Оцінку вже поставлено">
              <span v-for="n in 5" :key="n">{{ n <= trip.rating ? '★' : '☆' }}</span>
            </span>
            <span v-else>—</span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../../stores/authStore';
import { useOrderStore } from '../../stores/orderStore';
import { translateOrderStatus } from '../../utils/orderStatus';

const authStore = useAuthStore();
const orderStore = useOrderStore();
</script>
