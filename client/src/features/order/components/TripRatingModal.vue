<template>
  <div v-if="orderStore.tripToRate" class="modal-backdrop">
    <div class="modal-card modal-card--rating">

      <span class="modal-icon">🚖</span>
      <h3 class="modal-title modal-title--rating">Поїздку завершено</h3>
      <p class="modal-text mt-2">
        Оцініть поїздку<span v-if="orderStore.tripToRate.driver_name"> з водієм {{ orderStore.tripToRate.driver_name }}</span>:
      </p>

      <div class="modal-stars">
        <button
          v-for="n in 5"
          :key="n"
          type="button"
          class="rate-star-btn"
          :title="`${n} ${n === 1 ? 'зірка' : 'зірок'}`"
          @click="orderStore.rateOrder(orderStore.tripToRate.order_id, n)"
        >☆</button>
      </div>

      <button class="btn btn-muted w-full" @click="orderStore.dismissRating()">
        Пропустити
      </button>

    </div>
  </div>
</template>

<script setup lang="ts">
import { useOrderStore } from '../store/orderStore';

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
