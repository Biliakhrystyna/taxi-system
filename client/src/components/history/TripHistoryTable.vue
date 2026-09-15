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
          <th>ШІ Безпека</th>
          <th>Статус</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="trip in (authStore.currentUser?.role === 'passenger' ? orderStore.passengerTrips : orderStore.driverTrips)" :key="trip.order_id">
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
</template>

<script setup lang="ts">
import { useAuthStore } from '../../stores/authStore';
import { useOrderStore } from '../../stores/orderStore';

const authStore = useAuthStore();
const orderStore = useOrderStore();
</script>
