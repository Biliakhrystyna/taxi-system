<template>
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
</template>

<script setup lang="ts">
import { useOrderStore } from '../../stores/orderStore';

const orderStore = useOrderStore();
</script>
