<template>
  <div class="auth-card text-center">
    <h2>📧 Підтвердження пошти</h2>
    <p class="subtitle">Надіслали 6-значний код на <strong>{{ authStore.currentUser?.email }}</strong>. Перевірте папку "Спам", якщо не бачите листа.</p>

    <form @submit.prevent="authStore.handleVerifyEmail" class="auth-form">
      <div class="form-group">
        <label>Код підтвердження:</label>
        <input
          type="text"
          v-model="authStore.verificationCodeInput"
          placeholder="123456"
          maxlength="6"
          class="form-input"
          style="text-align: center; letter-spacing: 6px; font-size: 18px;"
          required
        />
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">✅ Підтвердити</button>
      </div>
    </form>

    <p class="toggle-auth-text">
      Не прийшов лист?
      <span @click="authStore.handleResendCode">Надіслати ще раз</span>
    </p>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../store/authStore';

const authStore = useAuthStore();
</script>
