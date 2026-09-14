<template>
  <div class="auth-card text-center">
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
</template>

<script setup>
import { useOrderStore } from '../../stores/orderStore';

const orderStore = useOrderStore();
</script>
