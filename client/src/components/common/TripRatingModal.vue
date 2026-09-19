<template>
  <div v-if="orderStore.tripToRate" class="modal-backdrop" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0, 0, 0, 0.75); display: flex; align-items: center; justify-content: center; z-index: 9999; padding: 20px;">
    <div class="modal-card" style="background: #0f172a; border: 2px solid #eab308; padding: 25px; border-radius: 12px; max-width: 420px; width: 100%; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.5); text-align: center;">

      <span style="font-size: 40px;">🚖</span>
      <h3 style="color: #eab308; margin: 10px 0 0 0; font-size: 18px; font-weight: bold; text-transform: uppercase;">Поїздку завершено</h3>
      <p style="color: #e2e8f0; font-size: 14px; margin-top: 8px;">
        Оцініть поїздку<span v-if="orderStore.tripToRate.driver_name"> з водієм {{ orderStore.tripToRate.driver_name }}</span>:
      </p>

      <div style="margin: 18px 0; font-size: 34px; letter-spacing: 6px;">
        <button
          v-for="n in 5"
          :key="n"
          type="button"
          class="rate-star-btn"
          :title="`${n} ${n === 1 ? 'зірка' : 'зірок'}`"
          @click="orderStore.rateOrder(orderStore.tripToRate.order_id, n)"
        >☆</button>
      </div>

      <button class="btn" style="background: rgba(71, 85, 105, 0.3); color: #cbd5e1; border: 1px solid #475569; padding: 10px; width: 100%;" @click="orderStore.dismissRating()">
        Пропустити
      </button>

    </div>
  </div>
</template>

<script setup lang="ts">
import { useOrderStore } from '../../stores/orderStore';

const orderStore = useOrderStore();
</script>

<style scoped>
.rate-star-btn {
  background: none;
  border: none;
  color: #b45309;
  cursor: pointer;
  padding: 0 2px;
  line-height: 1;
}
.rate-star-btn:hover {
  color: #eab308;
}
</style>
