<template>
  <div class="auth-card">
    <button class="back-link" @click="orderStore.authState = 'role_selection'">← Назад до вибору ролі</button>
    <h2>
      {{ orderStore.userRole === 'passenger' ? '🙋‍♀️ Кабінет Пасажира' : '🚖 Кабінет Водія' }} :
      {{ orderStore.isSignUp ? 'Реєстрація' : 'Авторизація' }}
    </h2>

    <form @submit.prevent="orderStore.handleAuthSubmit" class="auth-form">
      <div v-if="orderStore.isSignUp" class="form-grid">

        <div class="form-group">
          <label>Ім'я:</label>
          <input type="text" v-model="orderStore.firstNameInput" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Прізвище:</label>
          <input type="text" v-model="orderStore.lastNameInput" class="form-input" required />
        </div>

        <div class="form-group mt-2">
          <label>Номер телефону:</label>
          <input type="text" v-model="orderStore.phoneInput" placeholder="+380..." class="form-input" required />
        </div>

        <div v-if="orderStore.userRole === 'driver'" class="driver-fields mt-3" style="width: 100%;">
          <div class="form-group">
            <label>🪪 Номер водійського посвідчення:</label>
            <input type="text" v-model="orderStore.licenseInput" placeholder="BXX XXXXXX" class="form-input" required />
          </div>

          <div class="form-group mt-2">
            <label>🚗 Робочий клас автомобіля:</label>
            <select v-model="orderStore.driverCarClass" class="form-select">
              <option value="econom">🍃 Економ (Доступний сегмент)</option>
              <option value="comfort">⭐ Комфорт (Оптимальний сегмент)</option>
              <option value="lux">💎 Люкс / Бізнес (Преміум сегмент)</option>
            </select>
          </div>
        </div>

      </div> <div class="form-group mt-3">
        <label>Електронна пошта (Email):</label>
        <input type="email" v-model="orderStore.emailInput" placeholder="name@example.com" class="form-input" required />
      </div>

      <div class="form-group mt-2">
        <label>Пароль:</label>
        <input type="password" v-model="orderStore.passwordInput" placeholder="••••••••" class="form-input" required />
      </div>

      <div class="mt-4">
        <button type="submit" class="btn success-btn w-full">
          {{ orderStore.isSignUp ? '✨ Зареєструватися в системі' : '🔑 Увійти в кабінет' }}
        </button>
      </div>

      <p class="toggle-auth-text">
      {{ orderStore.isSignUp ? 'Вже є акаунт?' : 'Вперше в системі?' }}
      <span @click="orderStore.isSignUp = !orderStore.isSignUp">
        {{ orderStore.isSignUp ? 'Увійти' : 'Зареєструватися' }}
      </span>
    </p>
    </form>
  </div>
</template>

<script setup>
import { useOrderStore } from '../../stores/orderStore';

const orderStore = useOrderStore();
</script>
