<template>
  <div class="auth-card text-center">
    <button
      class="back-link"
      @click="authStore.authState = 'auth_form'; authStore.isSignUp = false; authStore.forgotPasswordStep = 'request';"
    >
      ← Назад до входу
    </button>

    <h2>🔑 Відновлення пароля</h2>

    <form v-if="authStore.forgotPasswordStep === 'request'" @submit.prevent="authStore.handleForgotPasswordRequest" class="auth-form">
      <p class="subtitle">Введіть email, яким реєструвались — надішлемо код для скидання пароля.</p>

      <div class="form-group">
        <label>Електронна пошта:</label>
        <input type="email" v-model="authStore.forgotPasswordEmail" placeholder="name@example.com" class="form-input" required />
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">📧 Надіслати код</button>
      </div>
    </form>

    <form v-else @submit.prevent="authStore.handleResetPassword" class="auth-form">
      <p class="subtitle">Код надіслано на <strong>{{ authStore.forgotPasswordEmail }}</strong>. Перевірте папку "Спам", якщо не бачите листа.</p>

      <div class="form-group">
        <label>Код підтвердження:</label>
        <input
          type="text"
          v-model="authStore.forgotPasswordCode"
          placeholder="123456"
          maxlength="6"
          class="form-input"
          style="text-align: center; letter-spacing: 6px; font-size: 18px;"
          required
        />
      </div>

      <div class="form-group mt-2">
        <label>Новий пароль:</label>
        <input
          type="password"
          v-model="authStore.forgotPasswordNewPassword"
          placeholder="Щонайменше 8 символів"
          class="form-input"
          required
        />
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">✅ Скинути пароль</button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../store/authStore';

const authStore = useAuthStore();
</script>
