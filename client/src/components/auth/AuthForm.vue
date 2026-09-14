<template>
  <div class="auth-card">
    <button class="back-link" @click="authStore.authState = 'role_selection'">← Назад до вибору ролі</button>
    <h2>
      {{ authStore.userRole === 'passenger' ? '🙋‍♀️ Кабінет Пасажира' : '🚖 Кабінет Водія' }} :
      {{ authStore.isSignUp ? 'Реєстрація' : 'Авторизація' }}
    </h2>

    <form @submit.prevent="authStore.handleAuthSubmit" class="auth-form">
      <div v-if="authStore.isSignUp" class="form-grid">

        <div class="form-group">
          <label>Ім'я:</label>
          <input type="text" v-model="authStore.firstNameInput" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Прізвище:</label>
          <input type="text" v-model="authStore.lastNameInput" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Номер телефону:</label>
          <input type="text" v-model="authStore.phoneInput" placeholder="+380..." class="form-input" required />
        </div>

        <div v-if="authStore.userRole === 'driver'" class="driver-fields mt-3" style="width: 100%;">
          <div class="form-group">
            <label>🪪 Номер водійського посвідчення:</label>
            <input type="text" v-model="authStore.licenseInput" placeholder="BXX XXXXXX" class="form-input" required />
          </div>

          <div class="form-group mt-2">
            <label>🚗 Робочий клас автомобіля:</label>
            <select v-model="authStore.driverCarClass" class="form-select">
              <option value="econom">🍃 Економ (Доступний сегмент)</option>
              <option value="comfort">⭐ Комфорт (Оптимальний сегмент)</option>
              <option value="lux">💎 Люкс / Бізнес (Преміум сегмент)</option>
            </select>
          </div>
        </div>

      </div> <div class="form-group mt-3">
        <label>Електронна пошта (Email):</label>
        <input type="email" v-model="authStore.emailInput" placeholder="name@example.com" class="form-input" required />
      </div>

      <div class="form-group mt-2">
        <label>Пароль:</label>
        <input type="password" v-model="authStore.passwordInput" placeholder="••••••••" class="form-input" required />
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">
          {{ authStore.isSignUp ? '✨ Зареєструватися в системі' : '🔑 Увійти в кабінет' }}
        </button>
      </div>

      <p class="toggle-auth-text">
      {{ authStore.isSignUp ? 'Вже є акаунт?' : 'Вперше в системі?' }}
      <span @click="authStore.isSignUp = !authStore.isSignUp">
        {{ authStore.isSignUp ? 'Увійти' : 'Зареєструватися' }}
      </span>
    </p>
    </form>
  </div>
</template>

<script setup>
import { useAuthStore } from '../../stores/authStore';

const authStore = useAuthStore();
</script>
